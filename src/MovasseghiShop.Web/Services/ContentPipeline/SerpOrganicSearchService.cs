using System.Text.Json;
using MovasseghiShop.Web.Models;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public interface ISerpOrganicSearchService
{
    bool IsConfigured { get; }
    Task<SeoContentResearchBundle?> SearchAsync(string query, CancellationToken ct = default);
}

public class SerpOrganicSearchService(
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<SerpOrganicSearchService> log) : ISerpOrganicSearchService
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(config["SerpApi:ApiKey"]);

    public async Task<SeoContentResearchBundle?> SearchAsync(string query, CancellationToken ct = default)
    {
        var apiKey = config["SerpApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(query))
            return null;

        try
        {
            var client = httpFactory.CreateClient("serpapi");
            var url =
                $"https://serpapi.com/search.json?engine=google&q={Uri.EscapeDataString(query)}&google_domain=google.com&gl=ir&hl=fa&num=20&api_key={apiKey}";
            var json = await client.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);
            var bundle = new SeoContentResearchBundle
            {
                SearchQuery = query,
                HasLiveSerp = true
            };

            if (doc.RootElement.TryGetProperty("organic_results", out var organic))
            {
                foreach (var item in organic.EnumerateArray())
                {
                    var pos = item.TryGetProperty("position", out var pEl) ? pEl.GetInt32() : 0;
                    var title = item.TryGetProperty("title", out var tEl) ? tEl.GetString() ?? "" : "";
                    var link = item.TryGetProperty("link", out var lEl) ? lEl.GetString() ?? "" : "";
                    var snippet = item.TryGetProperty("snippet", out var sEl) ? sEl.GetString() ?? "" : "";
                    var domain = "";
                    if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
                        domain = uri.Host;

                    bundle.Competitors.Add(new CompetitorPageResearch
                    {
                        SerpPosition = pos,
                        Title = title,
                        Url = link,
                        Domain = domain,
                        MetaDescription = snippet,
                        WordCount = SeoText.CountWords(snippet)
                    });
                }
            }

            ExtractQuestions(doc.RootElement, "related_questions", bundle.PeopleAlsoAsk);
            ExtractQuestions(doc.RootElement, "people_also_ask", bundle.PeopleAlsoAsk);

            if (doc.RootElement.TryGetProperty("related_searches", out var related))
            {
                foreach (var r in related.EnumerateArray())
                {
                    if (r.TryGetProperty("query", out var q))
                    {
                        var text = q.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                            bundle.RelatedSearches.Add(text!);
                    }
                }
            }

            return bundle;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Serp organic search failed for {Query}", query);
            return null;
        }
    }

    static void ExtractQuestions(JsonElement root, string property, List<string> target)
    {
        if (!root.TryGetProperty(property, out var arr)) return;
        foreach (var item in arr.EnumerateArray())
        {
            if (item.TryGetProperty("question", out var q))
            {
                var text = q.GetString();
                if (!string.IsNullOrWhiteSpace(text) && !target.Contains(text))
                    target.Add(text!);
            }
            else if (item.TryGetProperty("title", out var t))
            {
                var text = t.GetString();
                if (!string.IsNullOrWhiteSpace(text) && !target.Contains(text))
                    target.Add(text!);
            }
        }
    }
}
