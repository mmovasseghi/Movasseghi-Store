using MovasseghiShop.Web.Services;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class SeoKeywordDensityTests
{
    [Fact]
    public void EnforceDensityCap_trims_literal_repetitions()
    {
        const string focus = "ظرف چهارخانه کبابی فانتزی یکبار مصرف گیاهی";
        const string shortLabel = "ظرف چهارخانه کبابی فانتزی";
        var padded = string.Join(" ", Enumerable.Repeat(focus, 12));
        var filler = string.Join(" ", Enumerable.Repeat(
            "متن توضیحی تکمیلی برای حجم و تنوع واژگان در صفحه محصول فروشگاه عمده و خرید سازمانی.", 90));
        var html = $"<p>{padded}</p><p>{filler}</p>";

        var result = SeoKeywordPlacer.EnforceDensityCap(html, focus, shortLabel);
        var plain = SeoText.StripHtml(result);
        var words = SeoText.CountWords(plain);
        var occ = SeoText.CountPhrase(plain, focus);
        var density = occ * SeoText.CountWords(focus) * 100.0 / words;

        Assert.True(density <= SeoContentRules.KeywordDensityStuffing);
    }
}
