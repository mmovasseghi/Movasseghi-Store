using System.Text;
using System.Text.RegularExpressions;

namespace MovasseghiShop.Web.Services.Editorial;

/// <summary>کاهش شباهت پاراگراف‌های تکراری درون یک مقاله — بدون دست‌کاری امتیاز.</summary>
public static partial class EditorialParagraphDiversifier
{
    public static string ReduceInternalDuplication(string html, string focus, string secondary, string title, int seed)
    {
        var current = html;
        for (var pass = 0; pass < 6; pass++)
        {
            var paragraphs = ExtractParagraphs(current);
            if (paragraphs.Count < 2) break;

            var changed = false;
            for (var i = 0; i < paragraphs.Count; i++)
            {
                for (var j = i + 1; j < paragraphs.Count; j++)
                {
                    if (SeoTemplateTokens.DistinctiveSimilarity(
                            SeoText.StripHtml(paragraphs[i]), SeoText.StripHtml(paragraphs[j])) < 0.74)
                        continue;

                    paragraphs[j] = SeoEditorialWriter.BuildUniqueExpansionSnippet(
                        focus, secondary, title, seed + pass * 41 + i * 11 + j);
                    changed = true;
                }
            }

            if (!changed) break;
            current = RebuildHtml(current, paragraphs);

            var risk = SeoContentAnalyzer.Analyze(current, focus, secondary).DuplicationRisk;
            if (risk <= SeoContentRules.MaxDuplicationRisk) break;
        }

        return current;
    }

    static List<string> ExtractParagraphs(string html)
    {
        var list = new List<string>();
        foreach (Match m in ParagraphRegex().Matches(html))
        {
            var inner = m.Groups[1].Value.Trim();
            if (inner.Length > 0)
                list.Add(inner);
        }
        return list;
    }

    static string RebuildHtml(string original, List<string> paragraphInners)
    {
        var idx = 0;
        return ParagraphRegex().Replace(original, match =>
        {
            if (idx >= paragraphInners.Count) return match.Value;
            var inner = paragraphInners[idx++];
            return $"<p>{inner}</p>";
        });
    }

    [GeneratedRegex(@"<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ParagraphRegex();
}
