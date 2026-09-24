namespace MovasseghiShop.Web.Services;

public record SerpOrganicResult(int Position, string Title, string Url, string Domain);
public record SerpRankResult(int Position, string? MatchedUrl, List<SerpOrganicResult> TopResults);

public interface ISerpRankProvider
{
    bool IsConfigured { get; }
    Task<SerpRankResult?> GetRankAsync(string keyword, string targetDomain, CancellationToken ct = default);
}

public class SerpApiRankProvider(IHttpClientFactory httpFactory, IConfiguration config, ILogger<SerpApiRankProvider> log) : ISerpRankProvider
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(config["SerpApi:ApiKey"]);

    public async Task<SerpRankResult?> GetRankAsync(string keyword, string targetDomain, CancellationToken ct = default)
    {
        var apiKey = config["SerpApi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        try
        {
            var client = httpFactory.CreateClient("serpapi");
            var url = $"https://serpapi.com/search.json?engine=google&q={Uri.EscapeDataString(keyword)}&google_domain=google.com&gl=ir&hl=fa&num=20&api_key={apiKey}";
            var json = await client.GetStringAsync(url, ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("organic_results", out var organic)) return null;

            var top = new List<SerpOrganicResult>();
            var position = 0;
            string? matchedUrl = null;

            foreach (var item in organic.EnumerateArray())
            {
                var pos = item.TryGetProperty("position", out var pEl) ? pEl.GetInt32() : top.Count + 1;
                var title = item.TryGetProperty("title", out var tEl) ? tEl.GetString() ?? "" : "";
                var link = item.TryGetProperty("link", out var lEl) ? lEl.GetString() ?? "" : "";
                var domain = item.TryGetProperty("displayed_link", out var dEl) ? dEl.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(domain) && Uri.TryCreate(link, UriKind.Absolute, out var uri))
                    domain = uri.Host;

                top.Add(new SerpOrganicResult(pos, title, link, domain));

                if (matchedUrl == null && link.Contains(targetDomain, StringComparison.OrdinalIgnoreCase))
                {
                    position = pos;
                    matchedUrl = link;
                }
            }

            return new SerpRankResult(position, matchedUrl, top);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "SerpAPI failed for keyword {Keyword}", keyword);
            return null;
        }
    }
}

public class NullSerpRankProvider : ISerpRankProvider
{
    public bool IsConfigured => false;
    public Task<SerpRankResult?> GetRankAsync(string keyword, string targetDomain, CancellationToken ct = default) =>
        Task.FromResult<SerpRankResult?>(null);
}
