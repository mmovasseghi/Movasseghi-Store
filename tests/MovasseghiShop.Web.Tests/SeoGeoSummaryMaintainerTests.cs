using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class SeoGeoSummaryMaintainerTests
{
    [Fact]
    public void OnManualProductShortSaved_unchanged_preserves_ai_summary()
    {
        var profile = new SeoProfile { AiSummary = new string('گ', 350) };
        var shortText = new string('ک', 90);
        SeoProfileEditorialLocks.OnManualProductShortSaved(profile, shortText, shortText);
        Assert.False(profile.LockProductShortDescription);
        Assert.Equal(350, profile.AiSummary!.Length);
    }

    [Fact]
    public void OnManualProductShortSaved_when_changed_locks_without_clearing_geo()
    {
        var summary = new string('گ', 350);
        var profile = new SeoProfile { AiSummary = summary };
        SeoProfileEditorialLocks.OnManualProductShortSaved(profile, new string('ک', 90), "قبلی");
        Assert.True(profile.LockProductShortDescription);
        Assert.Equal(summary, profile.AiSummary);
    }

    [Fact]
    public void EnsureProduct_rebuilds_missing_geo_summary()
    {
        var product = new Product
        {
            Name = "کیک خوری",
            ProductCode = "000216",
            KeywordPrimary = "کیک خوری آملون",
            Material = "گیاهی",
            CapacityCc = 500
        };
        var profile = new SeoProfile();
        SeoGeoSummaryMaintainer.EnsureProduct(profile, product);
        Assert.True(SeoGeoSummaryMaintainer.IsAdequate(profile.AiSummary));
        Assert.Contains("000216", profile.AiSummary);
    }
}
