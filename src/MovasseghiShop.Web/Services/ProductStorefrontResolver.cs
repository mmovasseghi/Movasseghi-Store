using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

public static class ProductStorefrontResolver
{
    /// <summary>کد آملون «نی یکبار مصرف گیاهی» — برای alias اسلاگ کوتاه «نی».</summary>
    public const string StrawProductCode = "000832";

    public const string StrawLegacySlug = "نی";

    public const string StrawCanonicalSlug = "نی-یکبار-مصرف-گیاهی";

    public static bool HasSellableVariant(Product product)
        => HasSellablePrice(product.Variants);

    public static IReadOnlyList<ProductVariant> GetSellableVariants(IEnumerable<ProductVariant> variants)
    {
        return variants
            .Where(v => v.IsActive && v.ShowPrice && v.Price > 0)
            .GroupBy(v => v.SellUnit)
            .Select(g => g.OrderByDescending(CanonicalSkuScore).ThenBy(v => v.Id).First())
            .OrderBy(v => v.SellUnit)
            .ToList();
    }

    public static decimal? GetMinPrice(IEnumerable<ProductVariant> variants)
    {
        var sellable = GetSellableVariants(variants);
        return sellable.Count > 0 ? sellable.Min(v => v.Price) : null;
    }

    public static bool HasSellablePrice(IEnumerable<ProductVariant> variants)
        => GetMinPrice(variants) is > 0;

    public static bool UseFromPricePrefix(IReadOnlyList<ProductVariant> sellable)
        => sellable.Select(v => v.Price).Distinct().Count() > 1;

    public static PackagingType EffectivePackaging(ProductVariant variant)
    {
        if (variant.PackagingType is PackagingType.Bulk or PackagingType.Shrink)
            return variant.PackagingType;

        return variant.SellUnit switch
        {
            SellUnitType.BulkNylon or SellUnitType.BulkCarton => PackagingType.Bulk,
            SellUnitType.ShrinkPack or SellUnitType.ShrinkCarton => PackagingType.Shrink,
            _ => InferPackagingFromSku(variant.Sku)
        };
    }

    public static (IReadOnlyList<ProductVariant> Bulk, IReadOnlyList<ProductVariant> Shrink) SplitByPackaging(
        IReadOnlyList<ProductVariant> sellable)
    {
        var bulk = new List<ProductVariant>();
        var shrink = new List<ProductVariant>();
        foreach (var v in sellable)
        {
            if (EffectivePackaging(v) == PackagingType.Shrink)
                shrink.Add(v);
            else
                bulk.Add(v);
        }

        return (bulk, shrink);
    }

    public static IReadOnlyList<ProductVariant> GetDisplayVariants(IEnumerable<ProductVariant> variants)
    {
        var sellable = GetSellableVariants(variants);
        if (sellable.Count > 0)
            return sellable;

        return variants
            .Where(v => v.IsActive)
            .GroupBy(v => v.SellUnit)
            .Select(g => g.OrderByDescending(CanonicalSkuScore).ThenBy(v => v.Id).First())
            .OrderBy(v => v.SellUnit)
            .ToList();
    }

    public static PackagingType NormalizePackagingForStorage(ProductVariant variant)
        => EffectivePackaging(variant);

    static int CanonicalSkuScore(ProductVariant v)
    {
        var sku = v.Sku ?? "";
        if (sku.Contains("-dup", StringComparison.OrdinalIgnoreCase)) return -20;
        if (sku.StartsWith("AM-P", StringComparison.Ordinal)) return 0;
        if (sku.StartsWith("AM-", StringComparison.Ordinal)) return 10;
        return 1;
    }

    static PackagingType InferPackagingFromSku(string? sku)
    {
        if (string.IsNullOrEmpty(sku)) return PackagingType.Bulk;
        if (sku.EndsWith("SP", StringComparison.Ordinal) || sku.EndsWith("SC", StringComparison.Ordinal)
            || sku.EndsWith("-S", StringComparison.Ordinal))
            return PackagingType.Shrink;
        return PackagingType.Bulk;
    }

    public static bool IsStrawLegacySlug(string? slug)
    {
        foreach (var candidate in SlugLookupCandidates(slug ?? ""))
        {
            if (string.Equals(candidate, StrawLegacySlug, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    public static string NormalizeSlugInput(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return "";
        return slug.Trim().Replace('ي', 'ی').Replace('ك', 'ک').Replace('ة', 'ه');
    }

    public static IEnumerable<string> SlugLookupCandidates(string slug)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        void Add(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            seen.Add(NormalizeSlugInput(value));
        }

        Add(slug);
        try { Add(Uri.UnescapeDataString(slug)); } catch (UriFormatException) { /* ignore */ }

        return seen;
    }

    public static async Task<Product?> ResolveActiveBySlugAsync(
        ApplicationDbContext db, string slug, CancellationToken ct = default)
    {
        foreach (var candidate in SlugLookupCandidates(slug))
        {
            var product = await LoadStorefrontProductQuery(db)
                .FirstOrDefaultAsync(p => p.Slug == candidate && p.IsActive, ct);
            if (product != null)
                return product;

            if (IsStrawLegacySlug(candidate))
            {
                var straw = await LoadStorefrontProductQuery(db)
                    .FirstOrDefaultAsync(p => p.IsActive && p.ProductCode == StrawProductCode, ct);
                if (straw != null)
                    return straw;
            }
        }

        return null;
    }

    public static async Task<Product?> FindInactiveBySlugAsync(
        ApplicationDbContext db, string slug, CancellationToken ct = default)
    {
        foreach (var candidate in SlugLookupCandidates(slug))
        {
            var product = await db.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Slug == candidate && !p.IsActive, ct);
            if (product != null)
                return product;

            if (IsStrawLegacySlug(candidate))
            {
                var straw = await db.Products
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(p => !p.IsActive && p.ProductCode == StrawProductCode, ct);
                if (straw != null)
                    return straw;
            }
        }

        return null;
    }

    static IQueryable<Product> LoadStorefrontProductQuery(ApplicationDbContext db)
        => db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder))
            .Include(p => p.Variants.Where(v => v.IsActive));

    public static async Task<Product?> FindActiveAlternateAsync(ApplicationDbContext db, Product inactive, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(inactive.ProductCode))
        {
            var sameCode = await db.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Id != inactive.Id && p.ProductCode == inactive.ProductCode)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync(ct);
            if (sameCode != null) return sameCode;
        }

        if (!string.IsNullOrWhiteSpace(inactive.Slug) && inactive.Slug.Length >= 4)
        {
            var prefix = inactive.Slug;
            var slugFamily = await db.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Id != inactive.Id &&
                            (p.Slug == prefix || p.Slug.StartsWith(prefix + "-")))
                .OrderBy(p => p.Slug.Length)
                .ThenBy(p => p.Id)
                .FirstOrDefaultAsync(ct);
            if (slugFamily != null) return slugFamily;
        }

        // بدون حدس از روی نام/دسته — «نی» و مشابه به قاشق چای‌خوری redirect نمی‌شوند.
        return null;
    }
}
