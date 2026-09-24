using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Services.Editorial;

/// <summary>تصویر شاخص از گالری محصولات موجود — بدون دانلود خارجی.</summary>
public static class EditorialImageResolver
{
    public static bool IsPlaceholderFeatured(string? url) =>
        string.IsNullOrWhiteSpace(url)
        || url.Contains("originallogo", StringComparison.OrdinalIgnoreCase);

    public static async Task<string> ResolveFeaturedImageAsync(
        ApplicationDbContext db, string focus, string? secondary, CancellationToken ct = default)
    {
        var (featured, _, _) = await ResolveArticleImagesAsync(db, focus, secondary, ct);
        return featured;
    }

    /// <summary>نمایه ترجیحاً Scene؛ بدنه حداقل Scene + Studio با alt مناسب.</summary>
    public static async Task<(string FeaturedUrl, string FeaturedHint, IReadOnlyList<EditorialImagePick> Inline)> ResolveArticleImagesAsync(
        ApplicationDbContext db, string focus, string? secondary, CancellationToken ct = default)
    {
        var ranked = await RankProductsAsync(db, focus, secondary, ct);
        var picks = new List<EditorialImagePick>();

        void TryUrl(string? url, string roleFa, string? productName)
        {
            if (string.IsNullOrWhiteSpace(url) || IsPlaceholderFeatured(url)) return;
            if (picks.Any(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase))) return;
            picks.Add(new EditorialImagePick(url, roleFa, productName));
        }

        void TryPick(Product? p, ProductImageRole role, string roleFa) =>
            TryUrl(PickRoleUrl(p, role), roleFa, p?.Name);

        foreach (var p in ranked.Take(4))
        {
            TryPick(p, ProductImageRole.Scene, "🌅 Scene — تصویر محیط");
            TryPick(p, ProductImageRole.Studio, "📦 Studio — محصول روی سفید");
        }

        foreach (var p in ranked)
        {
            foreach (var img in p.Images.OrderBy(i => i.SortOrder))
            {
                if (IsPlaceholderFeatured(img.Url)) continue;
                var roleFa = img.Role == ProductImageRole.Scene
                    ? "🌅 Scene — تصویر محیط"
                    : img.Role == ProductImageRole.Studio
                        ? "📦 Studio — محصول روی سفید"
                        : "کاتالوگ";
                TryUrl(img.Url, roleFa, p.Name);
                if (picks.Count >= 5) break;
            }

            if (picks.Count >= 5) break;
        }

        var best = ranked.FirstOrDefault();
        var featuredUrl = PickRoleUrl(best, ProductImageRole.Scene)
            ?? PickRoleUrl(best, ProductImageRole.Studio)
            ?? picks.FirstOrDefault()?.Url
            ?? "/images/brand/originallogo.png";
        var featuredHint = featuredUrl.Contains("scene", StringComparison.OrdinalIgnoreCase)
            ? "🌅 Scene"
            : featuredUrl.Contains("studio", StringComparison.OrdinalIgnoreCase)
                ? "📦 Studio"
                : "کاتالوگ";

        var inline = picks
            .Where(p => !string.Equals(p.Url, featuredUrl, StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToList();

        while (inline.Count < 3 && picks.Count > inline.Count)
        {
            var extra = picks.FirstOrDefault(p => inline.All(i => !string.Equals(i.Url, p.Url, StringComparison.OrdinalIgnoreCase)));
            if (extra is null) break;
            inline.Add(extra);
        }

        return (featuredUrl, featuredHint, inline);
    }

    static async Task<List<Product>> RankProductsAsync(
        ApplicationDbContext db, string focus, string? secondary, CancellationToken ct)
    {
        var tokens = SeoText.Tokenize(focus)
            .Concat(SeoText.Tokenize(secondary))
            .Where(t => t.Length > 2)
            .Distinct()
            .Take(6)
            .ToList();

        var products = await db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Images)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        return products
            .OrderByDescending(p => ScoreProduct(p, tokens))
            .Where(p => p.Images.Any(i => !IsPlaceholderFeatured(i.Url)))
            .ToList();
    }

    /// <summary>۲–۳ تصویر متمایز برای درج در بدنه — ترجیح Scene و Studio از کاتالوگ.</summary>
    public static async Task<IReadOnlyList<string>> ResolveInlineImageUrlsAsync(
        ApplicationDbContext db, string focus, string? secondary, int max = 3, CancellationToken ct = default)
    {
        max = Math.Clamp(max, 2, 5);
        var tokens = SeoText.Tokenize(focus)
            .Concat(SeoText.Tokenize(secondary))
            .Where(t => t.Length > 2)
            .Distinct()
            .Take(6)
            .ToList();

        var products = await db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Images)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        var ranked = products.OrderByDescending(p => ScoreProduct(p, tokens)).ToList();
        var urls = new List<string>();

        void TryAdd(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || IsPlaceholderFeatured(url)) return;
            if (urls.Contains(url, StringComparer.OrdinalIgnoreCase)) return;
            urls.Add(url);
        }

        foreach (var role in new[] { ProductImageRole.Scene, ProductImageRole.Studio })
        {
            foreach (var p in ranked)
                TryAdd(PickRoleUrl(p, role));
            if (urls.Count >= max) return urls.Take(max).ToList();
        }

        foreach (var p in ranked)
        {
            foreach (var img in p.Images.OrderBy(i => i.SortOrder))
            {
                TryAdd(img.Url);
                if (urls.Count >= max) return urls;
            }
        }

        return urls;
    }

    static string? PickRoleUrl(Product? product, ProductImageRole role) =>
        product?.Images
            .Where(i => i.Role == role && !string.IsNullOrWhiteSpace(i.Url))
            .OrderBy(i => i.SortOrder)
            .Select(i => i.Url)
            .FirstOrDefault(u => !IsPlaceholderFeatured(u));

    static int ScoreProduct(Product p, IReadOnlyList<string> tokens)
    {
        var hay = $"{p.Name} {p.ProductType} {p.Applications} {p.Material}";
        return tokens.Count(t => hay.Contains(t, StringComparison.OrdinalIgnoreCase));
    }
}
