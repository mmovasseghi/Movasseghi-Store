using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface ISeoEngineService
{
    Task<SeoProfile> GetOrCreateProductProfileAsync(Product product, CancellationToken ct = default);
    Task<SeoScoreResult> AuditProductAsync(int productId, CancellationToken ct = default);
    Task<SeoScoreResult> AutoFixProductAsync(int productId, CancellationToken ct = default);
    Task<bool> CanPublishProductAsync(int productId, CancellationToken ct = default);
    (string Primary, string Secondary) SuggestKeywords(Product product);
    Task<BulkSeoResult> AutoFixAllProductsAsync(CancellationToken ct = default);

    Task<SeoProfile> GetOrCreateCategoryProfileAsync(Category category, CancellationToken ct = default);
    Task<SeoScoreResult> AuditCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<BulkSeoResult> AutoFixAllCategoriesAsync(CancellationToken ct = default);
    Task<SeoScoreResult> AutoFixCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<SeoScoreResult> AuditBlogAsync(int blogId, CancellationToken ct = default);
    Task<SeoScoreResult> AutoFixBlogAsync(int blogId, CancellationToken ct = default);
    Task<SeoScoreResult> AuditNewsAsync(int newsId, CancellationToken ct = default);
    Task<SeoScoreResult> AutoFixNewsAsync(int newsId, CancellationToken ct = default);
}

public class BulkSeoResult
{
    public int Total { get; set; }
    public int Fixed { get; set; }
    public int PublishReady { get; set; }
    public int Failed { get; set; }
    public double AverageScore { get; set; }
    public double AverageReadiness { get; set; }
}

/// <summary>
/// نمای سطح‌بالا روی Auto-Fix Ultimate — کنترلرها و ویوها با این کار می‌کنند.
/// همه امتیازدهی در <see cref="SeoUltimateAnalyzer"/> انجام می‌شود؛ اینجا فقط نگاشت و هماهنگی است.
/// </summary>
public class SeoEngineService(
    ApplicationDbContext db,
    ISeoAutoFixEngine engine,
    IOptions<SeoMaintenanceOptions> maintenanceOptions) : ISeoEngineService
{
    int BulkIterations => Math.Clamp(maintenanceOptions.Value.BulkFixMaxIterations, 3, 8);
    public const int PublishThreshold = SeoContentRules.PublishThreshold;

    // ── محصول ──

    public async Task<SeoScoreResult> AuditProductAsync(int productId, CancellationToken ct = default) =>
        Map(await engine.AuditProductAsync(productId, ct));

    public async Task<SeoScoreResult> AutoFixProductAsync(int productId, CancellationToken ct = default) =>
        Map(await engine.AutoFixProductAsync(productId, 5, SeoProductContentMode.RegenerateWhenUnlocked, ct));

    public async Task<bool> CanPublishProductAsync(int productId, CancellationToken ct = default) =>
        (await engine.AuditProductAsync(productId, ct)).IsPublishReady;

    public (string Primary, string Secondary) SuggestKeywords(Product product) =>
        (SiteKeywordStrategy.SuggestProductPrimary(product),
         SiteKeywordStrategy.SuggestProductSecondary(product));

    public async Task<BulkSeoResult> AutoFixAllProductsAsync(CancellationToken ct = default)
    {
        var ids = await db.Products.Select(p => p.Id).ToListAsync(ct);
        var result = new BulkSeoResult { Total = ids.Count };
        var scores = new List<int>();
        var readiness = new List<int>();

        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var report = await engine.AutoFixProductAsync(id, BulkIterations, SeoProductContentMode.PreserveLocked, ct);
                result.Fixed++;
                if (report.IsPublishReady) result.PublishReady++;
                scores.Add(report.OverallScore);
                readiness.Add(report.RankingReadiness);
            }
            catch
            {
                result.Failed++;
            }
        }

        result.AverageScore = scores.Count > 0 ? Math.Round(scores.Average(), 1) : 0;
        result.AverageReadiness = readiness.Count > 0 ? Math.Round(readiness.Average(), 1) : 0;
        return result;
    }

    // ── دسته ──

    public async Task<SeoScoreResult> AuditCategoryAsync(int categoryId, CancellationToken ct = default) =>
        Map(await engine.AuditCategoryAsync(categoryId, ct));

    public async Task<SeoScoreResult> AutoFixCategoryAsync(int categoryId, CancellationToken ct = default) =>
        Map(await engine.AutoFixCategoryAsync(categoryId, 5, ct));

    public async Task<SeoScoreResult> AuditBlogAsync(int blogId, CancellationToken ct = default) =>
        Map(await engine.AuditBlogAsync(blogId, ct));

    public async Task<SeoScoreResult> AutoFixBlogAsync(int blogId, CancellationToken ct = default) =>
        Map(await engine.AutoFixBlogAsync(blogId, 5, forceRegenerate: false, ct));

    public async Task<SeoScoreResult> AuditNewsAsync(int newsId, CancellationToken ct = default) =>
        Map(await engine.AuditNewsAsync(newsId, ct));

    public async Task<SeoScoreResult> AutoFixNewsAsync(int newsId, CancellationToken ct = default) =>
        Map(await engine.AutoFixNewsAsync(newsId, ct));

    public async Task<BulkSeoResult> AutoFixAllCategoriesAsync(CancellationToken ct = default)
    {
        var ids = await db.Categories.Select(c => c.Id).ToListAsync(ct);
        var result = new BulkSeoResult { Total = ids.Count };
        var scores = new List<int>();
        var readiness = new List<int>();

        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var report = await engine.AutoFixCategoryAsync(id, BulkIterations, ct);
                result.Fixed++;
                if (report.IsPublishReady) result.PublishReady++;
                scores.Add(report.OverallScore);
                readiness.Add(report.RankingReadiness);
            }
            catch
            {
                result.Failed++;
            }
        }

        result.AverageScore = scores.Count > 0 ? Math.Round(scores.Average(), 1) : 0;
        result.AverageReadiness = readiness.Count > 0 ? Math.Round(readiness.Average(), 1) : 0;
        return result;
    }

    // ── پروفایل ──

    public async Task<SeoProfile> GetOrCreateProductProfileAsync(Product product, CancellationToken ct = default)
    {
        var profile = await db.SeoProfiles
            .FirstOrDefaultAsync(p => p.EntityType == "product" && p.EntityId == product.Id, ct);
        if (profile != null) return profile;

        profile = new SeoProfile
        {
            EntityType = "product",
            EntityId = product.Id,
            FocusKeyword = product.KeywordPrimary ?? SiteKeywordStrategy.SuggestProductPrimary(product),
            SecondaryKeyword = product.KeywordSecondary ?? SiteKeywordStrategy.SuggestProductSecondary(product),
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            MetaKeywords = product.MetaKeywords
        };
        db.SeoProfiles.Add(profile);
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<SeoProfile> GetOrCreateCategoryProfileAsync(Category category, CancellationToken ct = default)
    {
        var profile = await db.SeoProfiles
            .FirstOrDefaultAsync(p => p.EntityType == "category" && p.EntityId == category.Id, ct);
        if (profile != null) return profile;

        var (primary, secondary) = SeoCategoryBuilder.SuggestKeywords(category);
        profile = new SeoProfile
        {
            EntityType = "category",
            EntityId = category.Id,
            FocusKeyword = primary,
            SecondaryKeyword = secondary,
            MetaTitle = category.MetaTitle,
            MetaDescription = category.MetaDescription
        };
        db.SeoProfiles.Add(profile);
        await db.SaveChangesAsync(ct);
        return profile;
    }

    // ── نگاشت ──

    /// <summary>
    /// نگاشت گزارش Ultimate به مدل ویو.
    /// نسخه قبلی نمره نهایی را <c>Math.Max(pro, legacy)</c> می‌گرفت که نمره را متورم می‌کرد و
    /// با هدف «رسیدن به ۱۰۰ باید سخت باشد» در تناقض بود. اکنون تنها یک منبع حقیقت وجود دارد.
    /// </summary>
    static SeoScoreResult Map(SeoUltimateReport r) => new()
    {
        SeoScore = r.SeoScore,
        AeoScore = r.AeoScore,
        GeoScore = r.GeoScore,
        ContentScore = r.ContentScore,
        MediaScore = r.ImageScore,
        TotalScore = SeoUltimateAnalyzer.ResolveDisplayOverall(r),
        IsPublishReady = r.IsPublishReady,
        WordCount = r.WordCount,
        KeywordDensity = r.KeywordDensity,
        HasSceneImage = r.HasSceneImage,
        HasStudioImage = r.HasStudioImage,
        Issues = r.CriticalIssues.Concat(r.Warnings).Take(20).ToList(),
        Suggestions = r.Opportunities.Take(15).ToList(),
        Checklist = r.PassedChecks.Take(30).ToList(),
        Ultimate = r,
        Pro = ToProReport(r)
    };

    /// <summary>سازگاری با ویوهای موجود که <see cref="SeoProReport"/> می‌خوانند.</summary>
    static SeoProReport ToProReport(SeoUltimateReport r) => new()
    {
        OnPageScore = r.SeoScore,
        ContentQualityScore = r.ContentScore,
        SemanticCoverageScore = r.SemanticScore,
        SearchIntentScore = r.IntentScore,
        ImageSeoScore = r.ImageScore,
        StructuredDataScore = r.SchemaScore,
        InternalLinkingScore = r.InternalLinkingScore,
        EcommerceSeoScore = r.ProductDataScore,
        TechnicalScore = r.TechnicalScore,
        PerformanceScore = r.TechnicalScore,
        UxScore = r.IntentScore,
        AeoScore = r.AeoScore,
        GeoScore = r.GeoScore,
        OverallScore = SeoUltimateAnalyzer.ResolveDisplayOverall(r),
        PrimaryKeywordCoverage = r.PrimaryKeywordCoverage,
        SemanticCoverage = r.SemanticCoverage,
        CompetitiveStrength = $"{r.CompetitiveStrength}/100 · {r.GradeFa}",
        CriticalIssues = r.CriticalIssues,
        Warnings = r.Warnings,
        Opportunities = r.Opportunities,
        PassedChecks = r.PassedChecks,
        ActionPlan = r.ActionPlan
    };
}
