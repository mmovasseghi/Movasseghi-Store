using System.Net;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>انتخاب کلیدواژه از داده واقعی (Search Console) — نه ساخت دستی بی‌ربط.</summary>
public static class SeoKeywordResearch
{
    public static (string Primary, string Secondary) ResolveProductKeywords(
        Product product,
        IReadOnlyList<GscQueryRow> gscQueries)
    {
        var secondary = SiteKeywordStrategy.SuggestProductSecondary(product);
        var fallback = SiteKeywordStrategy.SuggestProductPrimary(product);

        if (gscQueries.Count == 0)
            return (fallback, secondary);

        var name = WebUtility.HtmlDecode(product.Name ?? "").Trim();
        var tokens = Tokenize(name);

        var best = gscQueries
            .Where(q => !string.IsNullOrWhiteSpace(q.Query))
            .Where(q => q.Query.Length is >= 6 and <= 55)
            .Where(q => q.Impressions >= 3 || (q.Position is > 0 and <= 20))
            .Select(q => new
            {
                q.Query,
                Weight = q.Impressions + (q.Clicks * 4) + (q.Position is > 0 and <= 15 ? 12 : 0),
                Match = TokenOverlapScore(tokens, Tokenize(q.Query))
            })
            .Where(x => x.Match >= 0.35)
            .OrderByDescending(x => x.Weight * (0.5 + x.Match))
            .FirstOrDefault();

        if (best is not null)
        {
            var gscPrimary = best.Query.Trim();
            var nameTokens = Tokenize(name);
            var gscTokens = Tokenize(gscPrimary);
            var overlap = nameTokens.Count(t => gscTokens.Contains(t));
            if (overlap >= 2 || gscPrimary.Contains(name, StringComparison.OrdinalIgnoreCase))
                return (gscPrimary, secondary);
        }

        return (fallback, secondary);
    }

    public static (string Primary, string Secondary) ResolveEditorialKeywords(
        string title,
        IReadOnlyList<GscQueryRow> gscQueries)
    {
        var secondary = SiteKeywordStrategy.Secondary;
        var fallback = BuildTitleKeyword(title);

        if (gscQueries.Count == 0)
            return (fallback, secondary);

        var tokens = Tokenize(title);
        var best = gscQueries
            .Where(q => q.Query.Length is >= 6 and <= 55)
            .Select(q => new { q.Query, q.Impressions, Match = TokenOverlapScore(tokens, Tokenize(q.Query)) })
            .Where(x => x.Match >= 0.4)
            .OrderByDescending(x => x.Impressions * (0.4 + x.Match))
            .FirstOrDefault();

        return best is not null ? (best.Query.Trim(), secondary) : (fallback, secondary);
    }

    static string BuildTitleKeyword(string title)
    {
        var t = WebUtility.HtmlDecode(title ?? "").Trim();
        if (t.Length == 0) return SiteKeywordStrategy.Primary;
        var head = t.Split(['—', '–', '|', '،', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? t;
        if (!head.Contains("گیاهی", StringComparison.OrdinalIgnoreCase) && head.Length < 48)
            head = $"{head} — {SiteKeywordStrategy.Primary}";
        return head.Length <= 55 ? head : head[..55].Trim();
    }

    static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return text.Split([' ', '،', ',', '·', '-', '—', '–', '/', '|', '\n', '\r', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length >= 2)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    static double TokenOverlapScore(List<string> a, List<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;
        var hit = a.Count(t => b.Any(u => u.Contains(t, StringComparison.Ordinal) || t.Contains(u, StringComparison.Ordinal)));
        return hit / (double)Math.Max(a.Count, b.Count);
    }
}
