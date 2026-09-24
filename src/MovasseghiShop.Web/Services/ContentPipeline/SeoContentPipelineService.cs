using Microsoft.Extensions.Options;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public interface ISeoContentPipelineService
{
    Task<SeoContentPipelineResult> GenerateProductProseAsync(
        Product product,
        SeoProfile profile,
        string focus,
        string secondary,
        CancellationToken ct = default);
}

public class SeoContentPipelineService(
    ApplicationDbContext db,
    ISerpOrganicSearchService serpSearch,
    IHttpClientFactory httpFactory,
    IOptions<SeoContentPipelineConfig> options,
    ILogger<SeoContentPipelineService> log) : ISeoContentPipelineService
{
    public async Task<SeoContentPipelineResult> GenerateProductProseAsync(
        Product product,
        SeoProfile profile,
        string focus,
        string secondary,
        CancellationToken ct = default)
    {
        var cfg = options.Value;
        var query = BuildSearchQuery(product, focus);
        var research = await serpSearch.SearchAsync(query, ct) ?? new SeoContentResearchBundle
        {
            SearchQuery = query,
            HasLiveSerp = false
        };

        if (cfg.FetchCompetitorHtml && research.HasLiveSerp)
        {
            var client = httpFactory.CreateClient("competitor-fetch");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MovasseghiShop-SeoResearch/1.0");
            var top = research.Competitors.Take(cfg.MaxCompetitorPages).ToList();
            foreach (var page in top)
                await CompetitorPageFetcher.EnrichAsync(page, client, ct);
        }

        SeoContentRelevanceModule.ScoreAndFilter(product, research, cfg);
        var gap = SeoContentGapModule.Analyze(product, research);
        var keywordMap = SeoKeywordPriorityEngine.PlanForProduct(product, focus);

        SeoContentPipelineResult? best = null;
        for (var attempt = 0; attempt < cfg.MaxGenerationRetries; attempt++)
        {
            var html = SeoResearchAwareWriter.Build(
                product, focus, secondary, product.Category?.Slug, gap, research, attempt);

            SeoKeywordPriorityEngine.ApplyToHtml(ref html, keywordMap, product, focus);
            html = SeoKeywordPlacer.EnforceDensityCap(
                SeoKeywordPlacer.Resolve(html, focus, SeoText.ShortForm(focus, product.Name)),
                focus,
                SeoText.ShortForm(focus, product.Name));

            var quality = await SeoContentQualityGate.EvaluateAsync(
                db, product, html, research, cfg, ct);

            var candidate = new SeoContentPipelineResult
            {
                Html = html,
                FaqJson = SeoResearchAwareWriter.MergeFaqs(product, focus, secondary, research),
                Research = research,
                Gap = gap,
                KeywordMap = keywordMap,
                Quality = quality,
                Attempts = attempt + 1,
                UsedSerpResearch = research.HasLiveSerp
            };

            if (quality.Passed)
                return candidate;

            best = candidate;
            log.LogInformation(
                "Content quality gate attempt {Attempt} failed for product {Id}: {Reasons}",
                attempt + 1, product.Id, string.Join("; ", quality.Failures));
        }

        return best ?? new SeoContentPipelineResult
        {
            Research = research,
            Gap = gap,
            KeywordMap = keywordMap,
            Quality = new SeoContentQualityGateResult { Passed = false, Failures = ["تولید ناموفق"] }
        };
    }

    static string BuildSearchQuery(Product product, string focus)
    {
        var name = product.Name?.Trim() ?? focus;
        if (!name.Contains("آملون", StringComparison.OrdinalIgnoreCase))
            name += " آملون";
        return name.Length > 70 ? focus : name;
    }
}
