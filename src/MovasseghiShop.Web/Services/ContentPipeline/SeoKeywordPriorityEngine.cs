using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public static class SeoKeywordPriorityEngine
{
    public static List<KeywordMappingEntry> PlanForProduct(Product product, string productFocus)
    {
        var name = product.Name ?? "";
        var type = product.ProductType ?? "";
        var entries = new List<KeywordMappingEntry>();

        foreach (var pillar in SiteKeywordStrategy.Pillars.OrderBy(p => p.Priority))
        {
            var relevant = IsRelevant(pillar, name, type, productFocus);
            entries.Add(new KeywordMappingEntry
            {
                Priority = pillar.Priority,
                Phrase = pillar.Phrase,
                UsedInContent = false,
                Placement = relevant ? "candidate" : "skip"
            });
        }

        return entries;
    }

    public static void ApplyToHtml(ref string html, List<KeywordMappingEntry> map, Product product, string focus)
    {
        var candidates = map
            .Where(m => m.Placement == "candidate")
            .OrderBy(m => m.Priority)
            .Take(4)
            .ToList();

        if (candidates.Count == 0) return;

        var sb = new System.Text.StringBuilder();
        sb.Append("<h2>چرا خریداران عمده به موثقی اعتماد می‌کنند؟</h2><p>");
        var first = true;
        foreach (var c in candidates)
        {
            if (!first) sb.Append(' ');
            sb.Append("برای ");
            sb.Append(System.Net.WebUtility.HtmlEncode(c.Phrase));
            sb.Append("، ");
            c.UsedInContent = true;
            c.Placement = "body";
            first = false;
        }

        var shortName = SeoText.ShortForm(focus, product.Name);
        sb.Append("مدل <strong>").Append(System.Net.WebUtility.HtmlEncode(shortName)).Append("</strong>");
        if (!string.IsNullOrWhiteSpace(product.ProductCode))
            sb.Append(" (کد ").Append(System.Net.WebUtility.HtmlEncode(product.ProductCode)).Append(')');
        sb.Append(" با تأمین مستقیم از نمایندگی رسمی آملون و پشتیبانی فنی انتخاب سایز ارائه می‌شود.</p>");

        var block = sb.ToString();
        var idx = html.IndexOf("<h2", StringComparison.OrdinalIgnoreCase);
        html = idx >= 0 ? html.Insert(idx, block) : block + html;
    }

    static bool IsRelevant(PillarKeywordDef pillar, string name, string type, string focus)
    {
        if (pillar.Priority <= 3) return true;
        if (pillar.Cluster is "b2b" or "wholesale" or "pricing") return true;
        if (name.Contains(pillar.Phrase, StringComparison.OrdinalIgnoreCase)
            || type.Contains(pillar.Phrase, StringComparison.OrdinalIgnoreCase)
            || focus.Contains(pillar.Phrase, StringComparison.OrdinalIgnoreCase))
            return true;

        if (pillar.Phrase.Contains("کاسه", StringComparison.Ordinal) && name.Contains("کاسه", StringComparison.Ordinal))
            return true;
        if (pillar.Phrase.Contains("لیوان", StringComparison.Ordinal) && name.Contains("لیوان", StringComparison.Ordinal))
            return true;
        if (pillar.Phrase.Contains("مایکرو", StringComparison.Ordinal) && name.Contains("مایکرو", StringComparison.Ordinal))
            return true;
        if (pillar.Phrase.Contains("فست", StringComparison.Ordinal) && name.Contains("فست", StringComparison.Ordinal))
            return true;
        if (pillar.Phrase.Contains("کترینگ", StringComparison.Ordinal) && name.Contains("کترینگ", StringComparison.Ordinal))
            return true;

        return pillar.Priority <= 10;
    }
}
