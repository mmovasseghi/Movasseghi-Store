using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>خلاصه GEO (AiSummary) برای امتیازدهی — مستقل از lead دستی فروشگاه.</summary>
public static class SeoGeoSummaryMaintainer
{
    public static bool IsAdequate(string? aiSummary) =>
        !string.IsNullOrWhiteSpace(aiSummary)
        && aiSummary.Trim().Length >= SeoContentRules.MinGeoSummaryLength;

    public static void EnsureProduct(SeoProfile profile, Product product)
    {
        if (IsAdequate(profile.AiSummary)) return;

        var primary = profile.FocusKeyword ?? product.KeywordPrimary ?? product.Name;
        var secondary = profile.SecondaryKeyword ?? product.KeywordSecondary ?? SiteKeywordStrategy.Secondary;
        profile.AiSummary = SeoUltimateWriter.BuildAiSummary(product, primary.Trim(), secondary.Trim());
        profile.UpdatedAt = DateTime.UtcNow;
    }

    public static void EnsureCategory(SeoProfile profile, Category category, int productCount = 0)
    {
        if (IsAdequate(profile.AiSummary)) return;

        var primary = profile.FocusKeyword ?? category.Name;
        var secondary = profile.SecondaryKeyword ?? SiteKeywordStrategy.Secondary;
        profile.AiSummary = SeoCategoryBuilder.BuildAiSummary(category, primary.Trim(), secondary.Trim());
        profile.UpdatedAt = DateTime.UtcNow;
    }

    public static void EnsureEditorial(SeoProfile profile, string focus, string title, int entityId)
    {
        if (IsAdequate(profile.AiSummary)) return;

        profile.AiSummary = SeoArticleBuilder.BuildExcerpt(focus.Trim(), title.Trim(), entityId);
        profile.UpdatedAt = DateTime.UtcNow;
    }
}
