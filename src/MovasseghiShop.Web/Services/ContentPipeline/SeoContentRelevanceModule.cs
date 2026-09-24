using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public static class SeoContentRelevanceModule
{
    public static void ScoreAndFilter(Product product, SeoContentResearchBundle bundle, SeoContentPipelineConfig cfg)
    {
        var nameTokens = Tokenize(product.Name);
        var sigTokens = BuildSignatureTokens(product);

        foreach (var page in bundle.Competitors)
        {
            if (IsOurDomain(page.Domain, cfg.OurDomain))
            {
                page.Accepted = false;
                page.RejectReason = "دامنه خودمان";
                continue;
            }

            var haystack = string.Join(' ',
                page.Title, page.MetaDescription ?? "",
                string.Join(' ', page.H1), string.Join(' ', page.H2));

            var score = Score(haystack, nameTokens, sigTokens);
            page.RelevanceScore = score;

            var hasAmolon = haystack.Contains("آملون", StringComparison.OrdinalIgnoreCase)
                || haystack.Contains("amelon", StringComparison.OrdinalIgnoreCase);
            var nameOverlap = nameTokens.Count(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase));

            if (score < cfg.MinRelevanceScore)
            {
                page.Accepted = false;
                page.RejectReason = $"امتیاز ارتباط {score} < {cfg.MinRelevanceScore}";
            }
            else if (nameOverlap < 2 && !hasAmolon && nameTokens.Count >= 2)
            {
                page.Accepted = false;
                page.RejectReason = "نام محصول/برند آملون در صفحه دیده نشد";
            }
            else
            {
                page.Accepted = true;
            }
        }

        bundle.Competitors = bundle.Competitors
            .OrderByDescending(c => c.Accepted)
            .ThenByDescending(c => c.RelevanceScore)
            .ThenBy(c => c.SerpPosition)
            .ToList();
    }

    public static int ScoreProductRelevance(Product product, string plainContent)
    {
        var haystack = plainContent;
        return Score(haystack, Tokenize(product.Name), BuildSignatureTokens(product));
    }

    static int Score(string haystack, List<string> nameTokens, HashSet<string> sigTokens)
    {
        if (string.IsNullOrWhiteSpace(haystack)) return 0;
        var pts = 0;
        pts += Math.Min(40, nameTokens.Count(t => t.Length > 2 && haystack.Contains(t, StringComparison.OrdinalIgnoreCase)) * 12);
        pts += sigTokens.Count(t => haystack.Contains(t, StringComparison.OrdinalIgnoreCase)) * 8;
        if (haystack.Contains("آملون", StringComparison.OrdinalIgnoreCase)) pts += 15;
        if (haystack.Contains("گیاهی", StringComparison.OrdinalIgnoreCase)
            || haystack.Contains("نشاسته", StringComparison.OrdinalIgnoreCase)) pts += 10;
        return Math.Min(100, pts);
    }

    static HashSet<string> BuildSignatureTokens(Product p)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? s)
        {
            foreach (var t in Tokenize(s))
                set.Add(t);
        }

        Add(p.ProductType);
        Add(p.Material);
        Add(p.Applications);
        Add(p.Dimensions);
        if (p.CapacityCc is > 0) set.Add($"{p.CapacityCc}");
        if (p.CompartmentCount is > 0) set.Add($"{p.CompartmentCount}");
        if (!string.IsNullOrWhiteSpace(p.ProductCode)) set.Add(p.ProductCode.Trim());
        return set;
    }

    static List<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];
        return text.Split([' ', '،', ',', '·', '-', '—', '–', '/', '|', '\n'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 1 && !int.TryParse(t, out _))
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();
    }

    static bool IsOurDomain(string domain, string our) =>
        !string.IsNullOrWhiteSpace(domain)
        && domain.Contains(our, StringComparison.OrdinalIgnoreCase);
}
