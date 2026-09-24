using System.Text;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public static class SeoResearchAwareWriter
{
    public static string Build(
        Product product,
        string focus,
        string secondary,
        string? categorySlug,
        ContentGapInsight gap,
        SeoContentResearchBundle research,
        int differentiationAttempt)
    {
        var options = new SeoContentBuildOptions
        {
            Mode = SeoContentMode.Unique,
            DifferentiationAttempt = differentiationAttempt + 1,
            Intent = SeoIntentDetector.Detect(focus, secondary),
            MissingTopics = gap.UniqueAnglesForUs.Take(3).ToList()
        };

        var core = SeoUltimateWriter.BuildContent(product, focus, secondary, categorySlug, options);
        var sb = new StringBuilder();

        sb.Append("<section class=\"product-tldr\">");
        sb.Append("<h2>خلاصه سریع</h2><ul>");
        sb.Append("<li><strong>محصول:</strong> ").Append(E(product.Name));
        if (!string.IsNullOrWhiteSpace(product.ProductCode))
            sb.Append(" — کد ").Append(E(product.ProductCode));
        sb.Append("</li>");
        sb.Append("<li><strong>جنس:</strong> ").Append(E(product.Material ?? "مواد گیاهی (نشاسته)")).Append("</li>");
        var pack = DescribePackaging(product);
        if (!string.IsNullOrWhiteSpace(pack))
            sb.Append("<li><strong>بسته‌بندی:</strong> ").Append(E(pack)).Append("</li>");
        sb.Append("<li><strong>تأمین:</strong> خرید عمده از موثقی، نمایندگی رسمی آملون در تهران — حداقل سفارش ۱۰ میلیون تومان</li>");
        sb.Append("</ul></section>");

        if (research.PeopleAlsoAsk.Count > 0)
        {
            sb.Append("<h2>سوالات پرتکرار خریداران عمده</h2><ul>");
            foreach (var q in research.PeopleAlsoAsk.Take(4))
                sb.Append("<li>").Append(E(q)).Append("</li>");
            sb.Append("</ul>");
        }

        foreach (var h2 in gap.SuggestedH2.Take(2))
        {
            sb.Append("<h2>").Append(E(h2)).Append("</h2>");
            sb.Append("<p>برای <strong>").Append(E(product.Name)).Append("</strong>، ");
            sb.Append("این بخش بر اساس نیاز واقعی تأمین در رستوران و کیترینگ نوشته شده — ");
            sb.Append("نه متن عمومی کاتالوگ. ");
            if (!string.IsNullOrWhiteSpace(product.Applications))
                sb.Append("کاربرد اصلی: ").Append(E(product.Applications)).Append(".</p>");
            else
                sb.Append("جزئیات را با کارشناس فروش موثقی هماهنگ کنید.</p>");
        }

        sb.Append(core);
        return SeoHumanizationFilter.Clean(sb.ToString());
    }

    public static string MergeFaqs(
        Product product,
        string focus,
        string secondary,
        SeoContentResearchBundle research)
    {
        var faqs = SeoUltimateWriter.BuildFaqs(product, focus, secondary, aeoMode: true);
        foreach (var q in research.PeopleAlsoAsk.Take(3))
        {
            if (faqs.Any(f => SeoText.Similarity(f.Question, q) > 0.7)) continue;
            faqs.Add(new SeoFaq
            {
                Question = q.EndsWith('؟') ? q : q + "؟",
                ShortAnswer = $"برای {product.Name}، شرایط بسته‌بندی و حداقل سفارش عمده را کارشناسان موثقی بر اساس کد {product.ProductCode ?? "محصول"} اعلام می‌کنند.",
                Answer = $"پاسخ به «{q}» برای مدل {product.Name} به ظرفیت، بسته‌بندی فله/شیرینگ و موجودی انبار بستگی دارد. با تماس با فروش موثقی استعلام بگیرید."
            });
        }

        return SeoFaqStore.Serialize(faqs.Take(8).ToList());
    }

    static string? DescribePackaging(Product product)
    {
        var v = product.Variants.Where(x => x.IsActive).ToList();
        if (v.Count == 0) return null;
        var parts = v.Select(x => x.PackLabel).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().Take(3);
        return string.Join(" · ", parts);
    }

    static string E(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
