using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services.Editorial;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public static class SeoContentQualityGate
{
    public static async Task<SeoContentQualityGateResult> EvaluateAsync(
        ApplicationDbContext db,
        Product product,
        string html,
        SeoContentResearchBundle research,
        SeoContentPipelineConfig cfg,
        CancellationToken ct)
    {
        var plain = SeoText.StripHtml(html);
        var result = new SeoContentQualityGateResult
        {
            RelevanceScore = SeoContentRelevanceModule.ScoreProductRelevance(product, plain),
            HumanLikenessScore = SeoHumanizationFilter.Score(plain)
        };

        var competitorCorpus = research.Competitors
            .Where(c => c.Accepted)
            .Select(c => string.Join(' ', c.Title, c.MetaDescription ?? "", string.Join(' ', c.H2)))
            .Where(s => s.Length > 40)
            .ToList();

        var maxSim = 0.0;
        foreach (var chunk in competitorCorpus)
            maxSim = Math.Max(maxSim, SeoText.Similarity(plain, chunk));

        result.CompetitorSimilarityPercent = Math.Round(maxSim * 100, 1);

        var (siteDup, _) = await SeoSiteUniqueness.CheckAsync(
            db, "product", product.Id, plain, SeoSiteUniqueness.ProductSignature(product), ct);

        if (result.RelevanceScore < 50)
            result.Failures.Add($"ارتباط با محصول پایین ({result.RelevanceScore})");
        if (result.CompetitorSimilarityPercent > cfg.MaxCompetitorSimilarityPercent)
            result.Failures.Add($"شباهت با رقبا {result.CompetitorSimilarityPercent:F0}% (حد {cfg.MaxCompetitorSimilarityPercent:F0}%)");
        if (SeoSiteUniqueness.ShouldRewrite(siteDup))
            result.Failures.Add($"شباهت داخلی سایت {siteDup:F0}%");
        if (SeoText.CountWords(plain) < SeoContentRules.MinWordCount - 50)
            result.Failures.Add("طول متن کمتر از حد هدف");
        if (result.HumanLikenessScore < 55)
            result.Failures.Add("لحن مصنوعی / کلیشه AI");

        if (!plain.Contains(product.Name, StringComparison.OrdinalIgnoreCase))
            result.Failures.Add("نام محصول در متن نیست");

        result.Passed = result.Failures.Count == 0;
        return result;
    }

    public static async Task<SeoContentQualityGateResult> EvaluateEditorialAsync(
        ApplicationDbContext db,
        int blogId,
        string html,
        SeoContentResearchBundle research,
        SeoContentPipelineConfig cfg,
        CancellationToken ct)
    {
        var plain = SeoText.StripHtml(html);
        var result = new SeoContentQualityGateResult
        {
            RelevanceScore = 72,
            HumanLikenessScore = SeoHumanizationFilter.Score(plain)
        };

        var competitorCorpus = research.Competitors
            .Where(c => c.Accepted)
            .Select(c => string.Join(' ', c.Title, c.MetaDescription ?? "", string.Join(' ', c.H2)))
            .Where(s => s.Length > 40)
            .ToList();

        var maxSim = 0.0;
        foreach (var chunk in competitorCorpus)
            maxSim = Math.Max(maxSim, SeoText.Similarity(plain, chunk));
        result.CompetitorSimilarityPercent = Math.Round(maxSim * 100, 1);

        var blogMeta = await db.BlogPosts.AsNoTracking()
            .Where(b => b.Id == blogId)
            .Select(b => new { b.Title })
            .FirstOrDefaultAsync(ct);
        var focus = await db.SeoProfiles.AsNoTracking()
            .Where(p => p.EntityType == "blog" && p.EntityId == blogId)
            .Select(p => p.FocusKeyword)
            .FirstOrDefaultAsync(ct);
        var sig = blogMeta != null
            ? SeoSiteUniqueness.BlogSignature(
                new Models.Entities.BlogPost { Id = blogId, Title = blogMeta.Title }, focus)
            : plain[..Math.Min(80, plain.Length)];
        var (siteDup, _) = await SeoSiteUniqueness.CheckAsync(db, "blog", blogId, plain, sig, ct);

        if (result.CompetitorSimilarityPercent > cfg.MaxCompetitorSimilarityPercent)
            result.Failures.Add($"شباهت با رقبا {result.CompetitorSimilarityPercent:F0}%");
        if (SeoSiteUniqueness.ShouldRewrite(siteDup))
            result.Failures.Add($"شباهت داخلی سایت {siteDup:F0}%");
        if (SeoText.CountWords(plain) < SeoEditorialWriter.MinWordCount - 80)
            result.Failures.Add("طول متن کمتر از حد هدف مقاله");
        if (result.HumanLikenessScore < 50)
            result.Failures.Add("لحن مصنوعی / کلیشه AI");
        if (!plain.Contains("/Catalog", StringComparison.OrdinalIgnoreCase)
            && !plain.Contains("موثقی", StringComparison.OrdinalIgnoreCase))
            result.Failures.Add("سیگنال اعتماد/لینک داخلی ضعیف");

        result.Passed = result.Failures.Count == 0;
        return result;
    }
}
