using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>قفل محتوای دستی ادمین روی SeoProfile (جلوگیری از بازنویسی Auto-Fix).</summary>
public static class SeoProfileEditorialLocks
{
    public static async Task<SeoProfile> GetOrCreateAsync(
        ApplicationDbContext db, string entityType, int entityId, CancellationToken ct = default)
    {
        var profile = await db.SeoProfiles
            .FirstOrDefaultAsync(s => s.EntityType == entityType && s.EntityId == entityId, ct);
        if (profile is not null) return profile;
        profile = new SeoProfile { EntityType = entityType, EntityId = entityId };
        db.SeoProfiles.Add(profile);
        return profile;
    }

    /// <summary>فقط وقتی توضیح کوتاه واقعاً عوض شده قفل می‌شود — AiSummary برای GEO حفظ می‌شود.</summary>
    public static void OnManualProductShortSaved(
        SeoProfile profile, string? shortDescription, string? previousShortDescription)
    {
        var next = Normalize(shortDescription);
        var prev = Normalize(previousShortDescription);
        if (string.Equals(next, prev, StringComparison.Ordinal))
            return;

        profile.LockProductShortDescription = true;
        profile.UpdatedAt = DateTime.UtcNow;
    }

    public static void OnManualCategoryLeadSaved(
        SeoProfile profile, string? aiSummaryFromForm, bool aiSummaryFieldSubmitted)
    {
        profile.LockCategoryLead = true;
        if (aiSummaryFieldSubmitted && !string.IsNullOrWhiteSpace(aiSummaryFromForm))
            profile.AiSummary = aiSummaryFromForm.Trim();
        profile.UpdatedAt = DateTime.UtcNow;
    }

    public static void OnManualArticleSaved(
        SeoProfile profile,
        string? previousExcerpt,
        string? previousContentHtml,
        string? excerpt,
        string? contentHtml)
    {
        var excerptChanged = !string.Equals(Normalize(excerpt), Normalize(previousExcerpt), StringComparison.Ordinal);
        var contentChanged = !string.Equals(Normalize(contentHtml), Normalize(previousContentHtml), StringComparison.Ordinal);
        if (!excerptChanged && !contentChanged)
            return;

        if (excerptChanged && RequestHasMeaningful(excerpt))
            profile.LockArticleExcerpt = true;
        if (contentChanged && SeoStorefrontDisplay.HasSubstantialHtmlBody(contentHtml))
            profile.LockArticleContent = true;
        profile.UpdatedAt = DateTime.UtcNow;
    }

    static string? Normalize(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    static bool RequestHasMeaningful(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Trim().Length >= 3;
}
