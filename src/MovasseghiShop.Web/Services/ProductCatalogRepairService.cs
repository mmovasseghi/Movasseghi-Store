using System.Net;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface IProductCatalogRepairService
{
    Task<int> RepairAsync(CancellationToken ct = default);
}

/// <summary>Fixes corrupted product names, missing SKU codes, and duplicate پیش‌دستی mapping.</summary>
public class ProductCatalogRepairService(ApplicationDbContext db, ISeoEngineService seo) : IProductCatalogRepairService
{
    static readonly Dictionary<string, (string Code, string DisplayName, string PrimaryKw)> SkuMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["پیش-دستی"] = ("000530", "پیش دستی یکبار مصرف گیاهی (فله)", "پیش دستی یکبار مصرف گیاهی"),
        ["پیش-دستی-یکبار-مصرف-گیاهی"] = ("007712", "پیش دستی یکبار مصرف گیاهی — بسته ۱۲ عددی", "پیش دستی یکبار مصرف گیاهی"),
    };

    public async Task<int> RepairAsync(CancellationToken ct = default)
    {
        var changed = 0;
        var products = await db.Products.Include(p => p.Variants).ToListAsync(ct);

        foreach (var p in products)
        {
            if (!p.IsActive && HasSellableVariant(p))
            {
                p.IsActive = true;
                changed++;
            }

            var decoded = WebUtility.HtmlDecode(p.Name).Trim();
            if (decoded != p.Name)
            {
                p.Name = decoded;
                changed++;
            }

            if (p.Name.Contains("&#", StringComparison.Ordinal))
            {
                p.Name = WebUtility.HtmlDecode(p.Name);
                changed++;
            }

            foreach (var v in p.Variants)
            {
                var normalized = ProductStorefrontResolver.NormalizePackagingForStorage(v);
                if (v.PackagingType != normalized)
                {
                    v.PackagingType = normalized;
                    changed++;
                }
            }

            changed += RepairInactivePricedVariants(p);

            if (string.Equals(p.ProductCode, ProductStorefrontResolver.StrawProductCode, StringComparison.Ordinal)
                && string.Equals(p.Slug, ProductStorefrontResolver.StrawLegacySlug, StringComparison.Ordinal))
            {
                p.Slug = ProductStorefrontResolver.StrawCanonicalSlug;
                changed++;
            }

            if (SkuMap.TryGetValue(p.Slug, out var sku))
            {
                if (string.IsNullOrWhiteSpace(p.ProductCode) || p.ProductCode != sku.Code)
                {
                    p.ProductCode = sku.Code;
                    changed++;
                }

                if (p.Name.Contains("&#") || p.Name.Length > 80 || !p.Name.Contains("پیش", StringComparison.Ordinal))
                {
                    p.Name = sku.DisplayName;
                    changed++;
                }

                if (string.IsNullOrWhiteSpace(p.KeywordPrimary))
                {
                    p.KeywordPrimary = sku.PrimaryKw;
                    changed++;
                }

                if (string.IsNullOrWhiteSpace(p.KeywordSecondary))
                {
                    p.KeywordSecondary = SiteKeywordStrategy.Secondary;
                    changed++;
                }

                if (string.IsNullOrWhiteSpace(p.ProductType))
                {
                    p.ProductType = "پیش‌دستی";
                    changed++;
                }
            }
        }

        if (changed > 0)
        {
            await db.SaveChangesAsync(ct);

            // فقط وقتی داده عوض شد — Auto-Fix سنگین روی هر استارت نزن
            foreach (var id in new[] { 63, 64 })
            {
                if (await db.Products.AnyAsync(p => p.Id == id, ct))
                {
                    try { await seo.AutoFixProductAsync(id, ct); }
                    catch { /* ignore per-product */ }
                }
            }
        }

        return changed;
    }

    static bool HasSellableVariant(Product product)
        => ProductStorefrontResolver.HasSellableVariant(product);

    /// <summary>فعال‌کردن واریانت‌های قیمت‌دار غیرفعال و حذف SKUهای placeholder مثل AM-نی.</summary>
    static int RepairInactivePricedVariants(Product p)
    {
        var changed = 0;
        if (ProductStorefrontResolver.HasSellableVariant(p))
            return 0;

        var priced = p.Variants.Where(v => v.ShowPrice && v.Price > 0).ToList();
        if (priced.Count == 0)
            return 0;

        var activate = priced
            .GroupBy(v => v.SellUnit)
            .Select(g => g.OrderByDescending(v => CanonicalSkuScore(v)).ThenBy(v => v.Id).First())
            .ToList();

        foreach (var v in activate)
        {
            if (!v.IsActive)
            {
                v.IsActive = true;
                changed++;
            }
        }

        foreach (var v in p.Variants.Where(v => v.IsActive && IsJunkPlaceholderVariant(p, v)))
        {
            v.IsActive = false;
            changed++;
        }

        return changed;
    }

    static bool IsJunkPlaceholderVariant(Product p, ProductVariant v)
    {
        if (v.ShowPrice && v.Price > 0) return false;
        var sku = v.Sku ?? "";
        if (!sku.StartsWith("AM-", StringComparison.Ordinal)) return false;
        if (!string.IsNullOrEmpty(p.ProductCode) && sku.Contains(p.ProductCode, StringComparison.Ordinal))
            return false;
        if (sku.StartsWith($"AM-P{p.Id}-", StringComparison.Ordinal))
            return false;
        return true;
    }

    static int CanonicalSkuScore(ProductVariant v)
    {
        var sku = v.Sku ?? "";
        if (sku.StartsWith("AM-P", StringComparison.Ordinal)) return 0;
        if (sku.StartsWith("AM-", StringComparison.Ordinal)) return 10;
        return 1;
    }
}
