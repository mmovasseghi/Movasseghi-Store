using System.Text;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>تولید محتوای SEO برای بلاگ و اخبار — یکتا و مبتنی بر عنوان + GSC.</summary>
public static class SeoArticleBuilder
{
    public const int MinWordCount = 650;

    public static string BuildMetaTitle(string focus, string title) =>
        SeoUltimateWriter.BuildMetaTitle(focus, title);

    public static string BuildMetaDescription(string focus, string secondary, string title, string excerpt) =>
        Fit(
            string.IsNullOrWhiteSpace(excerpt)
                ? $"{SeoText.HeadPhrase(focus, title)} — {secondary}. راهنمای عملی برای خریداران عمده ظروف گیاهی آملون از فروشگاه موثقی."
                : excerpt.Trim(),
            SeoContentRules.MetaDescMin,
            SeoContentRules.MetaDescMax);

    public static string BuildExcerpt(string focus, string title, int entityId) =>
        $"{OpeningVariant(entityId)} {SeoText.HeadPhrase(focus, title)} در این مطلب با زبان ساده بررسی می‌شود — " +
        $"مناسب کسب‌وکارهایی که به {SiteKeywordStrategy.Secondary} نیاز دارند.";

    public static string BuildContent(string title, string focus, string secondary, int entityId, string kind, int differentiationAttempt = 0)
    {
        var sb = new StringBuilder();
        var seed = entityId + kind.Length + differentiationAttempt * 13;
        var lead = OpeningVariant(seed);
        sb.Append("<h2>").Append(E($"راهنمای {title}")).Append("</h2>");
        sb.Append("<p><strong>").Append(E(title)).Append("</strong> — ")
            .Append(E(lead)).Append(' ')
            .Append(E($"موضوع «{SeoText.HeadPhrase(focus, title)}» برای خریداران عمده موثقی است"))
            .Append(E($" (شناسه محتوا {entityId})."))
            .Append("</p>");

        sb.Append("<h2>").Append(E($"چرا {focus} مهم است؟")).Append("</h2><p>")
            .Append(E($"برای رستوران، کیترینگ و برندهای فست‌فود، انتخاب {secondary} مستقیم روی هزینه، سرعت سرویس و تصویر برند اثر می‌گذارد. "))
            .Append(E($"در فروشگاه موثقی، مدل‌های {SiteKeywordStrategy.Tertiary} با ضمانت اصالت عرضه می‌شوند."))
            .Append("</p>");

        sb.Append("<h2>").Append(E("نکات عملی قبل از سفارش")).Append("</h2><ol>")
            .Append("<li>").Append(E("حجم ماهانه و نوع غذا را مشخص کنید تا ظرفیت و ابعاد درست انتخاب شود.")).Append("</li>")
            .Append("<li>").Append(E("شرایط نگهداری و سازگاری با مایکروویو را از برچسب محصول بخوانید.")).Append("</li>")
            .Append("<li>").Append(E("برای استعلام قیمت عمده از /Catalog یا فرم تماس استفاده کنید.")).Append("</li>")
            .Append("</ol>");

        sb.Append("<h2>").Append(E("سوالات متداول")).Append("</h2>");
        sb.Append("<h3>").Append(E($"آیا {focus} برای مایکروویو مناسب است؟")).Append("</h3><p>")
            .Append(E("بسته به مدل و جنس نشاسته گیاهی متفاوت است؛ همیشه مشخصات فنی همان SKU را چک کنید."))
            .Append("</p>");
        sb.Append("<h3>").Append(E("حداقل سفارش عمده چقدر است؟")).Append("</h3><p>")
            .Append(E("شرایط خرید عمده در صفحه عمده‌فروشی و استعلام قیمت توضیح داده شده است."))
            .Append("</p>");

        sb.Append("<h2>").Append(E("جمع‌بندی")).Append("</h2><p>")
            .Append(E($"اگر به {focus} نیاز دارید، از کاتالوگ محصولات دیدن کنید یا با تیم موثقی تماس بگیرید — مشاوره رایگان است."))
            .Append("</p>");

        var html = sb.ToString();
        return SeoKeywordPlacer.Resolve(html, focus, secondary);
    }

    static string OpeningVariant(int seed) =>
        (seed % 5) switch
        {
            0 => "در بازار B2B ظروف یکبارمصرف،",
            1 => "برای تصمیم خرید هوشمند،",
            2 => "اگر زمان کمی دارید،",
            3 => "در تجربه مشتریان عمده موثقی،",
            _ => "از منظر عملیاتی کترینگ و رستوران،"
        };

    static string Fit(string text, int min, int max)
    {
        if (text.Length > max) return text[..(max - 1)].TrimEnd() + "…";
        while (text.Length < min && text.Length < max - 20)
            text += " مشاوره رایگان خرید عمده.";
        return text.Length <= max ? text : text[..max];
    }

    static string E(string v) => v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
