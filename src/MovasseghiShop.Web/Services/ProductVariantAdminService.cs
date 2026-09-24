using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Admin;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

public interface IProductVariantAdminService
{
    ProductPackagingViewModel BuildEditorModel(Product product);
    Task ApplyAsync(Product product, ProductPackagingInput input, CancellationToken ct = default);
}

public class ProductVariantAdminService(ApplicationDbContext db) : IProductVariantAdminService
{
    public ProductPackagingViewModel BuildEditorModel(Product product)
    {
        var prefix = GetSkuPrefix(product);
        var variants = product.Variants.ToList();
        var form = new ProductPackagingInput();

        var bulkNylon = FindVariant(variants, prefix, SellUnitType.BulkNylon, PackagingType.Bulk, "BN", "B");
        var bulkCarton = FindVariant(variants, prefix, SellUnitType.BulkCarton, PackagingType.Bulk, "BC", null);
        var shrinkPack = FindVariant(variants, prefix, SellUnitType.ShrinkPack, PackagingType.Shrink, "SP", "S");
        var shrinkCarton = FindVariant(variants, prefix, SellUnitType.ShrinkCarton, PackagingType.Shrink, "SC", null);

        form.BulkEnabled = variants.Any(v => v.PackagingType == PackagingType.Bulk && v.IsActive)
            || bulkNylon != null
            || HasLegacyBulk(variants, prefix);

        form.ShrinkEnabled = variants.Any(v =>
            v.PackagingType == PackagingType.Shrink && v.IsActive
            && (v.Sku.EndsWith("-SP", StringComparison.Ordinal)
                || v.Sku.EndsWith("-SC", StringComparison.Ordinal)
                || v.Sku == $"AM-{prefix}-S"));
        if (bulkNylon != null || HasLegacyBulk(variants, prefix))
        {
            var src = bulkNylon ?? variants.First(v => v.Sku == $"AM-{prefix}-B");
            form.BulkNylonUnits = src.UnitsPerPack > 0 ? src.UnitsPerPack : 200;
            form.BulkNylonPrice = NormalizeBulkNylonPrice(src);
            form.BulkNylonShowPrice = src.ShowPrice;
        }

        form.BulkCartonEnabled = bulkCarton?.IsActive == true;
        if (bulkCarton != null)
        {
            form.BulkCartonNylons = bulkCarton.PacksPerCarton > 0 ? bulkCarton.PacksPerCarton : 1;
            form.BulkCartonTotalUnits = bulkCarton.UnitsPerCarton;
            form.BulkCartonPrice = NormalizePrice(bulkCarton.Price);
            form.BulkCartonShowPrice = bulkCarton.ShowPrice;
        }

        if (shrinkPack != null || HasLegacyShrink(variants, prefix) || shrinkCarton != null)
        {
            var src = shrinkPack ?? variants.First(v => v.Sku == $"AM-{prefix}-S");
            form.ShrinkPackUnits = src.UnitsPerPack > 0 ? src.UnitsPerPack : 12;
            form.ShrinkPackPrice = NormalizeShrinkPackPrice(src);
            form.ShrinkPackShowPrice = src.ShowPrice;
        }

        form.ShrinkCartonEnabled = shrinkCarton?.IsActive == true;
        if (shrinkCarton != null)
        {
            form.ShrinkCartonPacks = shrinkCarton.PacksPerCarton > 0 ? shrinkCarton.PacksPerCarton : 1;
            form.ShrinkCartonTotalUnits = shrinkCarton.UnitsPerCarton;
            form.ShrinkCartonPrice = NormalizePrice(shrinkCarton.Price);
            form.ShrinkCartonShowPrice = shrinkCarton.ShowPrice;
        }

        return new ProductPackagingViewModel { Form = form, SkuPrefix = prefix };
    }

    public async Task ApplyAsync(Product product, ProductPackagingInput input, CancellationToken ct = default)
    {
        var prefix = GetSkuPrefix(product);

        if (input.BulkEnabled)
        {
            if (input.BulkNylonUnits < 1) input.BulkNylonUnits = 1;
            Upsert(product, prefix, "BN", SellUnitType.BulkNylon, PackagingType.Bulk,
                input.BulkNylonPrice, input.BulkNylonUnits, input.BulkNylonUnits, 0,
                $"نایلون {input.BulkNylonUnits:N0} عددی",
                input.BulkNylonShowPrice && input.BulkNylonPrice > 0);

            if (input.BulkCartonEnabled && input.BulkCartonNylons > 0)
            {
                var totalUnits = input.BulkCartonTotalUnits > 0
                    ? input.BulkCartonTotalUnits
                    : input.BulkNylonUnits * input.BulkCartonNylons;
                var price = input.BulkCartonPrice > 0
                    ? input.BulkCartonPrice
                    : input.BulkNylonPrice * input.BulkCartonNylons;
                Upsert(product, prefix, "BC", SellUnitType.BulkCarton, PackagingType.Bulk,
                    price, input.BulkNylonUnits, totalUnits, input.BulkCartonNylons,
                    $"کارتن {totalUnits:N0} عددی ({input.BulkCartonNylons} نایلون)",
                    input.BulkCartonShowPrice && price > 0);
            }
            else
                DeactivateBySuffix(product, prefix, "BC");
        }
        else
        {
            DeactivateBulk(product, prefix);
        }

        if (input.ShrinkEnabled)
        {
            if (input.ShrinkPackUnits < 1) input.ShrinkPackUnits = 1;
            Upsert(product, prefix, "SP", SellUnitType.ShrinkPack, PackagingType.Shrink,
                input.ShrinkPackPrice, input.ShrinkPackUnits, input.ShrinkPackUnits, 0,
                $"بسته {input.ShrinkPackUnits} عددی",
                input.ShrinkPackShowPrice && input.ShrinkPackPrice > 0);

            if (input.ShrinkCartonEnabled && input.ShrinkCartonPacks > 0)
            {
                var totalUnits = input.ShrinkCartonTotalUnits > 0
                    ? input.ShrinkCartonTotalUnits
                    : input.ShrinkPackUnits * input.ShrinkCartonPacks;
                var price = input.ShrinkCartonPrice > 0
                    ? input.ShrinkCartonPrice
                    : input.ShrinkPackPrice * input.ShrinkCartonPacks;
                Upsert(product, prefix, "SC", SellUnitType.ShrinkCarton, PackagingType.Shrink,
                    price, input.ShrinkPackUnits, totalUnits, input.ShrinkCartonPacks,
                    $"کارتن {input.ShrinkCartonPacks} بسته ({totalUnits:N0} عدد)",
                    input.ShrinkCartonShowPrice && price > 0);
            }
            else
                DeactivateBySuffix(product, prefix, "SC");
        }
        else
        {
            DeactivateShrink(product, prefix);
        }

        DeactivateLegacyAndClones(product, prefix);
    }

    string GetSkuPrefix(Product product)
    {
        var code = product.ProductCode?.Trim();
        if (string.IsNullOrWhiteSpace(code))
            return $"P{product.Id}";
        if (DuplicateProductCode(product.Id, code))
            return $"P{product.Id}";
        if (SkuPrefixTakenByOtherProduct(product.Id, code))
            return $"P{product.Id}";
        return code;
    }

    bool DuplicateProductCode(int productId, string code) =>
        db.Products.AsNoTracking().Any(p => p.Id != productId && p.ProductCode == code);

    /// <summary>کد آملون تکراری — SKU canonical قبلاً برای محصول دیگری ثبت شده (مثلاً 81 و 90 هر دو 000217).</summary>
    bool SkuPrefixTakenByOtherProduct(int productId, string prefix)
    {
        // SQLite/EF: StartsWith(..., StringComparison) ترجمه نمی‌شود — فقط overload تک‌پارامتری یا Contains روی لیست ثابت.
        var owned = CanonicalSkusForPrefix(prefix);
        return db.ProductVariants.AsNoTracking().Any(v =>
            v.ProductId != productId && owned.Contains(v.Sku));
    }

    static IReadOnlyList<string> CanonicalSkusForPrefix(string prefix) =>
    [
        $"AM-{prefix}-BN", $"AM-{prefix}-BC", $"AM-{prefix}-SP", $"AM-{prefix}-SC",
        $"AM-{prefix}-B", $"AM-{prefix}-S"
    ];

    static ProductVariant? FindVariant(
        List<ProductVariant> variants, string prefix, SellUnitType sellUnit, PackagingType packaging,
        string suffix, string? legacySuffix)
    {
        var canonical = variants.FirstOrDefault(v => v.Sku == $"AM-{prefix}-{suffix}");
        if (canonical != null) return canonical;

        var byUnit = variants.FirstOrDefault(v =>
            v.SellUnit == sellUnit && v.PackagingType == packaging);
        if (byUnit != null) return byUnit;

        if (legacySuffix != null)
        {
            var legacy = variants.FirstOrDefault(v => v.Sku == $"AM-{prefix}-{legacySuffix}");
            if (legacy != null) return legacy;
        }

        return null;
    }

    static bool HasLegacyBulk(List<ProductVariant> variants, string prefix) =>
        variants.Any(v => v.Sku == $"AM-{prefix}-B" && v.PackagingType == PackagingType.Bulk);

    static bool HasLegacyShrink(List<ProductVariant> variants, string prefix) =>
        variants.Any(v => v.Sku == $"AM-{prefix}-S" && v.PackagingType == PackagingType.Shrink);

    static decimal NormalizeBulkNylonPrice(ProductVariant src)
    {
        if (src.SellUnit == SellUnitType.BulkNylon) return src.Price;
        // legacy B often stored rials×10 or whole list price — 72600 → 7260 per nylon
        if (src.UnitsPerPack > 1 && src.Price >= 50_000)
            return Math.Round(src.Price / 10m, 0);
        return src.Price;
    }

    static decimal NormalizePrice(decimal price) => price > 0 ? price : 0;

    static decimal NormalizeShrinkPackPrice(ProductVariant src)
    {
        if (src.SellUnit == SellUnitType.ShrinkPack) return src.Price;
        if (src.SellUnit == default && src.PackagingType == PackagingType.Shrink && src.UnitsPerPack <= 24)
            return src.Price > 100_000 ? Math.Round(src.Price / src.UnitsPerPack, 0) : src.Price;
        return src.Price;
    }

    void Upsert(Product product, string prefix, string suffix, SellUnitType sellUnit, PackagingType packaging,
        decimal price, int unitsPerPack, int unitsPerCarton, int packsPerCarton, string packLabel, bool showPrice)
    {
        var sku = ResolveSku(product, prefix, suffix, sellUnit, packaging);
        var variant = product.Variants.FirstOrDefault(v => v.Sku == sku);
        if (variant == null)
        {
            variant = product.Variants.FirstOrDefault(v =>
                v.SellUnit == sellUnit && v.PackagingType == packaging && v.IsActive);
            if (variant == null)
            {
                variant = product.Variants.FirstOrDefault(v =>
                    v.SellUnit == sellUnit && v.PackagingType == packaging);
            }
            if (variant == null)
            {
                variant = new ProductVariant { ProductId = product.Id, Sku = sku };
                product.Variants.Add(variant);
                db.ProductVariants.Add(variant);
            }
            else
            {
                TryAssignSku(product, variant, sku);
            }
        }
        else
        {
            TryAssignSku(product, variant, sku);
        }

        variant.IsActive = true;
        variant.SellUnit = sellUnit;
        variant.PackagingType = packaging;
        variant.Price = price;
        variant.ShowPrice = showPrice;
        variant.UnitsPerPack = unitsPerPack;
        variant.UnitsPerCarton = unitsPerCarton;
        variant.PacksPerCarton = packsPerCarton;
        variant.PackLabel = packLabel;
    }

    string ResolveSku(Product product, string prefix, string suffix, SellUnitType sellUnit, PackagingType packaging)
    {
        var candidates = new[]
        {
            $"AM-{prefix}-{suffix}",
            $"AM-P{product.Id}-{suffix}",
            $"AM-P{product.Id}-{suffix}-alt"
        };
        foreach (var candidate in candidates)
        {
            if (IsSkuAvailable(product, variantId: 0, candidate))
                return candidate;
        }
        return $"AM-P{product.Id}-{suffix}-{suffix}";
    }

    void TryAssignSku(Product product, ProductVariant variant, string sku)
    {
        if (string.Equals(variant.Sku, sku, StringComparison.Ordinal))
            return;
        if (IsSkuAvailable(product, variant.Id, sku))
            variant.Sku = sku;
    }

    bool IsSkuAvailable(Product product, int variantId, string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return false;
        if (product.Variants.Any(v => v.Id != variantId && string.Equals(v.Sku, sku, StringComparison.Ordinal)))
            return false;
        return !db.ProductVariants.AsNoTracking().Any(v =>
            v.Sku == sku && v.ProductId != product.Id && (variantId <= 0 || v.Id != variantId));
    }

    static void DeactivateBySuffix(Product product, string prefix, string suffix)
    {
        foreach (var v in product.Variants.Where(v => v.Sku == $"AM-{prefix}-{suffix}"))
            v.IsActive = false;
    }

    static void DeactivateBulk(Product product, string prefix)
    {
        foreach (var v in product.Variants.Where(v => v.PackagingType == PackagingType.Bulk))
            v.IsActive = false;
    }

    static void DeactivateShrink(Product product, string prefix)
    {
        foreach (var v in product.Variants.Where(v => v.PackagingType == PackagingType.Shrink))
            v.IsActive = false;
    }

    static void DeactivateLegacyAndClones(Product product, string prefix)
    {
        var idPrefix = $"P{product.Id}";
        var usingIdPrefix = string.Equals(prefix, idPrefix, StringComparison.Ordinal);
        var canonical = new HashSet<string>(StringComparer.Ordinal)
        {
            $"AM-{prefix}-BN", $"AM-{prefix}-BC", $"AM-{prefix}-SP", $"AM-{prefix}-SC"
        };
        foreach (var v in product.Variants)
        {
            if (v.Sku == $"AM-{prefix}-B" || v.Sku == $"AM-{prefix}-S")
                v.IsActive = false;
            // فقط وقتی پیشوند کد آملون است، کلون‌های AM-P{id}- را خاموش کن — نه وقتی خودمان همان P{id} را canonical داریم.
            if (!usingIdPrefix && v.Sku.StartsWith($"AM-P{product.Id}-", StringComparison.Ordinal))
                v.IsActive = false;
            if (v.Sku.StartsWith($"AM-{prefix}-", StringComparison.Ordinal) && !canonical.Contains(v.Sku))
                v.IsActive = false;
        }
    }
}
