using System.Net;
using System.Text.RegularExpressions;
using MovasseghiShop.Web.Models;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public static class CompetitorPageFetcher
{
    static readonly Regex H1Rx = new(@"<h1[^>]*>(.*?)</h1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    static readonly Regex H2Rx = new(@"<h2[^>]*>(.*?)</h2>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    static readonly Regex H3Rx = new(@"<h3[^>]*>(.*?)</h3>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
    static readonly Regex MetaDescRx = new(@"<meta[^>]+name=[""']description[""'][^>]+content=[""']([^""']+)[""']", RegexOptions.IgnoreCase);
    static readonly Regex ScriptRx = new(@"<script[^>]*>.*?</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    static readonly Regex StyleRx = new(@"<style[^>]*>.*?</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    static readonly Regex TagRx = new("<[^>]+>", RegexOptions.Singleline);

    public static async Task EnrichAsync(
        CompetitorPageResearch page,
        HttpClient client,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(page.Url)) return;

        try
        {
            using var response = await client.GetAsync(page.Url, ct);
            if (!response.IsSuccessStatusCode) return;

            var html = await response.Content.ReadAsStringAsync(ct);
            if (html.Length > 250_000)
                html = html[..250_000];

            page.HasFaqSchema = html.Contains("FAQPage", StringComparison.OrdinalIgnoreCase)
                || html.Contains("@type\":\"FAQPage", StringComparison.OrdinalIgnoreCase);

            foreach (Match m in H1Rx.Matches(html))
                page.H1.Add(Strip(m.Groups[1].Value));
            foreach (Match m in H2Rx.Matches(html))
                page.H2.Add(Strip(m.Groups[1].Value));
            foreach (Match m in H3Rx.Matches(html))
                page.H3.Add(Strip(m.Groups[1].Value));

            var meta = MetaDescRx.Match(html);
            if (meta.Success)
                page.MetaDescription = WebUtility.HtmlDecode(meta.Groups[1].Value.Trim());

            var body = ScriptRx.Replace(html, " ");
            body = StyleRx.Replace(body, " ");
            body = TagRx.Replace(body, " ");
            body = WebUtility.HtmlDecode(body);
            page.WordCount = SeoText.CountWords(body);
        }
        catch
        {
            // SERP snippet alone is enough
        }
    }

    static string Strip(string html) =>
        WebUtility.HtmlDecode(TagRx.Replace(html, " ").Trim());
}
