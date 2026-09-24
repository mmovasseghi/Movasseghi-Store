using System.Text;
using System.Text.RegularExpressions;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>
/// Content Blueprint + تولید محتوا.
/// اصل کار: هر بخش اطلاعات «متفاوت و مشخص» می‌دهد — هیچ پاراگرافی تکرار نمی‌شود
/// و جای Keyword به‌صورت کنترل‌شده تعیین می‌شود تا چگالی طبیعی بماند.
/// </summary>
/// <summary>
/// توزیع کنترل‌شده عبارت کلیدی در متن.
/// متن با جای‌نگهدار نوشته می‌شود و در پایان بخشی از جای‌نگهدارها «عبارت کامل» و
/// بقیه «شکل کوتاه» می‌گیرند تا چگالی در بازه طبیعی بنشیند و Stuffing رخ ندهد.
/// </summary>
public static class SeoKeywordPlacer
{
    public const string Marker = "[[F]]";

    /// <summary>میانه بازه مجاز چگالی.</summary>
    const double TargetDensity = 1.5;

    public static string Resolve(string html, string focus, string shortLabel)
    {
        var markerCount = Count(html);
        if (markerCount == 0)
            return EnforceDensityCap(html, focus, shortLabel);
        if (string.IsNullOrWhiteSpace(focus)) return html.Replace(Marker, Escape(shortLabel));

        var phraseWords = Math.Max(1, SeoText.CountWords(focus));
        var chosen = markerCount;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            var candidate = Fill(html, focus, shortLabel, chosen);
            var plain = SeoText.StripHtml(candidate);
            var words = SeoText.CountWords(plain);
            if (words == 0) break;

            // شمارش واقعی — شکل کوتاه یا نام محصول هم ممکن است عبارت کامل را در خود داشته باشد
            var actual = SeoText.CountPhrase(plain, focus);
            var density = actual * phraseWords * 100.0 / words;
            if (density is >= SeoContentRules.KeywordDensityMin and <= SeoContentRules.KeywordDensityMax)
                return candidate;

            var ideal = (int)Math.Round(TargetDensity * words / (100.0 * phraseWords));
            var incidental = Math.Max(0, actual - Math.Min(actual, chosen));
            var next = Math.Max(0, Math.Min(markerCount, ideal - incidental));
            if (next == chosen) break;
            chosen = next;
        }

        return EnforceDensityCap(Fill(html, focus, shortLabel, chosen), focus, shortLabel);
    }

    /// <summary>پس از Resolve، تکرارهای literal عبارت کامل را تا زیر سقف Stuffing کم می‌کند.</summary>
    public static string EnforceDensityCap(string html, string focus, string shortLabel)
    {
        if (string.IsNullOrWhiteSpace(html) || string.IsNullOrWhiteSpace(focus))
            return html;

        var phraseWords = Math.Max(1, SeoText.CountWords(focus));
        var replacement = Escape(shortLabel);

        for (var pass = 0; pass < 16; pass++)
        {
            var plain = SeoText.StripHtml(html);
            var words = SeoText.CountWords(plain);
            if (words == 0) break;

            var actual = SeoText.CountPhrase(plain, focus);
            var density = actual * phraseWords * 100.0 / words;
            if (density <= SeoContentRules.KeywordDensityStuffing)
                return html;

            var targetPct = SeoContentRules.KeywordDensityStuffing - 0.25;
            var maxAllowed = (int)Math.Floor(targetPct * words / (100.0 * phraseWords));
            maxAllowed = Math.Clamp(maxAllowed, 0, actual - 1);

            var trimmed = TrimExcessLiteralPhrase(html, focus, replacement, maxAllowed);
            if (trimmed != html)
            {
                html = trimmed;
                continue;
            }

            trimmed = TrimExcessFlexibleHtml(html, focus, replacement, maxAllowed);
            if (trimmed == html)
                break;
            html = trimmed;
        }

        return html;
    }

    /// <summary>حذف تکرار وقتی عبارت کلیدی بین تگ‌های HTML یا فاصله‌ها پخش شده.</summary>
    static string TrimExcessFlexibleHtml(string html, string focus, string replacement, int keepFull)
    {
        var parts = focus.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return html;

        var gap = @"(?:<[^>]+>|\s|[\u200c\u00a0])+";
        var pattern = string.Join(gap, parts.Select(Regex.Escape));
        var rx = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        var kept = 0;
        return rx.Replace(html, m =>
        {
            if (kept < keepFull)
            {
                kept++;
                return m.Value;
            }

            return replacement;
        });
    }

    static string TrimExcessLiteralPhrase(string html, string focus, string replacement, int keepFull)
    {
        if (keepFull < 0) keepFull = 0;
        var sb = new StringBuilder();
        var idx = 0;
        var kept = 0;

        while (idx < html.Length)
        {
            var found = html.IndexOf(focus, idx, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                sb.Append(html, idx, html.Length - idx);
                break;
            }

            sb.Append(html, idx, found - idx);
            if (kept < keepFull)
            {
                sb.Append(html, found, focus.Length);
                kept++;
            }
            else
                sb.Append(replacement);

            idx = found + focus.Length;
        }

        return sb.ToString();
    }

    static string Fill(string html, string focus, string shortLabel, int fullCount)
    {
        var sb = new StringBuilder();
        var idx = 0;
        var used = 0;

        while (true)
        {
            var next = html.IndexOf(Marker, idx, StringComparison.Ordinal);
            if (next < 0)
            {
                sb.Append(html, idx, html.Length - idx);
                break;
            }

            sb.Append(html, idx, next - idx);
            sb.Append(used < fullCount ? Escape(focus) : Escape(shortLabel));
            used++;
            idx = next + Marker.Length;
        }

        return sb.ToString();
    }

    static int Count(string html)
    {
        var count = 0;
        var idx = 0;
        while ((idx = html.IndexOf(Marker, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += Marker.Length;
        }
        return count;
    }

    static string Escape(string value) => value
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}

public enum SeoContentMode
{
    Full,
    Semantic,
    Intent,
    Internal,
    Aeo,
    /// <summary>بازنویسی با لایه تفکیک محصول و چیدمان متفاوت — برای عبور از آستانه یکتایی سایت.</summary>
    Unique
}

public sealed record SeoContentBuildOptions
{
    public SeoContentMode Mode { get; init; } = SeoContentMode.Full;
    public SeoIntentMix? Intent { get; init; }
    public IReadOnlyList<string> MissingTopics { get; init; } = [];
    public IReadOnlyList<string> UnansweredQueries { get; init; } = [];
    /// <summary>شماره تلاش یکتاسازی — هر بار ساختار و مقدمه متفاوت می‌شود.</summary>
    public int DifferentiationAttempt { get; init; }
}

public static class SeoUltimateWriter
{
    /// <summary>جای‌نگهدار Keyword — بعداً بین «عبارت کامل» و «شکل کوتاه» توزیع می‌شود.</summary>
    const string Marker = SeoKeywordPlacer.Marker;

    // ─────────────────────────────────────────────
    //  Meta / Snippet / GEO
    // ─────────────────────────────────────────────

    public static string BuildMetaTitle(string focus, string productName)
    {
        var head = SeoText.HeadPhrase(focus, productName);
        var candidates = new[]
        {
            $"{head} | خرید عمده آملون | موثقی",
            $"{head} | قیمت عمده و مشخصات | موثقی",
            $"خرید عمده {head} | فروشگاه موثقی",
            $"{head} | آملون | موثقی",
            $"{head} | خرید عمده"
        };

        foreach (var c in candidates)
            if (c.Length is >= SeoContentRules.MetaTitleMin and <= SeoContentRules.MetaTitleMax)
                return c;

        // اگر نام محصول خیلی بلند است، کوتاه می‌کنیم اما ساختار CTR را نگه می‌داریم
        var suffix = " | خرید عمده | موثقی";
        var room = SeoContentRules.MetaTitleMax - suffix.Length;
        var trimmed = head.Length > room ? head[..room].TrimEnd() : head;
        var title = trimmed + suffix;
        if (title.Length < SeoContentRules.MetaTitleMin)
            title = $"{trimmed} | آملون | موثقی";

        return FitMetaTitle(title);
    }

    static string FitMetaTitle(string title)
    {
        if (title.Length > SeoContentRules.MetaTitleMax)
            title = title[..SeoContentRules.MetaTitleMax].TrimEnd(' ', '|', '—', '-');

        if (title.Length < SeoContentRules.MetaTitleMin)
        {
            var pad = " | موثقی";
            while (title.Length < SeoContentRules.MetaTitleMin && title.Length + pad.Length <= SeoContentRules.MetaTitleMax)
                title += pad;
        }

        return title.Length > SeoContentRules.MetaTitleMax
            ? title[..SeoContentRules.MetaTitleMax].TrimEnd()
            : title;
    }

    public static string BuildMetaDescription(Product product, string focus, string secondary)
    {
        var head = SeoText.HeadPhrase(focus, product.Name);
        var spec = product.CapacityCc is > 0 ? $"{product.CapacityCc} سی‌سی"
            : !string.IsNullOrWhiteSpace(product.Dimensions) ? product.Dimensions!.Trim()
            : !string.IsNullOrWhiteSpace(product.Material) ? product.Material!.Trim()
            : "گیاهی";

        // چند نسخه با طول‌های مختلف — اولین موردی که در بازه ۱۴۰–۱۶۰ بنشیند
        var candidates = new[]
        {
            $"خرید عمده {head} ({spec}) از نمایندگی رسمی آملون در تهران. ضمانت اصالت، ارسال سراسری ۲ تا ۷ روز کاری، حداقل سفارش ۱۰ میلیون تومان و مشاوره رایگان انتخاب.",
            $"{head} با {spec} — {secondary} از فروشگاه موثقی، نمایندگی رسمی آملون. ضمانت اصالت، ارسال سراسری و استعلام قیمت عمده با مشاوره رایگان کارشناس فروش.",
            $"خرید عمده {head} از موثقی، نمایندگی رسمی آملون تهران. ضمانت اصالت، ارسال سراسری، حداقل سفارش ۱۰ میلیون تومان و استعلام قیمت روز با مشاوره رایگان.",
            $"{head} — خرید عمده از نمایندگی رسمی آملون. ضمانت اصالت، ارسال سراسری و مشاوره رایگان انتخاب سایز و بسته‌بندی برای رستوران، کافه و کیترینگ."
        };

        foreach (var c in candidates)
            if (c.Length is >= SeoContentRules.MetaDescMin and <= SeoContentRules.MetaDescMax)
                return c;

        return Fit(candidates[0], SeoContentRules.MetaDescMin, SeoContentRules.MetaDescMax);
    }

    public static string BuildShortDescription(Product product, string focus, string secondary)
    {
        var head = SeoText.HeadPhrase(focus, product.Name);
        var spec = product.CapacityCc is > 0 ? $"ظرفیت {product.CapacityCc} سی‌سی، " : "";
        var micro = product.MicrowaveSafe ? "قابل استفاده در مایکروویو، " : "";
        return $"{head} از برند آملون؛ {spec}{micro}جنس {product.Material ?? "گیاهی"}. " +
               $"مناسب رستوران، کافه، فست‌فود و کیترینگ. خرید عمده {secondary} از فروشگاه موثقی " +
               "با ضمانت اصالت و ارسال سراسری.";
    }

    /// <summary>خلاصه GEO — Entity + Evidence + Clarity برای ارجاع موتورهای مولد.</summary>
    public static string BuildAiSummary(Product product, string focus, string secondary)
    {
        var head = SeoText.HeadPhrase(focus, product.Name);
        var sku = string.IsNullOrWhiteSpace(product.ProductCode) ? "" : product.ProductCode!.Trim();
        var sb = new StringBuilder();

        sb.Append($"{focus} محصولی از برند آملون است که فروشگاه موثقی، نمایندگی رسمی آملون در تهران، ");
        sb.Append($"آن را به‌صورت عمده عرضه می‌کند. ");

        var facts = new List<string>();
        if (!string.IsNullOrWhiteSpace(product.Material)) facts.Add($"جنس {product.Material.Trim()}");
        if (product.CapacityCc is > 0) facts.Add($"ظرفیت {product.CapacityCc} سی‌سی");
        if (product.CompartmentCount is > 0) facts.Add($"{product.CompartmentCount} خانه");
        if (!string.IsNullOrWhiteSpace(product.Dimensions)) facts.Add($"ابعاد {product.Dimensions.Trim()}");
        if (product.MicrowaveSafe) facts.Add("قابل استفاده در مایکروویو");
        if (facts.Count == 0) facts.Add("ساخته‌شده از مواد گیاهی");

        sb.Append($"مشخصات کلیدی: {string.Join("، ", facts)}. ");
        if (sku.Length > 0) sb.Append($"کد محصول (SKU): {sku}. ");

        sb.Append($"این محصول در گروه {SiteKeywordStrategy.Primary} و {secondary} قرار می‌گیرد و ");
        sb.Append("برای رستوران، فست‌فود، کافه و شرکت‌های کیترینگ کاربرد دارد. ");
        sb.Append("شرایط فروش: حداقل سفارش عمده ۱۰ میلیون تومان، ارسال به سراسر ایران با زمان تحویل ۲ تا ۷ روز کاری، ");
        sb.Append($"ضمانت اصالت کالا و مشاوره رایگان انتخاب سایز و بسته‌بندی. ");
        sb.Append($"برای استعلام قیمت روز {head} با کارشناسان فروش موثقی تماس بگیرید.");

        return sb.ToString();
    }

    // ─────────────────────────────────────────────
    //  AEO — Answer Blocks
    //  ساختار هر پاسخ: پاسخ مستقیم ← اطلاعات پشتیبان ← داده محصول
    // ─────────────────────────────────────────────

    public static List<SeoFaq> BuildFaqs(Product product, string focus, string secondary, bool aeoMode = false)
    {
        var head = SeoText.HeadPhrase(focus, product.Name);
        var sku = string.IsNullOrWhiteSpace(product.ProductCode) ? "—" : product.ProductCode!.Trim();
        var material = string.IsNullOrWhiteSpace(product.Material) ? "مواد گیاهی" : product.Material!.Trim();
        var capacity = product.CapacityCc is > 0 ? $"{product.CapacityCc} سی‌سی" : "بسته به مدل";
        var dims = string.IsNullOrWhiteSpace(product.Dimensions) ? null : product.Dimensions!.Trim();
        var apps = string.IsNullOrWhiteSpace(product.Applications)
            ? "رستوران، فست‌فود و کیترینگ"
            : product.Applications!.Trim();

        var faqs = new List<SeoFaq>
        {
            Faq($"قیمت {head} چقدر است؟",
                $"قیمت {head} بر اساس نوع بسته‌بندی (فله، شرینک یا کارتن) و حجم سفارش تعیین می‌شود، " +
                $"بنابراین نرخ ثابت واحد ندارد. برای سفارش‌های عمده، تعداد کارتن و تداوم خرید روی قیمت نهایی اثر مستقیم دارد. " +
                $"جهت دریافت لیست قیمت روز کد {sku} با کارشناسان فروش موثقی تماس بگیرید یا از صفحه استعلام قیمت درخواست ثبت کنید."),

            Faq($"حداقل سفارش عمده {head} چقدر است؟",
                $"حداقل سفارش عمده در فروشگاه موثقی ۱۰ میلیون تومان است و این مبلغ می‌تواند ترکیبی از چند محصول باشد. " +
                $"برای مشتریان با خرید ماهانه تکرارشونده، شرایط پرداخت و تخفیف پله‌ای جداگانه بررسی می‌شود. " +
                "مشاوره تعیین سبد خرید بهینه رایگان است."),

            Faq($"جنس {head} چیست و چه ویژگی‌هایی دارد؟",
                $"جنس این محصول {material} است و در گروه {SiteKeywordStrategy.Primary} قرار می‌گیرد. " +
                $"ظرفیت آن {capacity} است و برای سرو گرم و سرد در سرویس‌دهی حرفه‌ای طراحی شده است. " +
                (product.MicrowaveSafe
                    ? "این مدل قابلیت استفاده در مایکروویو را دارد."
                    : "برای گرم کردن در مایکروویو، مشخصات فنی مدل را با کارشناس بررسی کنید.")),

            Faq($"تفاوت {head} با ظروف پلاستیکی معمولی چیست؟",
                $"تفاوت اصلی در مواد اولیه است: {head} از منابع گیاهی تولید می‌شود، در حالی که ظروف پلاستیکی معمولی پایه نفتی دارند. " +
                "این تفاوت روی تصویر برند، انطباق با الزامات زیست‌محیطی و ترجیح مشتری اثر می‌گذارد. " +
                "در بسیاری از کاربردهای سرو گرم نیز رفتار حرارتی مناسب‌تری دارد."),

            Faq($"آیا {head} اصل و دارای ضمانت اصالت است؟",
                "بله. فروشگاه موثقی نمایندگی رسمی آملون در تهران است و کالا مستقیماً از مسیر رسمی تأمین می‌شود. " +
                $"کد محصول {sku} روی فاکتور درج می‌شود و امکان پیگیری اصالت وجود دارد. " +
                "خرید از نمایندگی رسمی، تأمین مستمر انبار برای سفارش‌های تکرارشونده را هم تضمین می‌کند."),

            Faq($"چگونه {head} را سفارش دهم؟",
                "ابتدا مدل و ظرفیت مورد نیاز را از کاتالوگ انتخاب کنید، سپس تعداد کارتن و نوع بسته‌بندی را به کارشناس فروش اعلام کنید. " +
                "پس از تأیید پیش‌فاکتور و تسویه، سفارش برای ارسال آماده می‌شود. " +
                "کل فرآیند از استعلام تا تحویل معمولاً ۲ تا ۷ روز کاری طول می‌کشد."),

            Faq($"کاربرد {head} در چه کسب‌وکارهایی است؟",
                $"{head} برای {apps} کاربرد دارد. " +
                "در سرویس بیرون‌بر و پذیرایی مراسم، سرعت آماده‌سازی و ظاهر یکدست سرویس اهمیت زیادی دارد. " +
                $"برای این مدل، {secondary} در خط تولید و سرو غذای گرم انتخاب رایجی است."),

            Faq($"شرایط ارسال {head} به شهرستان چگونه است؟",
                "ارسال به سراسر ایران انجام می‌شود و زمان تحویل معمولاً ۲ تا ۷ روز کاری بسته به مقصد است. " +
                "برای سفارش‌های حجیم، بارگیری از انبار تهران و هماهنگی باربری توسط تیم پشتیبانی انجام می‌شود. " +
                "هزینه حمل بر اساس حجم، وزن و مقصد در پیش‌فاکتور اعلام می‌شود.")
        };

        if (product.MicrowaveSafe)
            faqs.Add(Faq($"آیا {head} در مایکروویو قابل استفاده است؟",
                $"بله، این مدل ({sku}) برای گرم کردن در مایکروویو طراحی شده است. " +
                "برای سرو بیرون‌بر، بهتر است غذا کمی از دمای جوش پایین‌تر بیاید تا بخار روی کیفیت ظرف اثر نگذارد. " +
                "در صورت نیاز به دمای بالاتر، مشخصات فنی همان بچ تولید را با کارشناس تأیید کنید."));

        if (dims is not null)
            faqs.Add(Faq($"ابعاد {head} چقدر است؟",
                $"ابعاد ثبت‌شده این مدل {dims} است و برای هماهنگی با بسته‌بندی نهایی و چیدمان در کارتن استفاده می‌شود. " +
                $"ظرفیت سرو {capacity} است؛ اگر پرس غذای شما متفاوت است، کارشناس موثقی مدل نزدیک‌تر را پیشنهاد می‌دهد."));

        if (product.CompartmentCount is > 1)
            faqs.Add(Faq($"{head} چند خانه دارد و برای چه منویی مناسب است؟",
                $"این مدل {product.CompartmentCount} خانه دارد و برای منوهای چندماده‌ای (غذای اصلی + مخلفات) مناسب است. " +
                "در کیترینگ و فست‌فود، جداسازی مایعات و غذای خشک از سرعت سرو و رضایت مشتری حمایت می‌کند."));

        if (aeoMode && product.CapacityCc is > 0)
            faqs.Add(Faq($"برای پرس {capacity} چه مدلی از {head} مناسب‌تر است؟",
                $"برای پرس حدود {capacity}، مدل {head} با کد {sku} تطبیق خوبی دارد. " +
                "اگر منوی شما پرس سنگین‌تر دارد، یک سایز بالاتر از کاتالوگ انتخاب کنید تا لبه ظرف تحت فشار نشکند."));

        // ترتیب سوالات را بر اساس شناسه محصول جابه‌جا می‌کنیم تا الگوی یکسان در همه صفحات دیده نشود
        if (faqs.Count > 4)
        {
            var offset = Math.Abs(product.Id) % faqs.Count;
            faqs = faqs.Skip(offset).Concat(faqs.Take(offset)).ToList();
        }

        return faqs.Take(aeoMode ? 10 : 8).ToList();
    }

    static SeoFaq Faq(string question, string answer)
    {
        var q = question.TrimEnd();
        if (!q.EndsWith('؟') && !q.EndsWith('?')) q += "؟";

        // پاسخ کوتاه = جمله اول (بلوک قابل استخراج برای Featured Snippet)
        var firstStop = answer.IndexOf('.');
        var shortAnswer = firstStop > 30 && firstStop < answer.Length - 1
            ? answer[..(firstStop + 1)].Trim()
            : answer;

        // پاسخ کوتاه باید در بازه قابل استخراج بماند
        if (SeoText.CountWords(shortAnswer) < SeoContentRules.MinFaqAnswerWords)
            shortAnswer = answer;

        return new SeoFaq { Question = q, Answer = answer.Trim(), ShortAnswer = shortAnswer };
    }

    // ─────────────────────────────────────────────
    //  Content Blueprint
    // ─────────────────────────────────────────────

    /// <summary>
    /// ساختار محتوا بر اساس Intent غالب، موضوعات پوشش‌نداده و Query‌های بی‌پاسخ ساخته می‌شود.
    /// </summary>
    public static List<string> BuildBlueprint(SeoIntentMix intent, IEnumerable<string> missingTopics)
    {
        var plan = new List<string>
        {
            "مقدمه + پاسخ مستقیم",
            "معرفی و کاربرد (عنوان سوالی)",
            "جدول مشخصات و ابعاد",
            "ویژگی‌ها و مزایا",
            "کاربرد در صنف‌های مختلف"
        };

        if (intent.Dominant is "transactional" or "commercial")
        {
            plan.Add("مقایسه با پلاستیک (جدول)");
            plan.Add("راهنمای انتخاب سایز و بسته‌بندی");
            plan.Add("قیمت (عنوان سوالی)");
            plan.Add("مراحل سفارش و خرید عمده");
            plan.Add("ارسال و حداقل سفارش");
        }
        else
        {
            plan.Add("راهنمای انتخاب سایز و بسته‌بندی");
            plan.Add("مقایسه با پلاستیک (جدول)");
            plan.Add("نگهداری و نکات فنی");
            plan.Add("مراحل سفارش و خرید عمده");
        }

        plan.AddRange(missingTopics.Select(t => $"پوشش موضوع: {t}"));
        plan.Add("سوالات متداول خریداران عمده");
        return plan;
    }

    /// <summary>
    /// تولید بدنه کامل محتوا. خروجی همه قوانین ساختاری را برآورده می‌کند:
    /// جدول مشخصات، جدول مقایسه، لیست مرحله‌ای، عناوین سوالی،
    /// بلوک‌های پاسخ مستقیم و لینک داخلی با Anchor متنوع.
    /// </summary>
    public static string BuildContent(
        Product product,
        string focus,
        string secondary,
        string? categorySlug = null,
        SeoContentBuildOptions? options = null)
    {
        options ??= new SeoContentBuildOptions();
        var intent = options.Intent ?? SeoIntentDetector.Detect(focus, secondary);
        var blueprint = BuildBlueprint(intent, options.MissingTopics);

        var head = SeoText.HeadPhrase(focus, product.Name);
        var name = E(product.Name.Trim());
        var sku = string.IsNullOrWhiteSpace(product.ProductCode) ? "—" : E(product.ProductCode!.Trim());
        var material = E(string.IsNullOrWhiteSpace(product.Material) ? "مواد گیاهی (نشاسته)" : product.Material!.Trim());
        var dimensions = string.IsNullOrWhiteSpace(product.Dimensions) ? "بر اساس مدل" : E(product.Dimensions!.Trim());
        var capacity = product.CapacityCc is > 0 ? $"{product.CapacityCc} سی‌سی" : "بر اساس مدل";
        var compartments = product.CompartmentCount is > 0 ? $"{product.CompartmentCount} خانه" : "تک‌خانه";
        var micro = product.MicrowaveSafe ? "قابل استفاده" : "بر اساس مدل — با کارشناس بررسی شود";
        var apps = string.IsNullOrWhiteSpace(product.Applications)
            ? "رستوران، فست‌فود، کافه، کیترینگ"
            : E(product.Applications!.Trim());
        var type = E(string.IsNullOrWhiteSpace(product.ProductType) ? "ظروف یکبار مصرف" : product.ProductType!.Trim());

        // در بدنه متن از شکل کوتاه استفاده می‌شود؛ عبارت کامل فقط در جای‌نگهدارهای کنترل‌شده
        // قرار می‌گیرد تا چگالی طبیعی بماند و Keyword Stuffing رخ ندهد.
        var shortLabel = SeoText.ShortForm(focus, product.Name);
        var h = E(shortLabel);
        var sec = E(secondary);

        var sb = new StringBuilder();
        var transactionalFirst = options.Mode is SeoContentMode.Intent
            || intent.Dominant is "transactional" or "commercial";
        var attempt = options.DifferentiationAttempt;
        var emphasizeUnique = options.Mode is SeoContentMode.Unique || attempt > 0;

        if (emphasizeUnique)
            SeoProductDifferentiation.AppendIdentityLead(sb, product, Marker, attempt);
        else
            AppendIntro(sb, product, sku, material, capacity, apps, type, attempt);

        if (transactionalFirst)
            AppendTransactionalBlocks(sb, head, sec, sku, secondary);

        // ── ۲. عنوان سوالی + پاسخ مستقیم ──
        sb.Append("<h2>").Append(Marker).Append(" چیست و چه کاربردی دارد؟</h2>");
        sb.Append("<p>").Append(Marker).Append(" ظرفی است که از منابع گیاهی به‌جای پلیمرهای نفتی ساخته می‌شود و ")
          .Append("برای سرو یکبار مصرف در حجم بالا طراحی شده است. کاربرد اصلی آن در ").Append(apps)
          .Append(" است، جایی که سرعت آماده‌سازی و ظاهر یکدست سرویس اهمیت دارد. ")
          .Append("مدل حاضر در گروه ").Append(type).Append(" با ").Append(compartments)
          .Append(" دسته‌بندی می‌شود.</p>");

        sb.Append("<h3>چرا کسب‌وکارها به سمت ").Append(h).Append(" رفته‌اند؟</h3>");
        sb.Append("<p>سه عامل این تغییر را پیش برده است: الزامات زیست‌محیطی که مصرف پلاستیک یکبار مصرف را محدود می‌کند، ")
          .Append("ترجیح مشتری نهایی که بسته‌بندی گیاهی را نشانه کیفیت می‌داند، و پایداری تأمین که برای ")
          .Append("آشپزخانه‌های صنعتی حیاتی است. برند آملون هر سه را با تولید داخلی و کیفیت یکنواخت پوشش می‌دهد.</p>");

        // ── ۳. جدول مشخصات (Information Gain + Featured Snippet جدولی) ──
        sb.Append("<h2>مشخصات و ابعاد ").Append(Marker).Append("</h2>");
        sb.Append("<table><thead><tr><th>ویژگی</th><th>مقدار</th></tr></thead><tbody>");
        sb.Append("<tr><td>نام محصول</td><td>").Append(h).Append("</td></tr>");
        sb.Append("<tr><td>کد محصول (SKU)</td><td>").Append(sku).Append("</td></tr>");
        sb.Append("<tr><td>برند</td><td>آملون — نمایندگی رسمی موثقی</td></tr>");
        sb.Append("<tr><td>جنس</td><td>").Append(material).Append("</td></tr>");
        sb.Append("<tr><td>ظرفیت</td><td>").Append(capacity).Append("</td></tr>");
        sb.Append("<tr><td>ابعاد</td><td>").Append(dimensions).Append("</td></tr>");
        sb.Append("<tr><td>تعداد خانه</td><td>").Append(compartments).Append("</td></tr>");
        sb.Append("<tr><td>مایکروویو</td><td>").Append(micro).Append("</td></tr>");
        sb.Append("<tr><td>کاربرد</td><td>").Append(apps).Append("</td></tr>");
        sb.Append("<tr><td>حداقل سفارش عمده</td><td>۱۰ میلیون تومان</td></tr>");
        sb.Append("</tbody></table>");
        sb.Append("<p>جدول بالا برای هماهنگی سریع با آشپزخانه و انبار تنظیم شده است. ")
          .Append("اگر ابعاد دقیق برای بسته‌بندی نهایی یا چیدمان در کارتن اهمیت دارد، ")
          .Append("کارشناسان موثقی اندازه‌های میدانی هر مدل را در اختیار شما می‌گذارند.</p>");

        // ── ۴. ویژگی‌ها و مزایا (لیست) ──
        sb.Append("<h2>ویژگی‌ها و مزایای ").Append(Marker).Append("</h2><ul>");
        sb.Append("<li><strong>مواد اولیه گیاهی:</strong> ").Append(material)
          .Append(" به‌جای پلیمر نفتی</li>");
        sb.Append("<li><strong>کیفیت یکنواخت:</strong> تولید کنترل‌شده آملون، اختلاف بین بچ‌های تولید را کم می‌کند</li>");
        sb.Append("<li><strong>ظرفیت مشخص:</strong> ").Append(capacity)
          .Append(" برای پرس بسته‌بندی و کنترل پرشن</li>");
        sb.Append("<li><strong>بسته‌بندی بهداشتی:</strong> شرینک و کارتن برای انتقال بدون آلودگی</li>");
        sb.Append("<li><strong>تأمین مستمر:</strong> موجودی انبار برای سفارش‌های ماهانه تکرارشونده</li>");
        sb.Append("<li><strong>ضمانت اصالت:</strong> خرید از نمایندگی رسمی با درج کد ").Append(sku)
          .Append(" روی فاکتور</li>");
        sb.Append("<li><strong>پشتیبانی فنی:</strong> مشاوره رایگان انتخاب سایز و نوع بسته‌بندی</li>");
        sb.Append("</ul>");

        // ── ۵. کاربرد در صنف‌ها ──
        sb.Append("<h2>کاربرد ").Append(Marker).Append(" در رستوران، کافه و کیترینگ</h2>");
        sb.Append("<p>در رستوران‌های بیرون‌بر، مقاومت ظرف در برابر رطوبت و چربی غذای گرم تعیین‌کننده است. ")
          .Append("در کافه‌ها ظاهر و یکدستی سرویس اهمیت بیشتری دارد و در کیترینگ، سرعت چیدمان و ")
          .Append("قابلیت انبارش حجم بالا اولویت پیدا می‌کند. ").Append(sec)
          .Append(" هر سه نیاز را با یک استاندارد پوشش می‌دهد.</p>");

        sb.Append("<h3>انتخاب بر اساس نوع سرویس</h3><ul>");
        sb.Append("<li><strong>بیرون‌بر و دلیوری:</strong> اولویت به مقاومت لبه و امکان درب‌گذاری</li>");
        sb.Append("<li><strong>سرو در محل:</strong> اولویت به ظاهر، یکنواختی رنگ و ضخامت جداره</li>");
        sb.Append("<li><strong>پذیرایی مراسم:</strong> اولویت به تعداد بالا در هر کارتن و سرعت چیدمان</li>");
        sb.Append("<li><strong>تولید غذای آماده:</strong> اولویت به تطبیق با خط بسته‌بندی و پرس</li>");
        sb.Append("</ul>");

        // ── ۶. مقایسه (جدول) ──
        sb.Append("<h2>مقایسه ").Append(Marker).Append(" با ظروف پلاستیکی معمولی</h2>");
        sb.Append("<table><thead><tr><th>معیار</th><th>ظروف گیاهی آملون</th><th>پلاستیک معمولی</th></tr></thead><tbody>");
        sb.Append("<tr><td>مواد اولیه</td><td>منابع گیاهی</td><td>پلیمر پایه نفتی</td></tr>");
        sb.Append("<tr><td>تصویر برند</td><td>هم‌راستا با پیام زیست‌محیطی</td><td>حساسیت‌زا برای بخشی از مشتریان</td></tr>");
        sb.Append("<tr><td>سرو غذای گرم</td><td>رفتار حرارتی مناسب در کاربردهای رایج</td><td>محدودیت در دمای بالا</td></tr>");
        sb.Append("<tr><td>انطباق با الزامات</td><td>سازگار با روند محدودسازی پلاستیک</td><td>در معرض محدودیت آینده</td></tr>");
        sb.Append("<tr><td>قیمت واحد</td><td>بالاتر، با ارزش برندینگ</td><td>پایین‌تر</td></tr>");
        sb.Append("</tbody></table>");
        sb.Append("<p>نتیجه عملی این مقایسه: اگر مزیت رقابتی کسب‌وکار شما کیفیت و تصویر برند است، ")
          .Append("اختلاف قیمت واحد با ارزش ادراک‌شده مشتری جبران می‌شود. ")
          .Append("برای سرویس‌های حساس به قیمت، ترکیب دو دسته محصول راهکار متعادل‌تری است. ")
          .Append(E(SiteKeywordStrategy.Tertiary))
          .Append(" در گروه محصولات زیست‌تخریب‌پذیر دسته‌بندی می‌شوند و همین موضوع در ")
          .Append("مذاکره با برندهای حساس به الزامات محیط‌زیستی امتیاز محسوب می‌شود.</p>");

        // ── ۷. راهنمای انتخاب (Information Gain) ──
        sb.Append("<h2>راهنمای انتخاب سایز و بسته‌بندی</h2>");
        sb.Append("<p>سه پرسش، انتخاب را قطعی می‌کند: پرشن غذای شما چند گرم است، ظرف باید درب داشته باشد یا نه، ")
          .Append("و مصرف ماهانه شما چند هزار عدد است. پاسخ این سه، هم مدل و هم نوع بسته‌بندی ")
          .Append("(فله، شرینک یا کارتن) را مشخص می‌کند و مستقیماً روی قیمت تمام‌شده اثر دارد.</p>");
        sb.Append("<h3>تفاوت بسته‌بندی فله، شرینک و کارتن</h3><ul>");
        sb.Append("<li><strong>فله (نایلونی):</strong> کم‌هزینه‌ترین گزینه، مناسب مصرف سریع در محل</li>");
        sb.Append("<li><strong>شرینک:</strong> بهداشتی‌تر و مناسب انبارش طولانی یا توزیع بین شعبه‌ها</li>");
        sb.Append("<li><strong>کارتن:</strong> واحد استاندارد خرید عمده و بهترین گزینه برای حمل شهرستان</li>");
        sb.Append("</ul>");

        if (!transactionalFirst)
            AppendTransactionalBlocks(sb, head, sec, sku, secondary);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            sb.Append("<p>برای دیدن مدل‌های هم‌گروه و مقایسه ظرفیت‌ها، ")
              .Append("<a href=\"/Catalog?category=").Append(E(categorySlug!))
              .Append("\">سایر محصولات این دسته</a> را ببینید.</p>");
        }

        if (emphasizeUnique)
            SeoProductDifferentiation.AppendDifferentiationSection(sb, product, shortLabel, attempt);

        AppendTopicSections(sb, product, options.MissingTopics, material, capacity, sku, apps);

        if (options.Mode is SeoContentMode.Internal or SeoContentMode.Full)
            AppendInternalLinkCluster(sb, product, secondary, categorySlug);

        if (options.Mode is SeoContentMode.Aeo or SeoContentMode.Full)
        {
            AppendAeoAnswerBlocks(sb, product, options.UnansweredQueries, shortLabel, sku, material);
            AppendWholesaleQueryBlocks(sb, focus, secondary, shortLabel, sku);
        }

        // ── توسعه هدفمند تا رسیدن به حجم رقابتی (بدون تکرار) ──
        AppendExpansions(sb, product, shortLabel, secondary, material, sku, blueprint, attempt);

        return TuneKeywordDensity(sb.ToString(), focus, shortLabel);
    }

    static void AppendIntro(
        StringBuilder sb, Product product, string sku, string material, string capacity, string apps, string type, int attempt = 0)
    {
        var variant = (Math.Abs(product.Id) + attempt * 3) % 4;
        switch (variant)
        {
            case 0:
                sb.Append("<p><strong>").Append(Marker).Append("</strong> با کد ").Append(sku)
                  .Append(" و ظرفیت ").Append(capacity).Append(" برای خط سرو ")
                  .Append(apps).Append(" طراحی شده است. جنس ").Append(material)
                  .Append(" در گروه ").Append(type)
                  .Append(" قرار می‌گیرد و از مسیر نمایندگی رسمی آملون در تهران تأمین می‌شود.</p>");
                break;
            case 1:
                sb.Append("<p>اگر به دنبال ").Append(Marker)
                  .Append(" برای ").Append(apps).Append(" هستید، مدل ")
                  .Append(sku).Append(" با جنس ").Append(material)
                  .Append(" تعادل خوبی بین استحکام لبه و وزن سبک دارد. ظرفیت ")
                  .Append(capacity).Append(" برای کنترل پرس و هزینه تمام‌شده در خرید عمده مهم است.</p>");
                break;
            case 2:
                sb.Append("<p>").Append(Marker).Append(" از ")
                  .Append(material).Append(" ساخته می‌شود")
                  .Append(product.MicrowaveSafe ? " و برای گرم کردن در مایکروویو مناسب است" : "")
                  .Append(". این مدل (").Append(sku).Append(") در دسته ").Append(type)
                  .Append(" با ظرفیت ").Append(capacity)
                  .Append(" برای تیم‌های خرید عمده که به تأمین مستمر نیاز دارند عرضه می‌شود.</p>");
                break;
            default:
                sb.Append("<p>خریداران عمده ").Append(Marker)
                  .Append(" معمولاً سه معیار را چک می‌کنند: کد ").Append(sku)
                  .Append(" برای پیگیری اصالت، ظرفیت ").Append(capacity)
                  .Append(" برای تطبیق با منو، و جنس ").Append(material)
                  .Append(" برای انطباق با سیاست بسته‌بندی مجموعه. این صفحه همان سه مورد را برای تصمیم سریع جمع کرده است.</p>");
                break;
        }
    }

    static void AppendTransactionalBlocks(StringBuilder sb, string head, string sec, string sku, string secondary)
    {
        sb.Append("<h2>قیمت ").Append(Marker).Append(" چقدر است؟</h2>");
        sb.Append("<p>قیمت این محصول نرخ ثابت واحد ندارد و بر اساس نوع بسته‌بندی، تعداد کارتن و ")
          .Append("تداوم خرید تعیین می‌شود. سفارش‌های بالای حداقل ۱۰ میلیون تومان مشمول قیمت‌گذاری پله‌ای هستند و ")
          .Append("مشتریان با خرید ماهانه تکرارشونده شرایط بهتری دریافت می‌کنند. ")
          .Append("برای دریافت نرخ روز، ")
          .Append("<a href=\"/Page/Pricing\">استعلام قیمت عمده ظروف آملون</a> را ثبت کنید.</p>");

        sb.Append("<h2>مراحل سفارش و خرید عمده ").Append(Marker).Append("</h2><ol>");
        sb.Append("<li>مدل و ظرفیت مورد نیاز را از <a href=\"/Catalog\">کاتالوگ ظروف یکبار مصرف گیاهی</a> انتخاب کنید.</li>");
        sb.Append("<li>مصرف ماهانه و نوع بسته‌بندی (فله، شرینک، کارتن) را به کارشناس فروش اعلام کنید.</li>");
        sb.Append("<li>پیش‌فاکتور شامل قیمت واحد، هزینه حمل و زمان تحویل را بررسی و تأیید کنید.</li>");
        sb.Append("<li>پس از تسویه، بارگیری از انبار تهران و هماهنگی باربری انجام می‌شود.</li>");
        sb.Append("<li>تحویل در ۲ تا ۷ روز کاری بسته به مقصد انجام می‌شود.</li>");
        sb.Append("</ol>");
        sb.Append("<p>شرایط کامل همکاری، سقف اعتبار و تخفیف پله‌ای در صفحه ")
          .Append("<a href=\"/Page/Wholesale\">شرایط پخش عمده ظروف یکبار مصرف</a> توضیح داده شده است. ")
          .Append("برای شناخت بیشتر تولیدکننده، ")
          .Append("<a href=\"/Page/Amelon\">معرفی برند آملون</a> را مطالعه کنید.</p>");

        sb.Append("<h2>شرایط ارسال و حداقل سفارش</h2>");
        sb.Append("<p>ارسال به سراسر ایران انجام می‌شود و زمان تحویل معمولاً ۲ تا ۷ روز کاری بسته به مقصد است. ")
          .Append("حداقل سفارش عمده ۱۰ میلیون تومان است و می‌تواند ترکیبی از چند مدل ").Append(sec)
          .Append(" باشد. هزینه حمل بر اساس حجم، وزن و مقصد در پیش‌فاکتور اعلام می‌شود و برای سفارش‌های حجیم ")
          .Append("هماهنگی باربری توسط تیم پشتیبانی موثقی در انبار تهران انجام می‌گیرد.</p>");
    }

    static void AppendTopicSections(
        StringBuilder sb,
        Product product,
        IReadOnlyList<string> missingTopics,
        string material,
        string capacity,
        string sku,
        string apps)
    {
        if (missingTopics.Count == 0) return;

        sb.Append("<h2>جزئیات تکمیلی برای تصمیم خرید</h2>");
        foreach (var topic in missingTopics.Take(6))
        {
            var paragraph = TopicParagraph(topic, product, material, capacity, sku, apps);
            if (paragraph is null) continue;
            sb.Append("<h3>").Append(E(topic)).Append("</h3>");
            sb.Append("<p>").Append(paragraph).Append("</p>");
        }
    }

    static string? TopicParagraph(
        string topic, Product product, string material, string capacity, string sku, string apps)
    {
        return topic switch
        {
            "برند" => $"مدل {sku} از خط تولید آملون تأمین می‌شود و فروشگاه موثقی آن را به‌صورت رسمی در تهران عرضه می‌کند.",
            "جنس و مواد" => $"جنس ثبت‌شده {material} است؛ این مشخصه در کنترل کیفیت سرو غذای گرم و سرد اهمیت دارد.",
            "ویژگی‌ها" => $"ظرفیت {capacity} و ابعاد ثبت‌شده در جدول مشخصات، پایه انتخاب سایز برای منوی {apps} هستند.",
            "کاربرد" => $"در {apps}، یکنواختی ضخامت جداره و شکل لبه تعیین‌کننده سرعت چیدمان و رضایت مشتری است.",
            "قیمت" => "نرخ واحد به بسته‌بندی (فله، شرینک، کارتن) و حجم ماهانه بستگی دارد؛ استعلام روز از طریق کارشناس فروش انجام می‌شود.",
            "خرید عمده" => "خرید عمده با حداقل ۱۰ میلیون تومان امکان‌پذیر است و می‌توان چند مدل را در یک سفارش ترکیب کرد.",
            "حداقل سفارش" => "حداقل سفارش ۱۰ میلیون تومان است؛ برای رسیدن به این مبلغ می‌توان چند سایز یا نوع بسته‌بندی را با هم سفارش داد.",
            "ارسال و تحویل" => "ارسال سراسری از انبار تهران انجام می‌شود و زمان تحویل معمولاً ۲ تا ۷ روز کاری است.",
            "اصالت و نمایندگی" => $"کد {sku} روی فاکتور درج می‌شود و مسیر تأمین از نمایندگی رسمی آملون قابل پیگیری است.",
            "مقایسه" => "در مقایسه با پلاستیک معمولی، تمرکز روی مواد گیاهی، تصویر برند و انطباق با الزامات زیست‌محیطی است.",
            "مایکروویو" when product.MicrowaveSafe =>
                "این مدل برای گرم کردن در مایکروویو مناسب است؛ برای سرو بیرون‌بر بهتر است غذا کمی خنک‌تر بسته‌بندی شود.",
            "ظرفیت" when product.CapacityCc is > 0 =>
                $"ظرفیت {capacity} برای پرس‌های استاندارد مناسب است؛ اگر پرس سنگین‌تر دارید یک سایز بالاتر را بررسی کنید.",
            _ => $"برای موضوع «{topic}» در مدل {sku}، کارشناس فروش موثقی جزئیات عملیاتی را بر اساس مصرف ماهانه شما پیشنهاد می‌دهد."
        };
    }

    static void AppendInternalLinkCluster(
        StringBuilder sb, Product product, string secondary, string? categorySlug)
    {
        sb.Append("<h2>مسیرهای مرتبط برای خریدار عمده</h2><ul>");
        sb.Append("<li><a href=\"/Catalog\">کاتالوگ کامل ").Append(E(secondary)).Append("</a></li>");
        sb.Append("<li><a href=\"/Page/Wholesale\">شرایط پخش عمده و حداقل سفارش</a></li>");
        sb.Append("<li><a href=\"/Page/Pricing\">استعلام قیمت روز</a></li>");
        sb.Append("<li><a href=\"/Page/Amelon\">درباره برند آملون</a></li>");
        if (!string.IsNullOrWhiteSpace(categorySlug))
            sb.Append("<li><a href=\"/Catalog?category=").Append(E(categorySlug!))
              .Append("\">محصولات هم‌دسته</a></li>");
        sb.Append("</ul>");
        sb.Append("<p>اگر در حال مقایسه چند مدل هستید، از ")
          .Append("<a href=\"/Catalog\">کاتالوگ</a> شروع کنید و سپس برای تأیید نهایی سبد، ")
          .Append("با کارشناس فروش تماس بگیرید.</p>");
    }

    static void AppendAeoAnswerBlocks(
        StringBuilder sb,
        Product product,
        IReadOnlyList<string> unansweredQueries,
        string shortLabel,
        string sku,
        string material)
    {
        var h = E(shortLabel);
        var queries = unansweredQueries.Count > 0
            ? unansweredQueries
            :
            [
                $"آیا {shortLabel} اصل است؟",
                $"جنس {shortLabel} چیست؟",
                $"حداقل سفارش {shortLabel} چقدر است؟"
            ];

        foreach (var query in queries.Take(4))
        {
            var q = E(query.TrimEnd('؟', '?'));
            sb.Append("<h3>").Append(q).Append("؟</h3>");
            sb.Append("<p>").Append(AeoDirectAnswer(query, product, sku, material, h)).Append("</p>");
        }
    }

    static void AppendWholesaleQueryBlocks(StringBuilder sb, string focus, string secondary, string shortLabel, string sku)
    {
        var h = E(shortLabel);
        sb.Append("<h2>خرید عمده ").Append(h).Append(" از موثقی چگونه است؟</h2>");
        sb.Append("<p>خرید عمده ").Append(h)
          .Append(" از فروشگاه موثقی با حداقل سفارش ۱۰ میلیون تومان انجام می‌شود. ")
          .Append("مدل ").Append(sku)
          .Append(" را از کاتالوگ انتخاب کنید، مصرف ماهانه را به کارشناس بگویید و پیش‌فاکتور دریافت کنید. ")
          .Append("ارسال سراسری از تهران با زمان تحویل ۲ تا ۷ روز کاری انجام می‌شود.</p>");

        sb.Append("<h2>تفاوت ").Append(h).Append(" با ظروف پلاستیکی چیست؟</h2>");
        sb.Append("<p>تفاوت اصلی در مواد اولیه است: ")
          .Append(h).Append(" از منابع گیاهی ساخته می‌شود، در حالی که پلاستیک معمولی پایه نفتی دارد. ")
          .Append("این تفاوت روی تصویر برند، انطباق با الزامات زیست‌محیطی و ترجیح بخشی از مشتریان اثر می‌گذارد. ")
          .Append("در ").Append(E(secondary))
          .Append("، کیفیت یکنواخت آملون برای سرو حرفه‌ای مزیت رقابتی محسوب می‌شود.</p>");

        sb.Append("<h2>نحوه سفارش ").Append(h).Append("</h2>");
        sb.Append("<p>برای سفارش، کد ").Append(sku)
          .Append(" را از همین صفحه یادداشت کنید، تعداد کارتن و نوع بسته‌بندی را مشخص کنید و از ")
          .Append("<a href=\"/Page/Pricing\">صفحه استعلام قیمت</a> یا تماس با کارشناس فروش اقدام کنید. ")
          .Append("پس از تأیید پیش‌فاکتور و تسویه، بارگیری از انبار تهران انجام می‌شود.</p>");
    }

    static string AeoDirectAnswer(string query, Product product, string sku, string material, string shortLabel)
    {
        var norm = SeoText.Normalize(query);
        if (norm.Contains("قیمت", StringComparison.Ordinal))
            return $"قیمت {shortLabel} به بسته‌بندی و حجم سفارش بستگی دارد؛ برای کد {sku} استعلام روز از کارشناس فروش دریافت کنید.";
        if (norm.Contains("جنس", StringComparison.Ordinal))
            return $"جنس {shortLabel} {material} است و برای سرو یکبار مصرف در آشپزخانه‌های صنعتی طراحی شده است.";
        if (norm.Contains("حداقل", StringComparison.Ordinal))
            return "حداقل سفارش عمده ۱۰ میلیون تومان است و می‌توان چند محصول را در یک سفارش ترکیب کرد.";
        if (norm.Contains("اصالت", StringComparison.Ordinal) || norm.Contains("اصل", StringComparison.Ordinal))
            return $"بله؛ کد {sku} روی فاکتور درج می‌شود و تأمین از مسیر نمایندگی رسمی آملون انجام می‌گیرد.";
        if (norm.Contains("ارسال", StringComparison.Ordinal))
            return "ارسال سراسری از تهران انجام می‌شود و زمان تحویل معمولاً ۲ تا ۷ روز کاری است.";
        return $"{shortLabel} با کد {sku} از فروشگاه موثقی عرضه می‌شود؛ برای جزئیات بیشتر با کارشناس فروش تماس بگیرید.";
    }

    /// <summary>
    /// بخش‌های تکمیلی — هر بخش اطلاعات متفاوتی دارد و فقط تا رسیدن به حجم هدف اضافه می‌شود.
    /// جای حلقه قدیمی که یک پاراگراف یکسان را تکرار می‌کرد و محتوای تکراری می‌ساخت.
    /// </summary>
    static void AppendExpansions(
        StringBuilder sb,
        Product product,
        string shortLabel,
        string secondary,
        string material,
        string sku,
        IReadOnlyList<string> blueprint,
        int differentiationAttempt = 0)
    {
        var h = E(shortLabel);
        var d = SeoProductDifferentiation.Extract(product);
        var name = E(d.Name);
        var expansions = new List<string>
        {
            $"<h2>چک‌لیست سفارش {name}</h2><p>قبل از ثبت سفارش عمده، کد {sku}، " +
            $"{(d.PackPieceCount is > 0 ? $"بسته {d.PackPieceCount} قطعه‌ای" : "نوع بسته‌بندی")} و " +
            $"{(d.CapacityCc is > 0 ? $"ظرفیت {d.CapacityCc} سی‌سی" : "ظرفیت مورد نیاز")} را با منوی فعلی تطبیق دهید.</p>",

            $"<h2>سناریوی مصرف برای {name}</h2><p>این SKU برای {E(d.Applications ?? "رستوران و کیترینگ")} " +
            $"با تاکید بر {(string.IsNullOrWhiteSpace(d.SizeHint) ? "همان ابعاد ثبت‌شده" : $"سایز {E(d.SizeHint!)}")} نوشته شده — با مدل‌های دیگر فقط در نام و کد اشتباه گرفته نشود.</p>",
            "<h2>نگهداری و انبارش صحیح</h2>" +
            "<p>محصولات گیاهی به رطوبت حساس‌ترند، بنابراین انبار باید خشک و دور از تماس مستقیم آب باشد. " +
            "چیدمان کارتن‌ها روی پالت و پرهیز از فشار بیش از حد در ارتفاع، تغییر شکل لبه ظرف را کاهش می‌دهد. " +
            "رعایت ترتیب ورود و خروج موجودی، کیفیت را در سفارش‌های ماهانه یکنواخت نگه می‌دارد.</p>",

            "<h2>استانداردهای کیفیت و بهداشت</h2>" +
            $"<p>کنترل کیفیت {h} روی سه شاخص انجام می‌شود: یکنواختی ضخامت جداره، سالم بودن لبه و تمیزی سطح. " +
            $"بسته‌بندی بهداشتی مانع تماس محصول با محیط انبار می‌شود و جنس {material} " +
            "در تماس با مواد غذایی برای مصرف یکبار طراحی شده است. هر ارسال با کد محصول قابل پیگیری است.</p>",

            "<h3>آیا این مدل برای غذای گرم مناسب است؟</h3>" +
            "<p>" + (product.MicrowaveSafe
                ? "بله، این مدل قابلیت استفاده در مایکروویو را دارد و برای سرو غذای گرم در کاربردهای رایج مناسب است. "
                : "برای سرو غذای گرم در کاربردهای رایج مناسب است، اما برای گرم کردن در مایکروویو مشخصات مدل را با کارشناس بررسی کنید. ") +
            "در سرویس بیرون‌بر توصیه می‌شود غذا پیش از بسته‌بندی کمی از دمای جوش پایین‌تر بیاید " +
            "تا بخار جمع‌شده روی کیفیت ظرف و ظاهر غذا اثر نگذارد.</p>",

            "<h2>سوالات پرتکرار خریداران عمده</h2>" +
            $"<p>پرتکرارترین پرسش‌ها درباره {h} به سه موضوع برمی‌گردد: امکان تأمین مستمر برای مصرف ماهانه ثابت، " +
            "امکان ترکیب چند محصول برای رسیدن به حداقل سفارش، و زمان تحویل به شهرستان. " +
            $"پاسخ کوتاه هر سه مثبت است و جزئیات آن با کد {sku} در پیش‌فاکتور مشخص می‌شود.</p>",

            "<h2>همکاری بلندمدت با فروشگاه موثقی</h2>" +
            $"<p>برای مجموعه‌های زنجیره‌ای، تأمین {secondary} به‌صورت قرارداد دوره‌ای انجام می‌شود تا " +
            "قیمت و موجودی در بازه مشخص تثبیت شود. این مدل همکاری ریسک نوسان تأمین را کم می‌کند و " +
            "برنامه‌ریزی انبار را برای آشپزخانه مرکزی ساده‌تر می‌سازد. کارشناسان موثقی سبد خرید بهینه را پیشنهاد می‌دهند.</p>"
        };

        var offset = (Math.Abs(product.Id) + differentiationAttempt * 5) % expansions.Count;
        var ordered = expansions.Skip(offset).Concat(expansions.Take(offset)).ToList();

        foreach (var section in ordered)
        {
            if (SeoText.CountWords(SeoText.StripHtml(sb.ToString())) >= SeoContentRules.TargetWordCount)
                break;
            sb.Append(section);
        }

        // اگر Blueprint موضوع خاصی دارد که هنوز پوشش نشده، یک بخش کوتاه اضافه کن
        foreach (var step in blueprint.Where(b => b.StartsWith("پوشش موضوع:", StringComparison.Ordinal)).Take(2))
        {
            if (SeoText.CountWords(SeoText.StripHtml(sb.ToString())) >= SeoContentRules.TargetWordCount)
                break;
            var topic = step["پوشش موضوع:".Length..].Trim();
            var extra = TopicParagraph(topic, product, material,
                product.CapacityCc is > 0 ? $"{product.CapacityCc} سی‌سی" : "بر اساس مدل",
                sku,
                string.IsNullOrWhiteSpace(product.Applications) ? "رستوران و کیترینگ" : product.Applications!.Trim());
            if (extra is null) continue;
            sb.Append("<h3>").Append(E(topic)).Append("</h3><p>").Append(extra).Append("</p>");
        }
    }

    static string TuneKeywordDensity(string html, string focus, string shortLabel) =>
        SeoKeywordPlacer.EnforceDensityCap(SeoKeywordPlacer.Resolve(html, focus, shortLabel), focus, shortLabel);

    static string Fit(string text, int min, int max)
    {
        if (text.Length > max) return text[..(max - 1)].TrimEnd() + "…";
        if (text.Length >= min) return text;
        var pad = " ضمانت اصالت و مشاوره رایگان خرید عمده.";
        while (text.Length < min && text.Length + pad.Length <= max) text += pad;
        return text;
    }

    static string E(string value) => value
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}

/// <summary>سازگاری با کدهای قبلی — همه به موتور Ultimate هدایت می‌شوند.</summary>
public static class SeoContentBuilder
{
    public static string BuildLongDescription(Product product, string focus, string secondary) =>
        SeoUltimateWriter.BuildContent(product, focus, secondary, product.Category?.Slug);

    public static string BuildMetaTitle(string focus, string productName) =>
        SeoUltimateWriter.BuildMetaTitle(focus, productName);

    public static string BuildMetaDescription(Product product, string focus, string secondary) =>
        SeoUltimateWriter.BuildMetaDescription(product, focus, secondary);

    public static string BuildShortDescription(string focus) =>
        $"{SeoText.HeadPhrase(focus)} — {SiteKeywordStrategy.Primary}. " +
        $"{SiteKeywordStrategy.Tertiary} · خرید عمده از فروشگاه موثقی با ضمانت اصالت آملون.";

    public static string BuildAiSummary(Product product, string focus, string secondary) =>
        SeoUltimateWriter.BuildAiSummary(product, focus, secondary);
}
