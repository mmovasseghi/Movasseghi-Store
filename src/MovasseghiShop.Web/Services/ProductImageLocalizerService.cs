using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace MovasseghiShop.Web.Services;

public record ImageLocalizationResult(
    int ProductsProcessed,
    int ImagesDownloaded,
    int WebpGenerated,
    IReadOnlyList<string> Failures);

public interface IProductImageLocalizerService
{
    Task<ImageLocalizationResult> LocalizeAllAsync(CancellationToken ct = default);
}

public class ProductImageLocalizerService(
    ApplicationDbContext db,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment env,
    ILogger<ProductImageLocalizerService> logger) : IProductImageLocalizerService
{
    private const string ProductsVirtualRoot = "/images/products";

    private string ProductsPhysicalRoot => Path.Combine(env.WebRootPath, "images", "products");
    private string CacheProductsDir => Path.Combine(env.ContentRootPath, "App_Data", "amelon-cache", "products");
    private string ReportPath => Path.Combine(env.ContentRootPath, "App_Data", "image-localization-report.json");

    public async Task<ImageLocalizationResult> LocalizeAllAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(ProductsPhysicalRoot);

        var products = await db.Products
            .Include(p => p.Images)
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        var client = httpClientFactory.CreateClient("amelon");
        var failures = new List<string>();
        var downloaded = 0;
        var webpCount = 0;

        foreach (var product in products)
        {
            try
            {
                if (ProductImageHelper.HasOperatorGallery(product.Images, env.WebRootPath, product.Id))
                {
                    logger.LogDebug("Skipping image localization for product #{Id} — operator Scene/Studio gallery", product.Id);
                    continue;
                }

                var sourceUrls = await ResolveSourceUrlsAsync(product, client, ct);
                if (sourceUrls.Count == 0)
                {
                    failures.Add($"Product #{product.Id} ({product.Slug}): no source images found on amelon.co");
                    continue;
                }

                var productDir = Path.Combine(ProductsPhysicalRoot, product.Id.ToString("D4"));
                Directory.CreateDirectory(productDir);

                var newImages = new List<ProductImage>();
                var index = 0;

                foreach (var sourceUrl in sourceUrls)
                {
                    index++;
                    var fileStem = $"{index:D2}";
                    var download = await DownloadImageAsync(client, sourceUrl, productDir, fileStem, ct);
                    if (download == null)
                    {
                        failures.Add($"Product #{product.Id} ({product.Slug}): failed to download {sourceUrl}");
                        continue;
                    }

                    downloaded++;
                    var webpRelative = await TryCreateWebpAsync(productDir, download.Value.originalFileName, fileStem, ct);
                    if (webpRelative != null) webpCount++;

                    var displayRelative = webpRelative ?? download.Value.publicRelativePath;
                    newImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        Url = displayRelative,
                        AltText = product.Name,
                        IsPrimary = index == 1,
                        SortOrder = index - 1
                    });
                }

                if (newImages.Count == 0)
                {
                    failures.Add($"Product #{product.Id} ({product.Slug}): all downloads failed");
                    continue;
                }

                db.ProductImages.RemoveRange(product.Images);
                db.ProductImages.AddRange(newImages);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Localized {Count} images for product #{Id} {Slug}", newImages.Count, product.Id, product.Slug);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed localizing images for product #{Id}", product.Id);
                failures.Add($"Product #{product.Id} ({product.Slug}): {ex.Message}");
            }
        }

        var result = new ImageLocalizationResult(products.Count, downloaded, webpCount, failures);
        await WriteReportAsync(result, ct);
        return result;
    }

    private async Task<List<string>> ResolveSourceUrlsAsync(Product product, HttpClient client, CancellationToken ct)
    {
        var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var cachedHtml = TryReadCachedHtml(product);
        if (!string.IsNullOrWhiteSpace(cachedHtml))
        {
            foreach (var u in AmelonProductImageParser.ExtractImageUrls(cachedHtml))
                urls.Add(u);
        }

        if (urls.Count == 0 && !string.IsNullOrWhiteSpace(product.AmelonSourceUrl))
        {
            try
            {
                var html = await client.GetStringAsync(AmelonProductImageParser.ToRequestUri(product.AmelonSourceUrl), ct);
                foreach (var u in AmelonProductImageParser.ExtractImageUrls(html))
                    urls.Add(u);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not fetch product page for #{Id}", product.Id);
            }
        }

        foreach (var img in product.Images.Where(i => i.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            urls.Add(AmelonProductImageParser.NormalizeAmelonUrl(img.Url));

        return urls.OrderBy(u => u, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private string? TryReadCachedHtml(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.AmelonSourceUrl)) return null;
        if (!Directory.Exists(CacheProductsDir)) return null;

        var slug = Uri.UnescapeDataString(product.AmelonSourceUrl.TrimEnd('/').Split('/').Last());
        var encodedName = Uri.EscapeDataString(slug) + ".html";
        var candidates = new[]
        {
            Path.Combine(CacheProductsDir, encodedName),
            Path.Combine(CacheProductsDir, slug + ".html")
        };

        foreach (var path in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        return Directory.EnumerateFiles(CacheProductsDir, "*.html")
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).Equals(slug, StringComparison.OrdinalIgnoreCase)
                || Path.GetFileNameWithoutExtension(f).Equals(Uri.EscapeDataString(slug), StringComparison.OrdinalIgnoreCase))
            is string match ? File.ReadAllText(match) : null;
    }

    private async Task<(string originalFileName, string publicRelativePath)?> DownloadImageAsync(
        HttpClient client, string sourceUrl, string productDir, string fileStem, CancellationToken ct)
    {
        var ext = GuessExtension(sourceUrl);

        try
        {
            var requestUri = AmelonProductImageParser.ToRequestUri(sourceUrl);
            using var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            ext = ExtFromContentType(contentType) ?? ext;

            var originalFileName = $"{fileStem}-original{ext}";
            var originalPath = Path.Combine(productDir, originalFileName);
            await using (var fs = File.Create(originalPath))
                await response.Content.CopyToAsync(fs, ct);

            var publicRelative = $"{ProductsVirtualRoot}/{Path.GetFileName(productDir)}/{originalFileName}".Replace('\\', '/');
            return (originalFileName, publicRelative);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Download failed for {Url}", sourceUrl);
            return null;
        }
    }

    private static async Task<string?> TryCreateWebpAsync(string productDir, string originalFileName, string fileStem, CancellationToken ct)
    {
        var originalPath = Path.Combine(productDir, originalFileName);
        if (!File.Exists(originalPath)) return null;

        var webpFileName = $"{fileStem}.webp";
        var webpPath = Path.Combine(productDir, webpFileName);

        try
        {
            await using var input = File.OpenRead(originalPath);
            using var image = await Image.LoadAsync(input, ct);

            // منبع گاهی عکس خام دوربین است (چند هزار پیکسل). بدون تغییر ابعاد،
            // WebP هم چند مگابایت می‌ماند و روی LCP سنگینی می‌کند.
            var longest = Math.Max(image.Width, image.Height);
            if (longest > ImageOptimizerService.MaxEdge)
            {
                var scale = (double)ImageOptimizerService.MaxEdge / longest;
                image.Mutate(x => x.Resize(
                    (int)Math.Round(image.Width * scale),
                    (int)Math.Round(image.Height * scale)));
            }

            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            await image.SaveAsWebpAsync(
                webpPath, new WebpEncoder { Quality = ImageOptimizerService.WebpQuality }, ct);
            return $"{ProductsVirtualRoot}/{Path.GetFileName(productDir)}/{webpFileName}".Replace('\\', '/');
        }
        catch
        {
            return null;
        }
    }

    private static string GuessExtension(string url)
    {
        var path = url.Split('?', '#')[0];
        var ext = Path.GetExtension(path);
        return string.IsNullOrWhiteSpace(ext) ? ".jpg" : ext.ToLowerInvariant();
    }

    private static string? ExtFromContentType(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/jpeg" => ".jpg",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => null
    };

    private async Task WriteReportAsync(ImageLocalizationResult result, CancellationToken ct)
    {
        var payload = new
        {
            completedAt = DateTime.UtcNow,
            result.ProductsProcessed,
            result.ImagesDownloaded,
            result.WebpGenerated,
            failureCount = result.Failures.Count,
            failures = result.Failures
        };
        await File.WriteAllTextAsync(ReportPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }), ct);
    }
}
