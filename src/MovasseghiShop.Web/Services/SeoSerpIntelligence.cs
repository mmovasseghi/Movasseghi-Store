using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>
/// SERP Intelligence — می‌فهمد برای یک Keyword چه چیزی رتبه می‌گیرد و
/// میانگین رقبای Top 10 را به‌عنوان Baseline مقایسه می‌سازد.
/// </summary>
public interface ISeoSerpIntelligence
{
    Task<SeoSerpBaseline> GetBaselineAsync(string keyword, CancellationToken ct = default);
    Task<List<CompetitorSnapshot>> GetCompetitorsAsync(string keyword, CancellationToken ct = default);
    Task<List<GscQueryRow>> GetSearchConsoleQueriesAsync(CancellationToken ct = default);
    Task<List<string>> GetSerpTitlesAsync(string keyword, CancellationToken ct = default);
}

public class SeoSerpIntelligence(
    ApplicationDbContext db,
    IGoogleSearchConsoleService gsc,
    IMemoryCache cache,
    ILogger<SeoSerpIntelligence> logger) : ISeoSerpIntelligence
{
    const string GscCacheKey = "seo:gsc:top-queries";
    static readonly TimeSpan GscTtl = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Baseline صنعتی — وقتی داده واقعی SERP همگام‌سازی نشده باشد.
    /// این اعداد محافظه‌کارانه‌اند تا سیستم بی‌دلیل «قدرت رقابتی بالا» اعلام نکند.
    /// </summary>
    static SeoSerpBaseline IndustryDefault(string keyword) => new()
    {
        Keyword = keyword,
        HasLiveData = false,
        ContentBenchmark = 82,
        SemanticBenchmark = 88,
        IntentBenchmark = 91,
        EntityBenchmark = 79,
        AeoBenchmark = 71,
        ProductBenchmark = 86,
        ImageBenchmark = 74,
        SchemaBenchmark = 80
    };

    public async Task<SeoSerpBaseline> GetBaselineAsync(string keyword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return IndustryDefault("");

        var competitors = await GetCompetitorsAsync(keyword, ct);
        var baseline = IndustryDefault(keyword);

        if (competitors.Count == 0) return await AttachOurPositionAsync(baseline, keyword, ct);

        baseline.HasLiveData = true;
        baseline.CompetitorsAnalyzed = competitors.Count;
        baseline.DataDate = competitors.Max(c => c.CheckedAt);

        var wordCounts = competitors
            .Where(c => c.WordCountEstimate is > 0)
            .Select(c => c.WordCountEstimate!.Value)
            .ToList();

        if (wordCounts.Count > 0)
        {
            baseline.AvgCompetitorWordCount = (int)Math.Round(wordCounts.Average());
            // عمق محتوای رقبا نسبت به هدف ما — سقف ۹۷ چون رقیب هم کامل نیست
            baseline.ContentBenchmark = Clamp(
                (int)Math.Round(baseline.AvgCompetitorWordCount * 78.0 / SeoContentRules.MinWordCount), 55, 97);
        }

        baseline.FaqSchemaShare = Math.Round(
            competitors.Count(c => c.HasFaqSchema) * 1.0 / competitors.Count, 2);

        baseline.AeoBenchmark = Clamp((int)Math.Round(40 + baseline.FaqSchemaShare * 55), 40, 95);
        baseline.SchemaBenchmark = Clamp((int)Math.Round(58 + baseline.FaqSchemaShare * 37), 58, 95);

        // تنوع دامنه: SERP متنوع = رقابت پراکنده‌تر، SERP متمرکز = برندهای قوی
        var domainShare = competitors
            .GroupBy(c => c.Domain, StringComparer.OrdinalIgnoreCase)
            .Max(g => g.Count()) * 1.0 / competitors.Count;
        var concentration = (int)Math.Round(domainShare * 100);
        baseline.EntityBenchmark = Clamp(72 + concentration / 5, 72, 94);
        baseline.SemanticBenchmark = Clamp(baseline.ContentBenchmark + 6, 70, 95);

        return await AttachOurPositionAsync(baseline, keyword, ct);
    }

    async Task<SeoSerpBaseline> AttachOurPositionAsync(
        SeoSerpBaseline baseline, string keyword, CancellationToken ct)
    {
        var snapshot = await db.RankSnapshots
            .Where(s => s.Keyword == keyword && s.Position > 0)
            .OrderByDescending(s => s.CheckedAt)
            .FirstOrDefaultAsync(ct);

        baseline.OurPosition = snapshot?.Position;
        return baseline;
    }

    public async Task<List<CompetitorSnapshot>> GetCompetitorsAsync(
        string keyword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return [];

        var exact = await LoadLatestAsync(keyword, ct);
        if (exact.Count > 0) return exact;

        // Keyword محصول معمولاً pillar نیست — نزدیک‌ترین pillar را پیدا می‌کنیم
        var pillar = await FindClosestTrackedKeywordAsync(keyword, ct);
        return pillar is null ? [] : await LoadLatestAsync(pillar, ct);
    }

    async Task<List<CompetitorSnapshot>> LoadLatestAsync(string keyword, CancellationToken ct)
    {
        var latest = await db.CompetitorSnapshots
            .Where(s => s.Keyword == keyword)
            .OrderByDescending(s => s.CheckedAt)
            .Select(s => (DateTime?)s.CheckedAt)
            .FirstOrDefaultAsync(ct);

        if (latest is null) return [];

        // همان دور جمع‌آوری (پنجره ۱ روزه برای اختلاف ثانیه‌ها)
        var from = latest.Value.AddDays(-1);
        return await db.CompetitorSnapshots
            .Where(s => s.Keyword == keyword && s.CheckedAt >= from)
            .OrderBy(s => s.RankPosition)
            .Take(10)
            .ToListAsync(ct);
    }

    async Task<string?> FindClosestTrackedKeywordAsync(string keyword, CancellationToken ct)
    {
        var tracked = await db.CompetitorSnapshots
            .Select(s => s.Keyword)
            .Distinct()
            .ToListAsync(ct);

        return tracked
            .Select(k => (Keyword: k, Score: SeoText.Similarity(k, keyword)))
            .Where(x => x.Score >= 0.34)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Keyword)
            .FirstOrDefault();
    }

    public async Task<List<string>> GetSerpTitlesAsync(string keyword, CancellationToken ct = default)
    {
        var competitors = await GetCompetitorsAsync(keyword, ct);
        return competitors
            .Select(c => c.Title ?? "")
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }

    /// <summary>
    /// Query‌های واقعی Search Console — کش ۶۰ دقیقه‌ای چون Audit روی هر بازدید صفحه اجرا می‌شود
    /// و نباید هر بار به API گوگل درخواست بزند.
    /// </summary>
    public async Task<List<GscQueryRow>> GetSearchConsoleQueriesAsync(CancellationToken ct = default)
    {
        if (!gsc.IsConfigured) return [];
        if (cache.TryGetValue(GscCacheKey, out List<GscQueryRow>? cached) && cached is not null)
            return cached;

        try
        {
            var result = await gsc.GetTopQueriesAsync(28, ct);
            var rows = result.TopQueries ?? [];
            cache.Set(GscCacheKey, rows, GscTtl);
            return rows;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Search Console query fetch failed — continuing without query data");
            cache.Set(GscCacheKey, new List<GscQueryRow>(), TimeSpan.FromMinutes(10));
            return [];
        }
    }

    static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
}

/// <summary>محاسبه قدرت رقابتی و فاصله ابعاد نسبت به میانگین SERP.</summary>
public static class SeoCompetitiveModel
{
    /// <summary>
    /// هر بُعد: برابری با رقیب = ۵۰. برای رسیدن به ۱۰۰ باید واضحاً از رقبا جلو بزنید.
    /// این باعث می‌شود «قدرت رقابتی» با چند Auto-Fix ساده اشباع نشود.
    /// </summary>
    public static (int Strength, List<SeoDimensionGap> Gaps) Evaluate(
        SeoSerpBaseline baseline,
        int contentScore,
        int semanticScore,
        int intentScore,
        double entityCoverage,
        int aeoScore,
        int productScore,
        int imageScore,
        int schemaScore)
    {
        var rows = new (string Key, string Fa, int Ours, int Theirs)[]
        {
            ("content",  "عمق محتوا",        contentScore,            baseline.ContentBenchmark),
            ("semantic", "پوشش معنایی",      semanticScore,           baseline.SemanticBenchmark),
            ("intent",   "تطبیق Intent",     intentScore,             baseline.IntentBenchmark),
            ("entity",   "پوشش Entity",      (int)entityCoverage,     baseline.EntityBenchmark),
            ("aeo",      "AEO / پاسخ‌دهی",   aeoScore,                baseline.AeoBenchmark),
            ("product",  "داده محصول",       productScore,            baseline.ProductBenchmark),
            ("image",    "سئوی تصویر",       imageScore,              baseline.ImageBenchmark),
            ("schema",   "داده ساختاریافته", schemaScore,             baseline.SchemaBenchmark)
        };

        var gaps = rows.Select(r => new SeoDimensionGap
        {
            Dimension = r.Key,
            DimensionFa = r.Fa,
            OurScore = r.Ours,
            CompetitorAvg = r.Theirs
        }).ToList();

        // ۷۰ = برابری با میانگین SERP (موقعیت خوب ولی نه برنده).
        // برای رسیدن به ۸۵+ باید واضحاً از رقبا جلو بزنید، و صرفِ Auto-Fix
        // بدون برتری واقعی به «آماده رتبه» نمی‌رسد.
        var perDimension = rows.Select(r =>
            Math.Max(0, Math.Min(100, 70 + (r.Ours - r.Theirs) * 1.5)));

        var strength = (int)Math.Round(perDimension.Average());

        // اگر داده واقعی SERP نداریم، سقف ادعا را پایین می‌آوریم — صداقت مهم‌تر از نمره است
        if (!baseline.HasLiveData) strength = Math.Min(strength, 78);

        return (strength, gaps.OrderByDescending(g => g.Deficit).ToList());
    }

    /// <summary>
    /// نقطه‌ضعف رقبا — جایی که میانگین SERP پایین است و ما می‌توانیم واضحاً جلو بزنیم.
    /// </summary>
    public static List<string> FindCompetitorWeaknesses(SeoSerpBaseline b)
    {
        var weak = new List<string>();
        if (b.AeoBenchmark <= 75)
            weak.Add($"🟢 فرصت رقابتی: فقط {b.FaqSchemaShare * 100:F0}٪ رقبا FAQ Schema دارند — با AEO کامل جلو بزنید.");
        if (b.ContentBenchmark <= 80)
            weak.Add($"🟢 فرصت رقابتی: عمق محتوای رقبا حدود {b.AvgCompetitorWordCount} کلمه است — محتوای کامل‌تر برتری می‌دهد.");
        if (b.ImageBenchmark <= 78)
            weak.Add("🟢 فرصت رقابتی: سئوی تصویر رقبا ضعیف است — Scene + Studio + Alt توصیفی مزیت می‌سازد.");
        if (b.SchemaBenchmark <= 82)
            weak.Add("🟢 فرصت رقابتی: داده ساختاریافته رقبا ناقص است — Product + Offer + FAQ Schema کامل بگذارید.");
        return weak;
    }
}
