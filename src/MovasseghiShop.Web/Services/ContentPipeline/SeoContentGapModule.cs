using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services.ContentPipeline;

public static class SeoContentGapModule
{
    static readonly string[] OurStandardTopics =
    [
        "مشخصات", "کاربرد", "مقایسه", "خرید عمده", "قیمت", "بسته‌بندی",
        "نگهداری", "مایکروویو", "ضمانت", "کد محصول", "موثقی", "آملون"
    ];

    public static ContentGapInsight Analyze(Product product, SeoContentResearchBundle research)
    {
        var accepted = research.Competitors.Where(c => c.Accepted).ToList();
        var themes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in accepted)
        {
            foreach (var h in page.H2.Concat(page.H3))
            {
                var key = Normalize(h);
                if (key.Length < 4) continue;
                themes[key] = themes.GetValueOrDefault(key) + 1;
            }
        }

        var common = themes
            .Where(kv => kv.Value >= Math.Max(2, accepted.Count / 2))
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .Take(8)
            .ToList();

        var unique = new List<string>
        {
            $"راهنمای انتخاب {product.Name} برای خط تولید و سرو روزانه (بر اساس کد {product.ProductCode ?? "SKU"})",
            "چک‌لیست انبارداری و چیدمان کارتن برای سفارش‌های تکرارشونده عمده",
            "معیارهای کنترل کیفیت دریافت (لبه، ضخامت، یکنواختی رنگ) قبل از پذیرش محموله",
            "هم‌ترازی با الزامات برندینگ زیست‌محیطی و مستندسازی خرید از نمایندگی رسمی"
        };

        if (product.MicrowaveSafe)
            unique.Add("نکات گرم‌کردن و سرو در مایکروویو برای این مدل — بدون تغییر شکل در دمای سرو معمول");

        var suggestedH2 = new List<string>
        {
            $"خلاصه سریع برای تصمیم‌گیر خرید {product.Name}",
            $"مشخصات فنی و بسته‌بندی {product.Name}",
            "کاربرد در رستوران، فست‌فود و کیترینگ",
            "شرایط خرید عمده از موثقی (حداقل سفارش و ارسال)"
        };

        foreach (var u in unique.Take(3))
            suggestedH2.Add(u);

        return new ContentGapInsight
        {
            CommonCompetitorThemes = common,
            UniqueAnglesForUs = unique.Take(5).ToList(),
            SuggestedH2 = suggestedH2
        };
    }

    public static ContentGapInsight AnalyzeEditorial(
        string focus, string title, SeoContentResearchBundle research)
    {
        var accepted = research.Competitors.Where(c => c.Accepted).ToList();
        var themes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in accepted)
        {
            foreach (var h in page.H2.Concat(page.H3))
            {
                var key = Normalize(h);
                if (key.Length < 4) continue;
                themes[key] = themes.GetValueOrDefault(key) + 1;
            }
        }

        var common = themes
            .Where(kv => kv.Value >= Math.Max(2, accepted.Count / 2))
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .Take(8)
            .ToList();

        var head = SeoText.HeadPhrase(focus, title);
        var unique = new List<string>
        {
            $"چک‌لیست خرید عمده {head} برای رستوران و کیترینگ",
            "مقایسه بسته‌بندی فله، شرینگ و کارتن در سفارش ماهانه",
            "نکات انبارداری ظروف گیاهی در آب‌وهوای گرم تهران",
            "مسیر استعلام قیمت و پیش‌فاکتور از نمایندگی رسمی آملون"
        };

        var suggestedH2 = new List<string>
        {
            $"راهنمای انتخاب {head}",
            $"قیمت و شرایط عمده {head}",
            "مقایسه با ظروف پلاستیکی",
            "سوالات پرتکرار خریداران B2B"
        };

        foreach (var u in unique.Take(2))
            suggestedH2.Add(u);

        return new ContentGapInsight
        {
            CommonCompetitorThemes = common,
            UniqueAnglesForUs = unique,
            SuggestedH2 = suggestedH2
        };
    }

    static string Normalize(string h) =>
        h.Replace("–", "-").Trim();
}
