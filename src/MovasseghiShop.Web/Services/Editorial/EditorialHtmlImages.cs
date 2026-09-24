using System.Text.RegularExpressions;

namespace MovasseghiShop.Web.Services.Editorial;

public sealed record EditorialInlineImage(int Index, string Url, string Alt);

/// <summary>استخراج تصاویر درج‌شده در HTML مقاله برای پنل ویرایش ادمین.</summary>
public static partial class EditorialHtmlImages
{
    public static IReadOnlyList<EditorialInlineImage> Parse(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return [];

        var list = new List<EditorialInlineImage>();
        var i = 0;
        foreach (Match m in ImgTagRegex().Matches(html))
        {
            var url = m.Groups["src"].Value.Trim();
            if (string.IsNullOrWhiteSpace(url)) continue;
            var alt = m.Groups["alt"].Value.Trim();
            list.Add(new EditorialInlineImage(i++, url, alt));
        }

        return list;
    }

    public static string ReplaceImageUrl(string html, string oldUrl, string newUrl, string? newAlt = null)
    {
        if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(oldUrl)) return html ?? "";
        var escapedOld = Regex.Escape(oldUrl);
        var pattern = $@"<img([^>]*)\ssrc=[""']{escapedOld}[""']([^>]*)>";
        return Regex.Replace(html, pattern, m =>
        {
            var tag = m.Value;
            tag = Regex.Replace(tag, @"\ssrc=[""'][^""']+[""']", $" src=\"{newUrl}\"", RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(newAlt))
                tag = AltAttrRegex().IsMatch(tag)
                    ? AltAttrRegex().Replace(tag, $" alt=\"{EscapeAttr(newAlt)}\"")
                    : tag.Replace("<img", $"<img alt=\"{EscapeAttr(newAlt)}\"", StringComparison.OrdinalIgnoreCase);
            return tag;
        }, RegexOptions.IgnoreCase);
    }

    static string EscapeAttr(string v) => v.Replace("\"", "&quot;", StringComparison.Ordinal);

    [GeneratedRegex(@"<img\s+[^>]*src=[""'](?<src>[^""']+)[""'][^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ImgTagRegex();

    [GeneratedRegex(@"\salt=[""'][^""']*[""']", RegexOptions.IgnoreCase)]
    private static partial Regex AltAttrRegex();
}
