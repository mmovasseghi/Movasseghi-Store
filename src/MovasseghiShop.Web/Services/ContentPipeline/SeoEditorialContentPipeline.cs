using System.Text;
using Microsoft.Extensions.Options;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services.Editorial;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public interface IEditorialContentPipelineService
{
    Task<EditorialPipelineResult> GenerateBlogProseAsync(
        string title,
        string focus,
        string secondary,
        string angle,
        int entityId,
        IReadOnlyList<ProductLink> products,
        CancellationToken ct = default);
}

public sealed class EditorialPipelineResult
{
    public string Html { get; init; } = "";
    public string? FaqJson { get; init; }
    public string? GeoSummary { get; init; }
    public bool UsedSerpResearch { get; init; }
    public SeoContentQualityGateResult Quality { get; init; } = new() { Passed = true };
    public ContentGapInsight? Gap { get; init; }
}

public class EditorialContentPipelineService(
    ApplicationDbContext db,
    ISerpOrganicSearchService serpSearch,
    IHttpClientFactory httpFactory,
    IOptions<SeoContentPipelineConfig> options,
    ILogger<EditorialContentPipelineService> log) : IEditorialContentPipelineService
{
    public async Task<EditorialPipelineResult> GenerateBlogProseAsync(
        string title,
        string focus,
        string secondary,
        string angle,
        int entityId,
        IReadOnlyList<ProductLink> products,
        CancellationToken ct = default)
    {
        var cfg = options.Value;
        var query = BuildSearchQuery(focus, title);
        var research = await serpSearch.SearchAsync(query, ct) ?? new SeoContentResearchBundle
        {
            SearchQuery = query,
            HasLiveSerp = false
        };

        if (cfg.FetchCompetitorHtml && research.HasLiveSerp)
        {
            var client = httpFactory.CreateClient("competitor-fetch");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MovasseghiShop-EditorialResearch/1.0");
            foreach (var page in research.Competitors.Take(cfg.MaxCompetitorPages))
                await CompetitorPageFetcher.EnrichAsync(page, client, ct);
        }

        var gap = SeoContentGapModule.AnalyzeEditorial(focus, title, research);
        var shortForm = SeoText.ShortForm(focus, title);

        EditorialPipelineResult? best = null;
        for (var attempt = 0; attempt < cfg.MaxGenerationRetries; attempt++)
        {
            var sb = new StringBuilder();
            sb.Append(SeoEditorialWriter.BuildContent(title, focus, secondary, angle, entityId, products, attempt));
            AppendSerpEnrichment(sb, focus, title, research, gap);

            var html = SeoHumanizationFilter.Clean(sb.ToString());
            html = SeoEditorialWriter.FinalizePublishHtml(html, focus, shortForm, title);
            html = EditorialParagraphDiversifier.ReduceInternalDuplication(
                html, focus, secondary, title, entityId + attempt * 19);

            var quality = await SeoContentQualityGate.EvaluateEditorialAsync(
                db, entityId, html, research, cfg, ct);

            var faqs = MergeEditorialFaqs(focus, title, secondary, research);
            var geo = SeoEditorialWriter.BuildGeoSummary(focus, secondary, title, products);

            var candidate = new EditorialPipelineResult
            {
                Html = html,
                FaqJson = SeoFaqStore.Serialize(faqs),
                GeoSummary = geo,
                UsedSerpResearch = research.HasLiveSerp,
                Quality = quality,
                Gap = gap
            };

            if (quality.Passed)
                return candidate;

            best = candidate;
            log.LogInformation(
                "Editorial quality gate attempt {Attempt} failed for blog {Id}: {Reasons}",
                attempt + 1, entityId, string.Join("; ", quality.Failures));
        }

        return best ?? new EditorialPipelineResult
        {
            Html = SeoEditorialWriter.BuildContent(title, focus, secondary, angle, entityId, products),
            FaqJson = SeoFaqStore.Serialize(SeoEditorialWriter.BuildFaqs(focus, title, secondary)),
            GeoSummary = SeoEditorialWriter.BuildGeoSummary(focus, secondary, title, products),
            Quality = new SeoContentQualityGateResult { Passed = false, Failures = ["تولید ناموفق"] }
        };
    }

    static string BuildSearchQuery(string focus, string title)
    {
        var q = SeoText.HeadPhrase(focus, title);
        if (!q.Contains("آملون", StringComparison.OrdinalIgnoreCase))
            q += " آملون موثقی";
        return q.Length > 80 ? focus : q;
    }

    static void AppendSerpEnrichment(
        StringBuilder sb,
        string focus,
        string title,
        SeoContentResearchBundle research,
        ContentGapInsight gap)
    {
        if (research.PeopleAlsoAsk.Count > 0)
        {
            sb.Append("<h2>سوالات واقعی جستجو (People Also Ask)</h2>");
            foreach (var q in research.PeopleAlsoAsk.Take(5))
            {
                sb.Append("<h3>").Append(E(q)).Append("</h3>");
                sb.Append("<p>پاسخ کوتاه: برای ").Append(E(SeoText.HeadPhrase(focus, title)))
                  .Append("، مدل و بسته‌بندی را از کاتالوگ موثقی انتخاب کنید و برای قیمت روز با کارشناس فروش تماس بگیرید.</p>");
            }
        }

        foreach (var h2 in gap.SuggestedH2.Take(3))
        {
            if (sb.ToString().Contains(h2, StringComparison.OrdinalIgnoreCase)) continue;
            sb.Append("<h2>").Append(E(h2)).Append("</h2>");
            sb.Append("<p>این بخش بر اساس الگوی صفحات برتر نتایج جستجو و نیاز خریدار عمده نوشته شده — ")
              .Append("تمرکز روی تصمیم عملیاتی، نه تعریف کلیشه‌ای.</p>");
        }

        if (gap.CommonCompetitorThemes.Count > 0)
        {
            sb.Append("<h2>موضوعات پرتکرار در نتایج رقبا</h2><ul>");
            foreach (var theme in gap.CommonCompetitorThemes.Take(6))
                sb.Append("<li>").Append(E(theme)).Append("</li>");
            sb.Append("</ul>");
        }
    }

    static List<SeoFaq> MergeEditorialFaqs(
        string focus, string title, string secondary, SeoContentResearchBundle research)
    {
        var faqs = SeoEditorialWriter.BuildFaqs(focus, title, secondary);
        foreach (var q in research.PeopleAlsoAsk.Take(4))
        {
            if (faqs.Any(f => SeoText.Similarity(f.Question, q) > 0.65)) continue;
            var question = q.EndsWith('؟') ? q : q + "؟";
            faqs.Add(new SeoFaq
            {
                Question = question,
                ShortAnswer = "برای خرید عمده از موثقی، مدل مناسب را از کاتالوگ انتخاب کنید و استعلام قیمت بگیرید.",
                Answer = $"در خصوص «{q}»، پاسخ به مدل {SeoText.HeadPhrase(focus, title)} و حجم سفارش شما بستگی دارد. "
                         + "کارشناسان موثقی بدون تعهد، سبد خرید بهینه پیشنهاد می‌دهند."
            });
        }

        return faqs;
    }

    static string E(string v) => v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
