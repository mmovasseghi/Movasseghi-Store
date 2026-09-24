using System.Text.Json;
using System.Text.RegularExpressions;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

// ─────────────────────────────────────────────────────────────
//  FAQ — مدل یکپارچه
//  قبلاً سه شکل مختلف JSON در سیستم بود:
//    محصول  → { Question, ShortAnswer, FullAnswer }
//    دسته    → { question, answer, shortAnswer }
//    خواننده → { Question, Answer }
//  نتیجه: تحلیلگر AEO همیشه پاسخ‌ها را خالی می‌دید و امتیاز AEO اشتباه بود.
// ─────────────────────────────────────────────────────────────

public sealed class SeoFaq
{
    public string Question { get; set; } = "";
    /// <summary>پاسخ کامل (می‌تواند HTML داشته باشد).</summary>
    public string Answer { get; set; } = "";
    /// <summary>پاسخ کوتاه قابل استخراج برای Featured Snippet / AI Overview.</summary>
    public string ShortAnswer { get; set; } = "";

    public string BestAnswer => !string.IsNullOrWhiteSpace(Answer) ? Answer : ShortAnswer;
    public string PlainAnswer => SeoText.StripHtml(BestAnswer);
    public string PlainShortAnswer => SeoText.StripHtml(
        !string.IsNullOrWhiteSpace(ShortAnswer) ? ShortAnswer : Answer);
}

public static class SeoFaqStore
{
    static readonly JsonSerializerOptions WriteOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    /// <summary>پارس مقاوم — هر سه شکل تاریخی JSON را می‌خواند.</summary>
    public static List<SeoFaq> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var list = new List<SeoFaq>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;

                var q = Str(el, "question", "Question", "q");
                var full = Str(el, "answer", "Answer", "fullAnswer", "FullAnswer");
                var shortA = Str(el, "shortAnswer", "ShortAnswer", "snippet");

                if (string.IsNullOrWhiteSpace(q)) continue;
                if (string.IsNullOrWhiteSpace(full) && string.IsNullOrWhiteSpace(shortA)) continue;

                list.Add(new SeoFaq
                {
                    Question = q.Trim(),
                    Answer = (string.IsNullOrWhiteSpace(full) ? shortA : full).Trim(),
                    ShortAnswer = (string.IsNullOrWhiteSpace(shortA) ? full : shortA).Trim()
                });
            }
            return list;
        }
        catch { return []; }
    }

    public static string Serialize(IEnumerable<SeoFaq> faqs) =>
        JsonSerializer.Serialize(faqs.Select(f => new
        {
            question = f.Question,
            answer = f.Answer,
            shortAnswer = f.ShortAnswer
        }), WriteOptions);

    static string Str(JsonElement el, params string[] names)
    {
        foreach (var n in names)
            if (el.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString() ?? "";

        // تطبیق بدون حساسیت به حروف بزرگ/کوچک
        foreach (var prop in el.EnumerateObject())
            if (names.Any(n => string.Equals(n, prop.Name, StringComparison.OrdinalIgnoreCase))
                && prop.Value.ValueKind == JsonValueKind.String)
                return prop.Value.GetString() ?? "";

        return "";
    }
}

// ─────────────────────────────────────────────────────────────
//  Anti-AI-Generic Layer
// ─────────────────────────────────────────────────────────────

/// <summary>تشخیص جملات کلیشه‌ای و بی‌ارزش تولیدشده توسط AI.</summary>
public static partial class SeoGenericDetector
{
    /// <summary>عبارت‌های پرچم‌قرمز — بدون اطلاعات واقعی، فقط پرکننده.</summary>
    public static readonly string[] GenericPhrases =
    [
        "با کیفیت بالا طراحی شده", "انتخابی مناسب برای شما", "بهترین انتخاب برای شما",
        "در دنیای امروز", "لازم به ذکر است", "بدون شک", "همانطور که می‌دانید",
        "همانطور که میدانید", "در نهایت می‌توان گفت", "شایان ذکر است",
        "می‌تواند گزینه مناسبی باشد", "کیفیت بی‌نظیر", "کیفیت بی نظیر",
        "رضایت شما", "بالاترین سطح کیفیت", "یکی از پرتقاضات‌ترین",
        "یکی از پرتقاضاترین", "توجه به نیاز مشتریان", "با توجه به اهمیت",
        "نقش بسیار مهمی دارد", "روز به روز در حال افزایش",
        "امیدواریم این مطلب", "در این مقاله قصد داریم", "به جرات می‌توان گفت",
        "از اهمیت بالایی برخوردار است", "گزینه‌ای ایده‌آل", "گزینه ای ایده آل",
        "نمایندگی رسمی آملون در تهران", "ضمانت اصالت ۱۰۰٪", "مشاوره رایگان انتخاب",
        "خرید عمده با حداقل", "ارسال سراسری ۲ تا ۷ روز", "تیم پشتیبانی موثقی",
        "کیفیت یکنواخت", "تأمین مستمر", "برای رستوران، فست‌فود، کافه"
    ];

    /// <summary>کلمات مبهم — اگر جمله فقط از این‌ها ساخته شده باشد، اطلاعاتی ندارد.</summary>
    static readonly string[] VagueWords =
    [
        "عالی", "بی‌نظیر", "فوق‌العاده", "بهترین", "مناسب", "مطلوب", "ایده‌آل", "کامل"
    ];

    /// <summary>امتیاز ریسک ۰–۱۰۰. هدف زیر ۱۲٪.</summary>
    public static double Score(IReadOnlyList<string> paragraphs)
    {
        var sentences = paragraphs
            .SelectMany(p => SentenceRegex().Split(p))
            .Select(s => s.Trim())
            .Where(s => SeoText.CountWords(s) >= 4)
            .ToList();

        if (sentences.Count == 0) return 0;

        var flagged = 0;
        foreach (var s in sentences)
        {
            var norm = SeoText.Normalize(s);
            var hasGeneric = GenericPhrases.Any(g => norm.Contains(SeoText.Normalize(g), StringComparison.Ordinal));
            // جمله‌ای که هیچ عدد/داده مشخصی ندارد و پر از صفت مبهم است
            var vagueHits = VagueWords.Count(v => norm.Contains(SeoText.Normalize(v), StringComparison.Ordinal));
            var hasFact = DigitRegex().IsMatch(s);
            if (hasGeneric || (vagueHits >= 2 && !hasFact)) flagged++;
        }

        // جریمه اضافه برای شروع‌های تکراری جمله (نشانه تولید ماشینی)
        var openings = sentences
            .Select(s => string.Join(' ', SeoText.Tokenize(s).Take(3)))
            .Where(o => o.Length > 0)
            .GroupBy(o => o)
            .Count(g => g.Count() >= 3);

        var risk = flagged * 100.0 / sentences.Count + openings * 4.0;
        return Math.Round(Math.Min(100, risk), 1);
    }

    public static List<string> FindGenericSamples(IReadOnlyList<string> paragraphs, int take = 3) =>
        paragraphs
            .SelectMany(p => SentenceRegex().Split(p))
            .Select(s => s.Trim())
            .Where(s => GenericPhrases.Any(g =>
                SeoText.Normalize(s).Contains(SeoText.Normalize(g), StringComparison.Ordinal)))
            .Distinct()
            .Take(take)
            .ToList();

    [GeneratedRegex(@"[.!?؟]+\s*")]
    private static partial Regex SentenceRegex();

    [GeneratedRegex(@"[0-9\u06f0-\u06f9]")]
    private static partial Regex DigitRegex();
}

// ─────────────────────────────────────────────────────────────
//  Search Intent
// ─────────────────────────────────────────────────────────────

public static class SeoIntentDetector
{
    static readonly string[] TransactionalSignals =
        ["خرید", "سفارش", "قیمت", "عمده", "پخش", "فروش", "استعلام", "تخفیف", "ارسال"];
    static readonly string[] CommercialSignals =
        ["بهترین", "مقایسه", "تفاوت", "مشخصات", "بررسی", "کیفیت", "مدل", "انواع", "نمایندگی"];
    static readonly string[] InformationalSignals =
        ["چیست", "چگونه", "چرا", "آموزش", "راهنما", "کاربرد", "مزایا", "معایب", "تجزیه"];
    static readonly string[] NavigationalSignals =
        ["آملون", "موثقی", "برند", "سایت", "نمایندگی رسمی"];

    /// <summary>
    /// تشخیص ترکیب Intent از Keyword + Query‌های واقعی Search Console + Intent کلمات pillar.
    /// </summary>
    public static SeoIntentMix Detect(string focus, string secondary, IEnumerable<string>? serpTitles = null)
    {
        var corpus = string.Join(" ", new[] { focus, secondary }
            .Concat(serpTitles ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var t = Weight(corpus, TransactionalSignals) * 3;
        var c = Weight(corpus, CommercialSignals) * 2;
        var i = Weight(corpus, InformationalSignals) * 2;
        var n = Weight(corpus, NavigationalSignals);

        // پایه: فروشگاه B2B — پیش‌فرض به سمت تجاری/تراکنشی
        t += 6; c += 4; i += 1; n += 1;

        var sum = (double)(t + c + i + n);
        var mix = new SeoIntentMix
        {
            Transactional = (int)Math.Round(t / sum * 100),
            Commercial = (int)Math.Round(c / sum * 100),
            Informational = (int)Math.Round(i / sum * 100),
            Navigational = (int)Math.Round(n / sum * 100)
        };

        var pairs = new (string Key, string Fa, int Val)[]
        {
            ("transactional", "تراکنشی (خرید)", mix.Transactional),
            ("commercial", "تجاری (بررسی/مقایسه)", mix.Commercial),
            ("informational", "اطلاعاتی", mix.Informational),
            ("navigational", "برندی", mix.Navigational)
        };
        var top = pairs.OrderByDescending(p => p.Val).First();
        mix.Dominant = top.Key;
        mix.DominantFa = top.Fa;
        return mix;
    }

    static int Weight(string corpus, string[] signals) =>
        signals.Count(s => SeoText.Contains(corpus, s));
}

// ─────────────────────────────────────────────────────────────
//  Topic Authority
// ─────────────────────────────────────────────────────────────

public sealed record SeoTopicDef(string Name, int Weight, string[] Terms);

public static class SeoTopicModel
{
    /// <summary>
    /// موضوعاتی که یک صفحه محصول برای پوشش کامل «Topic» (نه فقط Keyword) نیاز دارد.
    /// </summary>
    public static List<SeoTopicDef> ForProduct(Product product, string focus, string secondary)
    {
        var head = SeoText.HeadPhrase(focus, product.Name);
        var topics = new List<SeoTopicDef>
        {
            new("محصول", 3, [head, product.Name]),
            new("برند", 3, ["آملون", "برند", "تولیدکننده"]),
            new("جنس و مواد", 3, ["جنس", "نشاسته", "گیاهی", "مواد اولیه", product.Material ?? "گیاهی"]),
            new("ویژگی‌ها", 3, ["ویژگی", "مشخصات", "ابعاد", "ظرفیت", "وزن"]),
            new("کاربرد", 3, ["کاربرد", "رستوران", "کیترینگ", "فست‌فود", "کافه", "بیرون‌بر"]),
            new("کیفیت و استاندارد", 2, ["کیفیت", "استاندارد", "بهداشتی", "مقاومت", "ضمانت"]),
            new("قیمت", 3, ["قیمت", "نرخ", "استعلام", "تومان"]),
            new("خرید", 3, ["خرید", "سفارش", "سبد", "تهیه"]),
            new("خرید عمده", 3, ["عمده", "پخش", "کارتن", "b2b", "بنکدار"]),
            new("حداقل سفارش", 2, ["حداقل سفارش", "میلیون تومان", "حداقل خرید"]),
            new("ارسال و تحویل", 2, ["ارسال", "تحویل", "باربری", "روز کاری", "لجستیک"]),
            new("موقعیت مکانی", 2, ["تهران", "سراسر کشور", "ایران"]),
            new("اصالت و نمایندگی", 2, ["اصالت", "نمایندگی", "رسمی", "اصل"]),
            new("مقایسه", 2, ["مقایسه", "تفاوت", "در مقابل", "جایگزین", "پلاستیک"]),
            new("نحوه سفارش", 2, ["نحوه سفارش", "مراحل", "تماس", "کارشناس", "مشاوره"]),
            new("سوالات متداول", 2, ["سوالات متداول", "پرسش", "؟"])
        };

        if (product.MicrowaveSafe)
            topics.Add(new SeoTopicDef("مایکروویو", 2, ["مایکروویو", "گرم کردن", "حرارت"]));
        if (product.CapacityCc is > 0)
            topics.Add(new SeoTopicDef("ظرفیت", 2, ["سی‌سی", "ظرفیت", "حجم"]));
        if (!string.IsNullOrWhiteSpace(product.ProductCode))
            topics.Add(new SeoTopicDef("کد محصول", 1, ["کد محصول", product.ProductCode!]));
        if (!string.IsNullOrWhiteSpace(secondary))
            topics.Add(new SeoTopicDef("کلمه فرعی", 2, [secondary]));

        return topics;
    }

    public static List<SeoTopicDef> ForCategory(Category category, string focus, string secondary) =>
    [
        new("دسته", 3, [category.Name, SeoText.HeadPhrase(focus, category.Name)]),
        new("برند", 3, ["آملون", "برند", "نمایندگی"]),
        new("تنوع محصول", 3, ["محصول", "انواع", "مدل", "سایز"]),
        new("جنس و مواد", 2, ["جنس", "گیاهی", "نشاسته"]),
        new("کاربرد", 3, ["کاربرد", "رستوران", "کیترینگ", "فست‌فود", "کافه"]),
        new("قیمت", 3, ["قیمت", "استعلام", "تومان"]),
        new("خرید عمده", 3, ["عمده", "پخش", "کارتن"]),
        new("حداقل سفارش", 2, ["حداقل سفارش", "میلیون تومان"]),
        new("ارسال", 2, ["ارسال", "تحویل", "روز کاری"]),
        new("موقعیت مکانی", 2, ["تهران", "سراسر کشور"]),
        new("راهنمای انتخاب", 2, ["راهنما", "انتخاب", "چطور"]),
        new("مقایسه", 2, ["مقایسه", "تفاوت", "پلاستیک"]),
        new("سوالات متداول", 2, ["سوالات متداول", "؟"]),
        new("کلمه فرعی", 2, [string.IsNullOrWhiteSpace(secondary) ? SiteKeywordStrategy.Secondary : secondary])
    ];

    /// <summary>پوشش موضوعی وزنی — هدف ≥۹۰٪.</summary>
    public static (double Coverage, List<SeoTopicCoverage> Detail) Evaluate(
        IEnumerable<SeoTopicDef> topics, string haystack)
    {
        var detail = new List<SeoTopicCoverage>();
        double gained = 0, possible = 0;

        foreach (var t in topics)
        {
            var mentions = t.Terms
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .Sum(term => SeoText.CountPhrase(haystack, term));

            possible += t.Weight;
            if (mentions > 0) gained += t.Weight;

            detail.Add(new SeoTopicCoverage
            {
                Topic = t.Name,
                Weight = t.Weight,
                Mentions = mentions,
                Covered = mentions > 0
            });
        }

        var coverage = possible > 0 ? Math.Round(gained / possible * 100, 1) : 0;
        return (coverage, detail);
    }
}

// ─────────────────────────────────────────────────────────────
//  Semantic Entity Graph
// ─────────────────────────────────────────────────────────────

public sealed record SeoEntityDef(string Name, string Kind, bool Required, string[] Aliases);

public static class SeoEntityGraph
{
    public static List<SeoEntityDef> ForProduct(Product product)
    {
        var list = new List<SeoEntityDef>
        {
            new("آملون", "Brand", true, ["آملون", "amelon"]),
            new("موثقی", "Business", true, ["موثقی", "فروشگاه موثقی"]),
            new("تهران", "Location", true, ["تهران"]),
            new("نمایندگی رسمی", "Credential", true, ["نمایندگی رسمی", "نمایندگی"]),
            new("ظروف یکبار مصرف گیاهی", "Category", true,
                [SiteKeywordStrategy.Primary, "ظروف گیاهی", "یکبار مصرف گیاهی"]),
            new("خرید عمده", "Offer", true, ["عمده", "پخش عمده", "خرید عمده"])
        };

        if (!string.IsNullOrWhiteSpace(product.Name))
            list.Add(new SeoEntityDef(product.Name.Trim(), "Product", true, [product.Name.Trim()]));
        if (!string.IsNullOrWhiteSpace(product.ProductCode))
            list.Add(new SeoEntityDef(product.ProductCode!, "SKU", true, [product.ProductCode!]));
        if (!string.IsNullOrWhiteSpace(product.Material))
            list.Add(new SeoEntityDef(product.Material!, "Material", false, [product.Material!]));
        if (product.Category is not null && !string.IsNullOrWhiteSpace(product.Category.Name))
            list.Add(new SeoEntityDef(product.Category.Name, "Collection", false, [product.Category.Name]));
        if (product.CapacityCc is > 0)
            list.Add(new SeoEntityDef($"{product.CapacityCc} سی‌سی", "Attribute", false,
                [$"{product.CapacityCc}", "سی‌سی"]));

        return list;
    }

    public static List<SeoEntityDef> ForCategory(Category category) =>
    [
        new("آملون", "Brand", true, ["آملون", "amelon"]),
        new("موثقی", "Business", true, ["موثقی"]),
        new("تهران", "Location", true, ["تهران"]),
        new("نمایندگی رسمی", "Credential", true, ["نمایندگی"]),
        new(category.Name, "Collection", true, [category.Name]),
        new("ظروف یکبار مصرف گیاهی", "Category", true, [SiteKeywordStrategy.Primary, "ظروف گیاهی"]),
        new("خرید عمده", "Offer", true, ["عمده", "پخش عمده"])
    ];

    /// <summary>
    /// Confidence = چند بار و در چند «سیگنال» (متن، عنوان، Meta، خلاصه GEO، داده محصول) آمده.
    /// Consistency = در چند سیگنال مستقل حاضر است (Entity فقط در یک جا = ضعیف).
    /// </summary>
    public static (double Coverage, List<SeoEntityScore> Detail) Evaluate(
        IEnumerable<SeoEntityDef> entities, params (string Signal, string Text)[] signals)
    {
        var detail = new List<SeoEntityScore>();
        var required = 0;
        var requiredCovered = 0;

        foreach (var e in entities)
        {
            var presentIn = 0;
            var totalMentions = 0;

            foreach (var (_, text) in signals)
            {
                var hits = e.Aliases.Where(a => !string.IsNullOrWhiteSpace(a))
                    .Sum(a => SeoText.CountPhrase(text, a));
                if (hits > 0) presentIn++;
                totalMentions += hits;
            }

            var signalCount = Math.Max(1, signals.Length);
            var consistency = (int)Math.Round(presentIn * 100.0 / signalCount);

            // Confidence بر پایه «شهادت» است نه تعداد سیگنال‌ها.
            // برخی Entity‌ها (مثل تهران) طبیعتاً در SKU یا نام محصول نمی‌آیند،
            // پس تقسیم بر تعداد کل سیگنال‌ها آن‌ها را بی‌دلیل ضعیف نشان می‌داد.
            var confidence = (int)Math.Min(100, Math.Round(totalMentions * 15.0 + presentIn * 12.0));

            detail.Add(new SeoEntityScore
            {
                Entity = e.Name,
                Kind = e.Kind,
                IsRequired = e.Required,
                Confidence = confidence,
                Consistency = consistency,
                Evidence = confidence switch
                {
                    >= 80 => "High",
                    >= 50 => "Medium",
                    > 0 => "Low",
                    _ => "None"
                }
            });

            if (e.Required)
            {
                required++;
                if (confidence >= 50) requiredCovered++;
            }
        }

        var coverage = required > 0 ? Math.Round(requiredCovered * 100.0 / required, 1) : 100;
        return (coverage, detail.OrderByDescending(d => d.IsRequired).ThenBy(d => d.Confidence).ToList());
    }
}

// ─────────────────────────────────────────────────────────────
//  Query Expansion
// ─────────────────────────────────────────────────────────────

public static class SeoQueryExpander
{
    /// <summary>Query‌های مرتبطی که صفحه باید پاسخ بدهد.</summary>
    public static List<SeoQueryTarget> Expand(string focus, string secondary, Product? product = null)
    {
        var head = SeoText.HeadPhrase(focus, product?.Name);
        var list = new List<SeoQueryTarget>
        {
            new() { Query = $"خرید {head}", Kind = "transactional" },
            new() { Query = $"قیمت {head}", Kind = "transactional" },
            new() { Query = $"قیمت عمده {head}", Kind = "transactional" },
            new() { Query = $"خرید عمده {head}", Kind = "transactional" },
            new() { Query = $"مشخصات {head}", Kind = "commercial" },
            new() { Query = $"بهترین {head}", Kind = "commercial" },
            new() { Query = $"تفاوت {head} با پلاستیک", Kind = "commercial" },
            new() { Query = $"کاربرد {head}", Kind = "informational" },
            new() { Query = $"نحوه سفارش {head}", Kind = "informational" },
            new() { Query = $"{head} تهران", Kind = "local" },
            new() { Query = $"نمایندگی آملون {head}", Kind = "navigational" }
        };

        if (product?.MicrowaveSafe == true)
            list.Add(new SeoQueryTarget { Query = $"آیا {head} مایکروویوی است", Kind = "informational" });
        if (!string.IsNullOrWhiteSpace(secondary))
            list.Add(new SeoQueryTarget { Query = $"خرید {secondary}", Kind = "transactional" });

        return list;
    }

    /// <summary>
    /// یک Query «پاسخ داده شده» است اگر در H2/H3، سوالات FAQ یا بدنه، توکن‌های معنادار آن پوشش داده شده باشد.
    /// </summary>
    public static void MarkAnswered(
        List<SeoQueryTarget> queries,
        IEnumerable<string> headings,
        IEnumerable<SeoFaq> faqs,
        string bodyText)
    {
        var answerSurfaces = headings
            .Concat(faqs.Select(f => $"{f.Question} {f.PlainShortAnswer}"))
            .ToList();

        foreach (var q in queries)
        {
            q.Answered = answerSurfaces.Any(s => SeoText.IsKeywordish(s, q.Query, 0.75))
                || SeoText.IsKeywordish(bodyText, q.Query, 0.95);
        }
    }

    /// <summary>
    /// Query Opportunity — چیزی که Search Console نشان می‌دهد کاربر جستجو می‌کند
    /// ولی صفحه پاسخ روشنی برای آن ندارد.
    /// </summary>
    public static List<SeoQueryTarget> FromSearchConsole(
        IEnumerable<GscQueryRow> rows, string focus, IEnumerable<string> answerSurfaces)
    {
        var head = SeoText.HeadPhrase(focus);
        var surfaces = answerSurfaces.ToList();

        return rows
            .Where(r => SeoText.IsKeywordish(r.Query, head, 0.5))
            .Select(r => new SeoQueryTarget
            {
                Query = r.Query,
                Kind = "search-console",
                FromSearchConsole = true,
                Impressions = r.Impressions,
                Clicks = r.Clicks,
                Position = r.Position,
                Answered = surfaces.Any(s => SeoText.IsKeywordish(s, r.Query, 0.75))
            })
            .OrderByDescending(r => r.Impressions)
            .Take(15)
            .ToList();
    }
}
