using System.Text.RegularExpressions;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>
/// قوانین نمایش عمومی: متن دستی ادمین بر خلاصهٔ خودکار SEO/GEO اولویت دارد.
/// </summary>
public static class SeoStorefrontDisplay
{
    public const int SubstantialBodyMinChars = 80;

    public static bool HasSubstantialPlainText(string? text) =>
        !string.IsNullOrWhiteSpace(text) && text.Trim().Length >= SubstantialBodyMinChars;

    public static bool HasSubstantialHtmlBody(string? html) =>
        HasSubstantialPlainText(SeoText.StripHtml(html));

    public static string? GetGeoSnippetWhenNoManualLead(string? manualLead, SeoProfile? profile)
    {
        if (HasSubstantialPlainText(manualLead)) return null;
        return string.IsNullOrWhiteSpace(profile?.AiSummary) ? null : profile.AiSummary.Trim();
    }

    /// <summary>خلاصهٔ بالای صفحه دسته — فقط وقتی بدنهٔ HTML دسته هنوز کوتاه/خالی است.</summary>
    public static string? GetCategoryFeaturedSnippet(string? categoryDescriptionHtml, string? aiSummary)
    {
        if (HasSubstantialHtmlBody(categoryDescriptionHtml)) return null;
        return string.IsNullOrWhiteSpace(aiSummary) ? null : aiSummary.Trim();
    }

    /// <summary>زیرعنوان هیرو دسته — خلاصه GEO، ابتدای متن کوتاه، یا Meta Description.</summary>
    public static string? GetCategoryHeroLead(string? metaDescription, string? featuredSnippet, string? descriptionHtml)
    {
        if (!string.IsNullOrWhiteSpace(featuredSnippet)) return featuredSnippet.Trim();
        var plain = SeoText.StripHtml(descriptionHtml).Trim();
        if (plain.Length >= 40 && plain.Length <= 280) return plain;
        return string.IsNullOrWhiteSpace(metaDescription) ? null : metaDescription.Trim();
    }

    public static string ResolveProductLead(Product product)
    {
        if (HasSubstantialPlainText(product.ShortDescription))
            return product.ShortDescription!.Trim();
        return $"{product.Name} — ظرف یکبار مصرف گیاهی (نشاسته) اصل کارخانه آملون، مناسب پخش عمده رستوران، فست‌فود و کترینگ.";
    }

    /// <summary>خلاصهٔ بالای باکس خرید — از GEO / مقاله، نه توضیح کوتاه دستی.</summary>
    public static string ResolveProductBuyboxSummary(Product product, SeoProfile? profile)
    {
        string? candidate = null;
        if (HasSubstantialPlainText(profile?.AiSummary))
            candidate = profile!.AiSummary!.Trim();
        else
        {
            var fromArticle = ExtractArticleLead(product.Description);
            if (HasSubstantialPlainText(fromArticle))
                candidate = fromArticle;
            else
            {
                var focus = profile?.FocusKeyword?.Trim();
                if (!string.IsNullOrEmpty(focus))
                {
                    var secondary = profile?.SecondaryKeyword?.Trim() ?? SiteKeywordStrategy.Secondary;
                    candidate = SeoUltimateWriter.BuildAiSummary(product, focus, secondary);
                }
            }
        }

        if (!HasSubstantialPlainText(candidate))
            candidate = $"{product.Name} — ظرف گیاهی اصل آملون؛ مناسب خرید عمده رستوران و کترینگ از موثقی با ضمانت اصالت و ارسال سراسری.";

        return TruncateAtSentence(candidate!.Trim(), 420);
    }

    /// <summary>اولین پاراگراف معنادار از توضیحات تکمیلی (HTML).</summary>
    public static string? ExtractArticleLead(string? descriptionHtml, int maxChars = 420)
    {
        if (string.IsNullOrWhiteSpace(descriptionHtml)) return null;
        var plain = SeoText.StripHtml(descriptionHtml).Trim();
        plain = Regex.Replace(plain, @"\s+", " ");
        if (plain.Length < SubstantialBodyMinChars) return null;
        return TruncateAtSentence(plain, maxChars);
    }

    static string TruncateAtSentence(string text, int maxChars)
    {
        if (text.Length <= maxChars) return text;
        var slice = text[..maxChars];
        var lastStop = Math.Max(
            slice.LastIndexOf('۔'),
            Math.Max(slice.LastIndexOf('.'), Math.Max(slice.LastIndexOf('!'), slice.LastIndexOf('؟'))));
        if (lastStop > maxChars / 3)
            return slice[..(lastStop + 1)].Trim();
        var lastSpace = slice.LastIndexOf(' ');
        return (lastSpace > 0 ? slice[..lastSpace] : slice).Trim() + "…";
    }
}
