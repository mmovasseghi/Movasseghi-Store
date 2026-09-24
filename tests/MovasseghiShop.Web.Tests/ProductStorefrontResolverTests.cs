using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;
using MovasseghiShop.Web.Services;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class ProductStorefrontResolverTests
{
    [Fact]
    public void HasSellableVariant_requires_active_priced_variant()
    {
        var p = new Product
        {
            Variants =
            [
                new ProductVariant { IsActive = true, ShowPrice = true, Price = 1000 },
                new ProductVariant { IsActive = false, ShowPrice = true, Price = 500 }
            ]
        };
        Assert.True(ProductStorefrontResolver.HasSellableVariant(p));

        p.Variants.First().IsActive = false;
        Assert.False(ProductStorefrontResolver.HasSellableVariant(p));
    }

    [Fact]
    public void SplitByPackaging_groups_sellable_when_packaging_type_was_zero()
    {
        var variants = ProductStorefrontResolver.GetSellableVariants([
            new ProductVariant
            {
                Id = 1, IsActive = true, ShowPrice = true, Price = 930_800,
                SellUnit = SellUnitType.BulkNylon, PackagingType = default, Sku = "AM-000107-BN"
            },
            new ProductVariant
            {
                Id = 2, IsActive = true, ShowPrice = true, Price = 13_031_200,
                SellUnit = SellUnitType.BulkCarton, PackagingType = default, Sku = "AM-000107-BC"
            }
        ]);

        var (bulk, shrink) = ProductStorefrontResolver.SplitByPackaging(variants);

        Assert.Equal(2, bulk.Count);
        Assert.Empty(shrink);
    }

    [Fact]
    public void GetSellableVariants_prefers_canonical_sku_per_sell_unit()
    {
        var sellable = ProductStorefrontResolver.GetSellableVariants([
            new ProductVariant { Id = 1, IsActive = true, ShowPrice = true, Price = 100, SellUnit = SellUnitType.BulkNylon, Sku = "AM-P86-BN" },
            new ProductVariant { Id = 2, IsActive = true, ShowPrice = true, Price = 100, SellUnit = SellUnitType.BulkNylon, Sku = "AM-000107-BN" }
        ]);

        Assert.Single(sellable);
        Assert.Equal("AM-000107-BN", sellable[0].Sku);
    }

    [Fact]
    public void Straw_legacy_slug_is_recognized_after_normalization()
    {
        Assert.True(ProductStorefrontResolver.IsStrawLegacySlug("نی"));
        Assert.True(ProductStorefrontResolver.IsStrawLegacySlug("%D9%86%DB%8C"));
        Assert.False(ProductStorefrontResolver.IsStrawLegacySlug("قاشق-چایخوری-یکبار-مصرف-گیاهی-بسته-24-عد"));
    }
}
