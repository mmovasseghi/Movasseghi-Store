using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web;

public sealed record ProductGallerySlide(string Url, bool IsScene, string Alt);

public sealed record ProductDualCardSlides(
    string? StudioUrl,
    string? SceneUrl,
    string StudioAlt,
    string SceneAlt,
    bool CanCycle);

public static class ProductImageHelper
{
    public static ProductImage? GetPrimary(IEnumerable<ProductImage> images)
        => images.FirstOrDefault(i => i.Role == ProductImageRole.Studio && i.IsPrimary)
           ?? images.FirstOrDefault(i => i.Role == ProductImageRole.Studio)
           ?? images.FirstOrDefault(i => i.IsPrimary)
           ?? images.OrderBy(i => i.SortOrder).FirstOrDefault();

    public static string GetAltText(ProductImage? image, string fallback)
    {
        var alt = image?.AltText?.Trim();
        return string.IsNullOrWhiteSpace(alt) ? fallback : alt;
    }

    public static string GetAltText(IEnumerable<ProductImage> images, string fallback)
        => GetAltText(GetPrimary(images), fallback);

    public static string? GetDisplayUrl(ProductImage? image, IWebHostEnvironment? env)
    {
        if (image == null || string.IsNullOrWhiteSpace(image.Url)) return null;
        if (image.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return null;

        var url = image.Url.StartsWith('/') ? image.Url : "/" + image.Url;
        return PreferWebp(url, env);
    }

    /// <summary>
    /// مسیر ذخیره‌شده در دیتابیس معمولاً به PNG اصلی اشاره می‌کند، در حالی که
    /// نسخه WebP هم کنارش روی دیسک هست. تا امروز همان PNG سنگین سرو می‌شد؛
    /// این متد اگر معادل WebP وجود داشته باشد آن را برمی‌گرداند.
    /// نتیجه بررسی دیسک کش می‌شود تا برای هر درخواست I/O انجام نشود.
    /// </summary>
    public static string? PreferWebp(string? url, IWebHostEnvironment? env)
    {
        if (env is null || string.IsNullOrWhiteSpace(url)) return url;
        if (url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) return url;
        if (!url.StartsWith('/')) return url;

        return WebpCache.GetOrAdd(url, static (key, environment) =>
        {
            foreach (var candidate in WebpCandidates(key))
            {
                var relative = candidate.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var absolute = Path.Combine(environment.WebRootPath, relative);
                if (File.Exists(absolute)) return candidate;
            }
            return key;
        }, env);
    }

    /// <summary>نام‌گذاری روی دیسک یکدست نیست: هم «01-original.png → 01.webp» داریم هم «01-hero.png → 01-hero.webp».</summary>
    static IEnumerable<string> WebpCandidates(string url)
    {
        var dot = url.LastIndexOf('.');
        if (dot <= 0) yield break;

        var withoutExt = url[..dot];
        if (withoutExt.EndsWith("-original", StringComparison.OrdinalIgnoreCase))
            yield return withoutExt[..^"-original".Length] + ".webp";

        yield return withoutExt + ".webp";
    }

    static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> WebpCache = new();

    /// <summary>Two-slide gallery: scene (index 0) + studio (index 1). Falls back to one URL for both until scene is uploaded.</summary>
    /// <summary>استودیو + صحنه برای کارت محصول — چرخه فقط وقتی هر دو URL متفاوت باشند.</summary>
    public static ProductDualCardSlides ResolveDualCardSlides(IEnumerable<ProductImage> images, IWebHostEnvironment? env, string productName)
    {
        var list = images
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .ToList();
        if (list.Count == 0)
            return new ProductDualCardSlides(null, null, productName, productName, false);

        var sceneImg = FindSceneImage(list);
        var studioImg = FindStudioImage(list, sceneImg);
        if (studioImg == null)
            return new ProductDualCardSlides(null, null, productName, productName, false);

        var studioUrl = GetDisplayUrl(studioImg, env);
        if (string.IsNullOrEmpty(studioUrl))
            return new ProductDualCardSlides(null, null, productName, productName, false);

        if (sceneImg == null || sceneImg.Id == studioImg.Id)
            return new ProductDualCardSlides(studioUrl, null, GetAltText(studioImg, productName), productName, false);

        var sceneUrl = ResolveSceneDisplayUrl(sceneImg, env);
        if (string.IsNullOrEmpty(sceneUrl) || string.Equals(sceneUrl, studioUrl, StringComparison.OrdinalIgnoreCase))
            return new ProductDualCardSlides(studioUrl, null, GetAltText(studioImg, productName), productName, false);

        return new ProductDualCardSlides(
            studioUrl,
            sceneUrl,
            GetAltText(studioImg, productName),
            GetAltText(sceneImg, "تصویر محیطی محصول"),
            true);
    }

    /// <summary>Scene hero must not collapse to studio 01.webp when both map to the same WebP.</summary>
    static string? ResolveSceneDisplayUrl(ProductImage sceneImg, IWebHostEnvironment? env)
    {
        var preferred = GetDisplayUrl(sceneImg, env);
        if (env is null || string.IsNullOrWhiteSpace(sceneImg.Url))
            return preferred;

        var raw = sceneImg.Url.StartsWith('/') ? sceneImg.Url : "/" + sceneImg.Url;
        if (UrlIsScene(raw) && !raw.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            var heroWebp = PreferWebp(raw, env);
            if (!string.IsNullOrEmpty(heroWebp) && !string.Equals(heroWebp, preferred, StringComparison.OrdinalIgnoreCase))
                return heroWebp;
            if (File.Exists(Path.Combine(env.WebRootPath, raw.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))))
                return raw;
        }

        return preferred;
    }

    public static IReadOnlyList<ProductGallerySlide> BuildGallery(IEnumerable<ProductImage> images, IWebHostEnvironment? env)
    {
        var list = images
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .ToList();

        if (list.Count == 0) return Array.Empty<ProductGallerySlide>();

        var sceneImg = FindSceneImage(list);
        var studioImg = FindStudioImage(list, sceneImg);

        var sceneUrl = GetDisplayUrl(sceneImg, env) ?? GetDisplayUrl(studioImg, env);
        var studioUrl = GetDisplayUrl(studioImg, env) ?? sceneUrl;

        if (string.IsNullOrEmpty(sceneUrl)) return Array.Empty<ProductGallerySlide>();

        var sceneAlt = GetAltText(sceneImg, "تصویر محیطی محصول");
        var studioAlt = GetAltText(studioImg, sceneAlt);

        return new List<ProductGallerySlide>
        {
            new(sceneUrl, true, sceneAlt),
            new(studioUrl!, false, studioAlt)
        };
    }

    static ProductImage? FindSceneImage(IReadOnlyList<ProductImage> list)
        => list.FirstOrDefault(i => i.Role == ProductImageRole.Scene)
           ?? list.FirstOrDefault(i => UrlLooksLikeScene(i.Url));

    static ProductImage? FindStudioImage(IReadOnlyList<ProductImage> list, ProductImage? scene)
        => list.FirstOrDefault(i => i.Role == ProductImageRole.Studio)
           ?? list.FirstOrDefault(i => i.IsPrimary && i != scene)
           ?? list.FirstOrDefault(i => UrlLooksLikeStudio(i.Url))
           ?? list.LastOrDefault(i => i != scene)
           ?? list.LastOrDefault();

    public static bool UrlIsScene(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && (url.Contains("hero", StringComparison.OrdinalIgnoreCase)
               || url.Contains("scene", StringComparison.OrdinalIgnoreCase)
               || url.Contains("-01-hero", StringComparison.OrdinalIgnoreCase));

    public static bool UrlIsStudio(string? url)
        => !string.IsNullOrWhiteSpace(url)
           && (url.Contains("studio", StringComparison.OrdinalIgnoreCase)
               || url.Contains("-02-", StringComparison.OrdinalIgnoreCase));

    /// <summary>گالری دستی اپراتور (01-hero / 02-studio) — نباید با LocalizeAll از آملون بازنویسی شود.</summary>
    public static bool HasOperatorGallery(IEnumerable<ProductImage> images, string? webRootPath, int productId)
    {
        var list = images.ToList();
        if (list.Any(i => UrlIsScene(i.Url) || UrlIsStudio(i.Url)))
            return true;

        if (string.IsNullOrWhiteSpace(webRootPath))
            return false;

        var dir = Path.Combine(webRootPath, "images", "products", productId.ToString("D4"));
        if (!Directory.Exists(dir))
            return false;

        return Directory.EnumerateFiles(dir).Any(f =>
        {
            var name = Path.GetFileName(f);
            return name.StartsWith("01-hero.", StringComparison.OrdinalIgnoreCase)
                   || name.StartsWith("02-studio.", StringComparison.OrdinalIgnoreCase);
        });
    }

    /// <summary>نقش Scene/Studio از نام فایل و SortOrder — قبل از Auto-Fix و بعد از ذخیرهٔ گالری.</summary>
    public static void ApplyGalleryRolesFromUrls(Product product)
    {
        var scene = product.Images.FirstOrDefault(i => i.Role == ProductImageRole.Scene)
                    ?? product.Images.FirstOrDefault(i => UrlIsScene(i.Url));
        var studio = product.Images.FirstOrDefault(i => i.Role == ProductImageRole.Studio)
                     ?? product.Images.FirstOrDefault(i => UrlIsStudio(i.Url))
                     ?? product.Images.FirstOrDefault(i => i.IsPrimary && i != scene);

        if (scene != null)
        {
            scene.Role = ProductImageRole.Scene;
            scene.SortOrder = 0;
            scene.IsPrimary = false;
        }

        if (studio != null && studio != scene)
        {
            studio.Role = ProductImageRole.Studio;
            studio.SortOrder = 1;
            studio.IsPrimary = true;
        }
        else if (studio == null && scene == null)
        {
            var legacy = product.Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenByDescending(i => i.SortOrder)
                .FirstOrDefault();
            if (legacy != null)
            {
                legacy.Role = ProductImageRole.Studio;
                legacy.SortOrder = 1;
                legacy.IsPrimary = true;
            }
        }

        foreach (var img in product.Images)
        {
            if (img == scene || img == studio) continue;
            if (img.IsPrimary) img.IsPrimary = false;
        }
    }

    static bool UrlLooksLikeScene(string url) => UrlIsScene(url);

    static bool UrlLooksLikeStudio(string url) => UrlIsStudio(url);

    public static string? GetWebpUrl(string? displayUrl)
    {
        if (string.IsNullOrWhiteSpace(displayUrl)) return null;
        if (displayUrl.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) return displayUrl;

        var webp = displayUrl.Replace("-original.png", ".webp", StringComparison.OrdinalIgnoreCase)
            .Replace("-original.jpg", ".webp", StringComparison.OrdinalIgnoreCase)
            .Replace("-original.jpeg", ".webp", StringComparison.OrdinalIgnoreCase);

        if (webp.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)) return webp;
        return null;
    }

    public static bool IsLocal(string? url)
        => !string.IsNullOrWhiteSpace(url) && url.StartsWith("/", StringComparison.Ordinal);

    /// <summary>اگر فایل Scene/Studio روی دیسک هست ولی رکورد DB پاک شده، دوباره لینک می‌کند.</summary>
    public static int RepairGalleriesFromDisk(Product product, string webRootPath)
    {
        var dir = Path.Combine(webRootPath, "images", "products", product.Id.ToString("D4"));
        if (!Directory.Exists(dir))
            return 0;

        var added = 0;
        added += EnsureDiskSlotLinked(product, dir, "01-hero", ProductImageRole.Scene, 0, false);
        added += EnsureDiskSlotLinked(product, dir, "02-studio", ProductImageRole.Studio, 1, true);
        if (added > 0)
            ApplyGalleryRolesFromUrls(product);
        return added;
    }

    static int EnsureDiskSlotLinked(
        Product product, string productDir, string stem, ProductImageRole role, int sortOrder, bool isPrimary)
    {
        var file = Directory.EnumerateFiles(productDir)
            .FirstOrDefault(f => Path.GetFileName(f).StartsWith(stem + ".", StringComparison.OrdinalIgnoreCase));
        if (file == null)
            return 0;

        var url = $"/images/products/{product.Id:D4}/{Path.GetFileName(file)}".Replace('\\', '/');
        var existing = product.Images.FirstOrDefault(i => i.Role == role)
                       ?? product.Images.FirstOrDefault(i =>
                           role == ProductImageRole.Scene ? UrlIsScene(i.Url) : UrlIsStudio(i.Url));
        if (existing != null)
        {
            if (!string.Equals(existing.Url, url, StringComparison.OrdinalIgnoreCase))
                existing.Url = url;
            existing.Role = role;
            existing.SortOrder = sortOrder;
            existing.IsPrimary = isPrimary;
            return 0;
        }

        product.Images.Add(new ProductImage
        {
            ProductId = product.Id,
            Url = url,
            Role = role,
            SortOrder = sortOrder,
            IsPrimary = isPrimary,
            AltText = product.Name
        });
        return 1;
    }
}
