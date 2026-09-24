using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

public interface IAmelonImportService
{
    Task<int> ImportAllAsync(CancellationToken ct = default);
    Task<int> ImportMissingByWebCodesAsync(IReadOnlyList<string> webCodes, CancellationToken ct = default);
}

public partial class AmelonImportService(
    ApplicationDbContext db,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment env,
    ILogger<AmelonImportService> logger) : IAmelonImportService
{
    private const string SitemapUrl = "https://amelon.co/product-sitemap.xml";
    private const string SitemapCacheFile = "sitemap.xml";

    private string CacheDir => Path.Combine(env.ContentRootPath, "App_Data", "amelon-cache");

    public async Task<int> ImportAllAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(CacheDir);

        var client = httpClientFactory.CreateClient("amelon");
        var sitemap = await FetchTextAsync(client, SitemapUrl, SitemapCacheFile, ct);
        if (string.IsNullOrWhiteSpace(sitemap))
        {
            logger.LogWarning("Could not load amelon sitemap (network or cache). Import skipped.");
            return 0;
        }

        var urls = ProductUrlRegex().Matches(sitemap)
            .Select(m => m.Groups[1].Value.TrimEnd('/'))
            .Where(u => u.Contains("/product/", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (urls.Count == 0)
        {
            logger.LogWarning("No product URLs found in amelon sitemap.");
            return 0;
        }

        logger.LogInformation("Importing {Count} products from amelon.co", urls.Count);

        await db.ProductImages.ExecuteDeleteAsync(ct);
        await db.ProductVariants.ExecuteDeleteAsync(ct);
        await db.Products.ExecuteDeleteAsync(ct);
        await db.Categories.ExecuteDeleteAsync(ct);

        var categoryCache = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        var imported = 0;

        foreach (var url in urls)
        {
            try
            {
                var slug = ExtractSlugFromUrl(url);
                var cacheKey = $"products/{Uri.EscapeDataString(slug)}.html";
                var html = await FetchTextAsync(client, url, cacheKey, ct);
                if (string.IsNullOrWhiteSpace(html)) continue;

                var parsed = ParseProductPage(html, url);
                if (parsed == null) continue;

                var category = await ResolveCategoryAsync(parsed.CategoryName, categoryCache, ct);

                var product = new Product
                {
                    Name = parsed.Name,
                    Slug = parsed.Slug,
                    ShortDescription = parsed.ShortDescription,
                    Description = parsed.Description,
                    ProductCode = parsed.ProductCode,
                    Dimensions = parsed.Dimensions,
                    Applications = parsed.ApplicationsRaw,
                    UseCaseTags = parsed.UseCaseTags,
                    Material = parsed.Material,
                    ProductType = parsed.ProductType,
                    CapacityCc = parsed.CapacityCc,
                    CompartmentCount = parsed.CompartmentCount,
                    MicrowaveSafe = parsed.MicrowaveSafe,
                    AmelonSourceUrl = url,
                    CategoryId = category.Id,
                    IsActive = true,
                    MetaTitle = $"{parsed.Name} | آملون",
                    MetaDescription = parsed.ShortDescription,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                for (var imgIdx = 0; imgIdx < parsed.ImageUrls.Count; imgIdx++)
                {
                    product.Images.Add(new ProductImage
                    {
                        Url = parsed.ImageUrls[imgIdx],
                        AltText = parsed.Name,
                        IsPrimary = imgIdx == 0,
                        SortOrder = imgIdx
                    });
                }

                product.Variants.Add(new ProductVariant
                {
                    PackagingType = parsed.PackagingType,
                    Sku = $"AM-{parsed.Slug}",
                    Price = 0,
                    ShowPrice = false,
                    UnitsPerCarton = 500,
                    UnitsPerPack = parsed.UnitsPerPack ?? 1,
                    PackLabel = parsed.PackLabel,
                    IsActive = true
                });

                db.Products.Add(product);
                await db.SaveChangesAsync(ct);
                imported++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to import {Url}", url);
                DetachPendingEntries();
            }
        }

        logger.LogInformation("Import finished: {Count} products saved", imported);
        return imported;
    }

    public async Task<int> ImportMissingByWebCodesAsync(IReadOnlyList<string> webCodes, CancellationToken ct = default)
    {
        Directory.CreateDirectory(CacheDir);
        var codes = webCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal).ToList();
        if (codes.Count == 0) return 0;

        var existing = await db.Products
            .Where(p => p.ProductCode != null && codes.Contains(p.ProductCode))
            .Select(p => p.ProductCode!)
            .ToListAsync(ct);

        var missing = codes.Except(existing, StringComparer.Ordinal).ToList();
        if (missing.Count == 0) return 0;

        var categoryCache = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        var imported = 0;

        foreach (var code in missing)
        {
            try
            {
                if (await TryImportFromCacheByWebCodeAsync(code, categoryCache, ct))
                {
                    imported++;
                    continue;
                }

                if (MissingProductCatalog.TryGetStub(code, out var stub)
                    && await CreateFromStubAsync(stub, categoryCache, ct))
                {
                    imported++;
                }
                else
                {
                    logger.LogWarning("Could not import missing product code {Code}", code);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed importing missing product {Code}", code);
                DetachPendingEntries();
            }
        }

        logger.LogInformation("Imported {Count} missing amelon products", imported);
        return imported;
    }

    async Task<bool> TryImportFromCacheByWebCodeAsync(string webCode, Dictionary<string, Category> categoryCache, CancellationToken ct)
    {
        var productsDir = Path.Combine(CacheDir, "products");
        if (!Directory.Exists(productsDir)) return false;

        string? bestHtml = null;
        string? bestUrl = null;
        foreach (var file in Directory.EnumerateFiles(productsDir, "*.html"))
        {
            var html = await File.ReadAllTextAsync(file, ct);
            if (!Regex.IsMatch(html, $@"کد\s*محصول\s*[:：]?\s*{Regex.Escape(webCode)}\b"))
                continue;
            bestHtml = html;
            var slug = Uri.UnescapeDataString(Path.GetFileNameWithoutExtension(file));
            bestUrl = $"https://amelon.co/product/{Uri.EscapeDataString(slug)}/";
            break;
        }

        if (bestHtml == null || bestUrl == null) return false;

        var parsed = ParseProductPage(bestHtml, bestUrl);
        if (parsed == null) return false;

        parsed = parsed with { ProductCode = webCode, Sku = webCode };
        await SaveParsedProductAsync(parsed, bestUrl, categoryCache, ct);
        return true;
    }

    async Task<bool> CreateFromStubAsync(MissingProductStub stub, Dictionary<string, Category> categoryCache, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(p => p.ProductCode == stub.WebCode || p.Slug == stub.Slug, ct))
            return false;

        var category = await ResolveCategoryAsync(stub.CategoryName, categoryCache, ct);
        var images = new List<string>();
        if (!string.IsNullOrWhiteSpace(stub.ImageCacheSlug))
        {
            var cachePath = Path.Combine(CacheDir, "products", Uri.EscapeDataString(stub.ImageCacheSlug) + ".html");
            if (File.Exists(cachePath))
            {
                var html = await File.ReadAllTextAsync(cachePath, ct);
                images = AmelonProductImageParser.ExtractImageUrls(html).ToList();
            }
        }

        var product = new Product
        {
            Name = stub.Name,
            Slug = stub.Slug,
            ShortDescription = stub.ShortDescription,
            Description = stub.Description,
            ProductCode = stub.WebCode,
            ProductType = stub.ProductType,
            Material = "نشاسته گیاهی",
            CapacityCc = stub.CapacityCc,
            MicrowaveSafe = true,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        product.MetaTitle = $"{stub.Name} | خرید عمده {stub.ProductType} آملون — موثقی";
        product.MetaDescription = stub.ShortDescription + " خرید عمده از فروشگاه موثقی، نمایندگی رسمی آملون تهران.";
        product.MetaKeywords = $"{stub.Name}, {stub.ProductType}, آملون, ظروف گیاهی, خرید عمده, کد {stub.WebCode}, موثقی";

        for (var i = 0; i < images.Count; i++)
        {
            product.Images.Add(new ProductImage
            {
                Url = images[i],
                AltText = $"{stub.Name} — تصویر {(i + 1)}",
                IsPrimary = i == 0,
                SortOrder = i
            });
        }

        product.Variants.Add(new ProductVariant
        {
            PackagingType = PackagingType.Bulk,
            Sku = $"AM-{stub.WebCode}-B",
            Price = 0,
            ShowPrice = false,
            UnitsPerCarton = 500,
            UnitsPerPack = 1,
            PackLabel = "فله (عمده)",
            IsActive = true
        });

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return true;
    }

    async Task SaveParsedProductAsync(ParsedProduct parsed, string url, Dictionary<string, Category> categoryCache, CancellationToken ct)
    {
        if (await db.Products.AnyAsync(p => p.ProductCode == parsed.ProductCode || p.Slug == parsed.Slug, ct))
            return;

        var category = await ResolveCategoryAsync(parsed.CategoryName, categoryCache, ct);
        var product = new Product
        {
            Name = parsed.Name,
            Slug = parsed.Slug,
            ShortDescription = parsed.ShortDescription,
            Description = parsed.Description,
            ProductCode = parsed.ProductCode,
            Dimensions = parsed.Dimensions,
            Applications = parsed.ApplicationsRaw,
            UseCaseTags = parsed.UseCaseTags,
            Material = parsed.Material,
            ProductType = parsed.ProductType,
            CapacityCc = parsed.CapacityCc,
            CompartmentCount = parsed.CompartmentCount,
            MicrowaveSafe = parsed.MicrowaveSafe,
            AmelonSourceUrl = url,
            CategoryId = category.Id,
            IsActive = true,
            MetaTitle = $"{parsed.Name} | آملون",
            MetaDescription = parsed.ShortDescription,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        for (var imgIdx = 0; imgIdx < parsed.ImageUrls.Count; imgIdx++)
        {
            product.Images.Add(new ProductImage
            {
                Url = parsed.ImageUrls[imgIdx],
                AltText = parsed.Name,
                IsPrimary = imgIdx == 0,
                SortOrder = imgIdx
            });
        }

        product.Variants.Add(new ProductVariant
        {
            PackagingType = parsed.PackagingType,
            Sku = $"AM-{parsed.ProductCode}-B",
            Price = 0,
            ShowPrice = false,
            UnitsPerCarton = 500,
            UnitsPerPack = parsed.UnitsPerPack ?? 1,
            PackLabel = parsed.PackLabel,
            IsActive = true
        });

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
    }

    private void DetachPendingEntries()
    {
        foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToList())
            entry.State = EntityState.Detached;
    }

    private async Task<Category> ResolveCategoryAsync(string categoryName, Dictionary<string, Category> cache, CancellationToken ct)
    {
        categoryName = string.IsNullOrWhiteSpace(categoryName) ? "محصولات آملون" : categoryName.Trim();
        if (cache.TryGetValue(categoryName, out var cached))
            return cached;

        var slug = SlugHelper.Generate(categoryName);
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Slug == slug || c.Name == categoryName, ct);
        if (existing != null)
        {
            cache[categoryName] = existing;
            return existing;
        }

        var uniqueSlug = slug;
        var suffix = 2;
        while (CategorySlugTaken(uniqueSlug) || await db.Categories.AnyAsync(c => c.Slug == uniqueSlug, ct))
            uniqueSlug = $"{slug}-{suffix++}";

        try
        {
            var category = new Category
            {
                Name = categoryName,
                Slug = uniqueSlug,
                IsActive = true,
                SortOrder = cache.Count
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);
            cache[categoryName] = category;
            return category;
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Category slug collision for {Name}, reloading", categoryName);
            DetachPendingEntries();
            var fallback = await db.Categories.FirstOrDefaultAsync(c => c.Slug == slug || c.Name == categoryName, ct)
                ?? await db.Categories.FirstAsync(c => c.Slug.StartsWith(slug), ct);
            cache[categoryName] = fallback;
            return fallback;
        }
    }

    private bool CategorySlugTaken(string slug)
        => db.ChangeTracker.Entries<Category>().Any(e => e.Entity.Slug == slug && e.State == EntityState.Added);

    private async Task<string?> FetchTextAsync(HttpClient client, string url, string cacheRelativePath, CancellationToken ct)
    {
        var cachePath = Path.Combine(CacheDir, cacheRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);

        if (File.Exists(cachePath))
        {
            try
            {
                return await File.ReadAllTextAsync(cachePath, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed reading cache {CachePath}", cachePath);
            }
        }

        try
        {
            var text = await client.GetStringAsync(url, ct);
            await File.WriteAllTextAsync(cachePath, text, ct);
            return text;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Network fetch failed for {Url}", url);
            return null;
        }
    }

    private static string ExtractSlugFromUrl(string url)
        => Uri.UnescapeDataString(url.TrimEnd('/').Split('/').Last());

    private static ParsedProduct? ParseProductPage(string html, string url)
    {
        var jsonLd = JsonLdRegex().Match(html).Groups[1].Value;
        string? jsonName = null, jsonDescription = null, jsonSku = null, jsonCategory = null, jsonImageUrl = null;
        string? categoryName = null;

        if (!string.IsNullOrWhiteSpace(jsonLd))
        {
            try
            {
                using var docJson = JsonDocument.Parse(jsonLd);
                if (docJson.RootElement.TryGetProperty("@graph", out var graph))
                {
                    foreach (var node in graph.EnumerateArray())
                    {
                        if (node.TryGetProperty("@type", out var t) && t.GetString() == "Product")
                        {
                            if (node.TryGetProperty("name", out var nameEl))
                                jsonName = nameEl.GetString();
                            if (node.TryGetProperty("description", out var descEl))
                                jsonDescription = descEl.GetString();
                            if (node.TryGetProperty("sku", out var skuEl))
                                jsonSku = skuEl.GetString();
                            if (node.TryGetProperty("category", out var catEl))
                                jsonCategory = catEl.GetString();
                            if (node.TryGetProperty("image", out var imgEl))
                            {
                                jsonImageUrl = imgEl.ValueKind == JsonValueKind.Array && imgEl.GetArrayLength() > 0
                                    ? imgEl[0].TryGetProperty("url", out var iu) ? iu.GetString()
                                    : imgEl[0].TryGetProperty("@id", out var idEl) ? idEl.GetString() : imgEl[0].GetString()
                                    : imgEl.TryGetProperty("url", out var su) ? su.GetString() : imgEl.GetString();
                            }
                        }

                        if (node.TryGetProperty("@type", out var bt) && bt.GetString() == "BreadcrumbList")
                        {
                            var items = node.GetProperty("itemListElement").EnumerateArray().ToList();
                            if (items.Count >= 2 && items[1].TryGetProperty("item", out var item) && item.TryGetProperty("name", out var cn))
                                categoryName = DecodeHtml(cn.GetString());
                        }
                    }
                }
            }
            catch { /* fallback to regex */ }
        }

        var title = H1Regex().Match(html).Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(jsonName))
            title = jsonName.Split('»').FirstOrDefault()?.Trim() ?? "";
        title = DecodeHtml(title);
        if (string.IsNullOrWhiteSpace(title)) return null;

        var slug = ExtractSlugFromUrl(url);
        var description = !string.IsNullOrWhiteSpace(jsonDescription)
            ? DecodeHtml(jsonDescription)
            : MetaDescRegex().Match(html).Groups[1].Value;

        var specs = ParseSpecsFromDescription(description);
        var sku = jsonSku ?? specs.ProductCode;
        categoryName ??= !string.IsNullOrWhiteSpace(jsonCategory) ? DecodeHtml(jsonCategory) : null;
        categoryName ??= "محصولات آملون";

        var imageUrls = AmelonProductImageParser.ExtractImageUrls(html);
        var imageUrl = imageUrls.FirstOrDefault();
        if (string.IsNullOrEmpty(imageUrl))
            imageUrl = OgImageRegex().Match(html).Groups[1].Value;
        if (string.IsNullOrEmpty(imageUrl))
            imageUrl = jsonImageUrl;

        var packing = PackagingType.Bulk;
        if (PackingShrinkRegex().IsMatch(html) || title.Contains("بسته", StringComparison.OrdinalIgnoreCase))
            packing = title.Contains("بسته", StringComparison.OrdinalIgnoreCase) ? PackagingType.Shrink : packing;

        var packMatch = PackCountRegex().Match(title);
        string? packLabel = null;
        int? unitsPerPack = 1;
        if (packMatch.Success)
        {
            packing = PackagingType.Shrink;
            unitsPerPack = int.Parse(packMatch.Groups[1].Value);
            packLabel = $"بسته {unitsPerPack} عددی";
        }

        if (html.Contains(">فله<", StringComparison.OrdinalIgnoreCase)) packing = PackagingType.Bulk;
        if (html.Contains(">شیرین", StringComparison.OrdinalIgnoreCase)) packing = PackagingType.Shrink;

        var productType = DetectProductType(title);
        var capacity = ExtractCapacityCc(title, description);
        var compartments = ExtractCompartments(title);
        var microwave = description.Contains("مایکرو", StringComparison.OrdinalIgnoreCase);
        var useCases = NormalizeUseCases(specs.Applications ?? description);

        return new ParsedProduct(
            title, slug, categoryName, sku, specs.ProductCode, specs.Dimensions, specs.Applications,
            specs.Material ?? "نشاسته", description, imageUrls, packing, packLabel, unitsPerPack,
            productType, capacity, compartments, microwave, useCases,
            specs.Applications != null ? specs.Applications[..Math.Min(160, specs.Applications.Length)] : null);
    }

    private static string DecodeHtml(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("&raquo;", "»").Replace("&nbsp;", " ").Replace("&#8211;", "–").Replace("&amp;", "&");
    }

    private static SpecFields ParseSpecsFromDescription(string text)
    {
        var code = Regex.Match(text, @"کد\s*محصول\s*[:：]?\s*(\d+)", RegexOptions.IgnoreCase);
        var dims = Regex.Match(text, @"ابعاد\s*[:：]?\s*([^کاربردساخته]+)", RegexOptions.IgnoreCase);
        var apps = Regex.Match(text, @"کاربرد\s*[:：]?\s*([^ساخته]+)", RegexOptions.IgnoreCase);
        var mat = Regex.Match(text, @"ساخته\s*شده\s*از\s*[:：]?\s*(\S+)", RegexOptions.IgnoreCase);
        return new SpecFields(
            code.Success ? code.Groups[1].Value : null,
            dims.Success ? dims.Groups[1].Value.Trim() : null,
            apps.Success ? apps.Groups[1].Value.Trim().Trim('،', ',', ' ') : null,
            mat.Success ? mat.Groups[1].Value.Trim() : null);
    }

    private static string DetectProductType(string name)
    {
        if (CompartmentRegex().IsMatch(name)) return "ظرف چندخانه";
        if (name.Contains("بشقاب", StringComparison.OrdinalIgnoreCase)) return "بشقاب";
        if (name.Contains("دیس", StringComparison.OrdinalIgnoreCase)) return "دیس";
        if (name.Contains("سطل", StringComparison.OrdinalIgnoreCase)) return "سطل";
        if (name.Contains("کاسه", StringComparison.OrdinalIgnoreCase)) return "کاسه";
        if (name.Contains("لیوان", StringComparison.OrdinalIgnoreCase) || name.Contains("فنجان", StringComparison.OrdinalIgnoreCase)) return "لیوان";
        if (name.Contains("قاشق", StringComparison.OrdinalIgnoreCase)) return "قاشق";
        if (name.Contains("چنگال", StringComparison.OrdinalIgnoreCase)) return "چنگال";
        if (name.Contains("نی", StringComparison.OrdinalIgnoreCase)) return "نی";
        if (name.Contains("کیک", StringComparison.OrdinalIgnoreCase)) return "کیک‌خوری";
        if (name.Contains("پیش", StringComparison.OrdinalIgnoreCase) && name.Contains("دست", StringComparison.OrdinalIgnoreCase)) return "پیش‌دستی";
        if (name.Contains("ظرف", StringComparison.OrdinalIgnoreCase)) return "ظرف";
        return "سایر";
    }

    private static int? ExtractCapacityCc(string name, string desc)
    {
        try
        {
            var m = CapacityRegex().Match(name + " " + desc);
            if (!m.Success) return null;
            var raw = m.Groups[1].Value;
            var normalized = raw.Replace("۰", "0").Replace("۱", "1").Replace("۲", "2").Replace("۳", "3")
                .Replace("۴", "4").Replace("۵", "5").Replace("۶", "6").Replace("۷", "7")
                .Replace("۸", "8").Replace("۹", "9");
            var digits = new string(normalized.Where(char.IsAsciiDigit).ToArray());
            return int.TryParse(digits, out var n) ? n : null;
        }
        catch { return null; }
    }

    private static int? ExtractCompartments(string name)
    {
        var m = CompartmentRegex().Match(name);
        if (!m.Success) return null;
        var s = m.Groups[1].Value;
        var map = new Dictionary<string, int> { ["یک"] = 1, ["دو"] = 2, ["سه"] = 3, ["چهار"] = 4, ["پنج"] = 5, ["شش"] = 6, ["هفت"] = 7, ["هشت"] = 8, ["نه"] = 9, ["ده"] = 10 };
        if (map.TryGetValue(s, out var n)) return n;
        return int.TryParse(s, out var d) ? d : null;
    }

    private static string? NormalizeUseCases(string? applications)
    {
        if (string.IsNullOrWhiteSpace(applications)) return null;
        var parts = applications.Split(['،', ',', '؛', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var normalized = parts.Where(p => p.Length > 1).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return normalized.Count == 0 ? null : string.Join(',', normalized);
    }

    private record SpecFields(string? ProductCode, string? Dimensions, string? Applications, string? Material);
    private record ParsedProduct(
        string Name, string Slug, string CategoryName, string? Sku, string? ProductCode,
        string? Dimensions, string? ApplicationsRaw, string? Material, string Description,
        IReadOnlyList<string> ImageUrls, PackagingType PackagingType, string? PackLabel, int? UnitsPerPack,
        string ProductType, int? CapacityCc, int? CompartmentCount, bool MicrowaveSafe,
        string? UseCaseTags, string? ShortDescription);

    [GeneratedRegex(@"<loc>(https://amelon\.co/product/[^<]+)</loc>", RegexOptions.IgnoreCase)]
    private static partial Regex ProductUrlRegex();

    [GeneratedRegex(@"<script[^>]*class=""rank-math-schema-pro""[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex JsonLdRegex();

    [GeneratedRegex(@"<h1[^>]*>(.*?)</h1>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex H1Regex();

    [GeneratedRegex(@"<meta name=""description"" content=""(.*?)""", RegexOptions.Singleline)]
    private static partial Regex MetaDescRegex();

    [GeneratedRegex(@"<meta property=""og:image"" content=""(.*?)""")]
    private static partial Regex OgImageRegex();

    [GeneratedRegex(@"attribute_pa_packing[^>]*>[\s\S]*?<p>شیرین")]
    private static partial Regex PackingShrinkRegex();

    [GeneratedRegex(@"بسته\s*(\d+)\s*عدد")]
    private static partial Regex PackCountRegex();

    [GeneratedRegex(@"(\d+)\s*(?:سی\s*سی|cc|CC)")]
    private static partial Regex CapacityRegex();

    [GeneratedRegex(@"(\d+|یک|دو|سه|چهار|پنج|شش|هفت|هشت|نه|ده)\s*خانه")]
    private static partial Regex CompartmentRegex();
}
