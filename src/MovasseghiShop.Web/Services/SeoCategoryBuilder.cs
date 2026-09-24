using System.Text;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public static class SeoCategoryBuilder
{
    /// <summary>حداقل حجم محتوای صفحه دسته (کمتر از محصول، چون لیست محصولات هم محتواست).</summary>
    public const int MinWordCount = 700;
    public const int TargetWordCount = 850;

    public static (string Primary, string Secondary) SuggestKeywords(Category category)
    {
        var name = (category.Name ?? "").Trim();
        if (name.Length == 0) return (SiteKeywordStrategy.Primary, SiteKeywordStrategy.Secondary);

        var hasNature = name.Contains("گیاهی", StringComparison.OrdinalIgnoreCase)
            || name.Contains("یکبار مصرف", StringComparison.OrdinalIgnoreCase);

        var primary = hasNature ? name : $"{name} یکبار مصرف گیاهی";
        if (primary.Length > 55) primary = name;

        return (primary, SiteKeywordStrategy.Secondary);
    }

    public static string BuildMetaTitle(Category category, string focus)
    {
        var head = SeoText.HeadPhrase(focus, category.Name);
        var candidates = new[]
        {
            $"{head} | خرید عمده آملون | موثقی",
            $"{head} | قیمت عمده و انواع مدل | موثقی",
            $"خرید عمده {head} | فروشگاه موثقی",
            $"{head} | آملون | موثقی"
        };

        foreach (var c in candidates)
            if (c.Length is >= SeoContentRules.MetaTitleMin and <= SeoContentRules.MetaTitleMax)
                return c;

        var suffix = " | خرید عمده | موثقی";
        var room = SeoContentRules.MetaTitleMax - suffix.Length;
        var trimmed = head.Length > room ? head[..room].TrimEnd() : head;
        return trimmed + suffix;
    }

    public static string BuildMetaDescription(Category category, string focus, string secondary)
    {
        var head = SeoText.HeadPhrase(focus, category.Name);
        var count = category.Products?.Count ?? 0;
        var candidates = new[]
        {
            $"خرید عمده {head} از نمایندگی رسمی آملون در تهران — {count} مدل فعال. ضمانت اصالت، ارسال سراسری ۲ تا ۷ روز کاری، حداقل سفارش ۱۰ میلیون تومان و مشاوره رایگان.",
            $"{head} ({secondary}) در فروشگاه موثقی — {count} محصول آملون. ضمانت اصالت، ارسال سراسری و استعلام قیمت عمده با مشاوره رایگان کارشناس فروش.",
            $"دسته {head} با {count} مدل از برند آملون. خرید عمده با ضمانت اصالت، ارسال سراسری و حداقل سفارش ۱۰ میلیون تومان از فروشگاه موثقی تهران."
        };

        foreach (var c in candidates)
            if (c.Length is >= SeoContentRules.MetaDescMin and <= SeoContentRules.MetaDescMax)
                return c;

        var text = candidates[0];
        return text.Length > SeoContentRules.MetaDescMax
            ? text[..(SeoContentRules.MetaDescMax - 1)].TrimEnd() + "…"
            : text;
    }

    public static string BuildAiSummary(Category category, string focus, string secondary)
    {
        var count = category.Products?.Count ?? 0;
        var head = SeoText.HeadPhrase(focus, category.Name);
        return
            $"{focus} یکی از گروه‌های محصول در فروشگاه موثقی، نمایندگی رسمی آملون در تهران است. " +
            $"این دسته در حال حاضر {count} مدل فعال دارد که همه در گروه {SiteKeywordStrategy.Primary} و {secondary} قرار می‌گیرند. " +
            "کاربرد اصلی این محصولات در رستوران، فست‌فود، کافه، شرکت‌های کیترینگ و واحدهای تولید غذای آماده است. " +
            "شرایط فروش: حداقل سفارش عمده ۱۰ میلیون تومان، ارسال به سراسر ایران با زمان تحویل ۲ تا ۷ روز کاری، " +
            $"ضمانت اصالت کالا و مشاوره رایگان انتخاب مدل و بسته‌بندی. برای استعلام قیمت روز {head} " +
            "با کارشناسان فروش موثقی تماس بگیرید.";
    }

    /// <summary>
    /// محتوای صفحه دسته. نسخه قبلی یک پاراگراف یکسان را در حلقه while تکرار می‌کرد
    /// تا به حجم برسد — که محتوای تکراری و Keyword Stuffing می‌ساخت.
    /// اکنون بخش‌های متمایز فقط تا رسیدن به حجم هدف اضافه می‌شوند.
    /// </summary>
    public static string BuildDescription(Category category, string focus, string secondary)
    {
        // در بدنه متن شکل کوتاه به کار می‌رود؛ عبارت کامل فقط در جای‌نگهدارهای
        // کنترل‌شده قرار می‌گیرد تا چگالی طبیعی بماند (قبلاً به ۲٫۶٪ می‌رسید).
        var head = E(SeoText.ShortForm(focus, category.Name));
        var name = E((category.Name ?? "").Trim());
        var sec = E(secondary);
        var primary = E(SiteKeywordStrategy.Primary);
        var count = category.Products?.Count ?? 0;

        var sb = new StringBuilder();

        sb.Append("<p><strong>").Append(SeoKeywordPlacer.Marker)
          .Append("</strong> در فروشگاه موثقی، نمایندگی رسمی آملون در تهران، ")
          .Append("شامل ").Append(count).Append(" مدل فعال است. همه محصولات این دسته از منابع گیاهی تولید می‌شوند و ")
          .Append("برای سرویس‌دهی حجم بالا در رستوران، کافه و کیترینگ طراحی شده‌اند. ")
          .Append("حداقل سفارش عمده ۱۰ میلیون تومان و زمان تحویل ۲ تا ۷ روز کاری است.</p>");

        sb.Append("<h2>معرفی دسته ").Append(SeoKeywordPlacer.Marker).Append("</h2>");
        sb.Append("<p>این گروه محصول بخشی از سبد ").Append(primary)
          .Append(" است و برای کسب‌وکارهایی مناسب است که به تأمین مستمر و کیفیت یکنواخت نیاز دارند. ")
          .Append("برای دیدن همه مدل‌ها و ظرفیت‌ها، ")
          .Append("<a href=\"/Catalog\">کاتالوگ کامل ظروف یکبار مصرف گیاهی</a> را ببینید.</p>");

        sb.Append("<h2>مدل‌ها و تنوع ").Append(head).Append("</h2><ul>");
        sb.Append("<li><strong>تنوع ظرفیت:</strong> انتخاب بر اساس پرشن غذا و نوع سرویس</li>");
        sb.Append("<li><strong>تنوع بسته‌بندی:</strong> فله نایلونی، شرینک و کارتن استاندارد</li>");
        sb.Append("<li><strong>کیفیت یکنواخت:</strong> تولید کنترل‌شده آملون در همه بچ‌ها</li>");
        sb.Append("<li><strong>بسته‌بندی بهداشتی:</strong> مناسب انبارش طولانی و توزیع بین شعبه‌ها</li>");
        sb.Append("<li><strong>ضمانت اصالت:</strong> تأمین مستقیم از مسیر رسمی نمایندگی</li>");
        sb.Append("<li><strong>تأمین مستمر:</strong> موجودی انبار برای سفارش‌های ماهانه تکرارشونده</li>");
        sb.Append("</ul>");

        sb.Append("<h2>قیمت ").Append(SeoKeywordPlacer.Marker).Append(" چقدر است؟</h2>");
        sb.Append("<p>قیمت محصولات این دسته نرخ ثابت واحد ندارد و بر اساس مدل، نوع بسته‌بندی و تعداد کارتن تعیین می‌شود. ")
          .Append("سفارش‌های بالای حداقل ۱۰ میلیون تومان مشمول قیمت‌گذاری پله‌ای هستند. ")
          .Append("برای دریافت نرخ روز، ")
          .Append("<a href=\"/Page/Pricing\">استعلام قیمت عمده ظروف آملون</a> را ثبت کنید.</p>");

        sb.Append("<h2>کاربرد ").Append(head).Append(" در صنعت غذا</h2>");
        sb.Append("<p>در سرویس بیرون‌بر، مقاومت ظرف در برابر رطوبت و چربی تعیین‌کننده است؛ در کافه ظاهر و ")
          .Append("یکدستی سرویس اهمیت بیشتری دارد و در کیترینگ سرعت چیدمان و انبارش حجم بالا اولویت پیدا می‌کند. ")
          .Append(sec).Append(" هر سه نیاز را با یک استاندارد پوشش می‌دهد.</p>");

        sb.Append("<h3>انتخاب بر اساس نوع کسب‌وکار</h3><ul>");
        sb.Append("<li><strong>رستوران و دلیوری:</strong> اولویت به مقاومت لبه و امکان درب‌گذاری</li>");
        sb.Append("<li><strong>کافه:</strong> اولویت به ظاهر، یکنواختی رنگ و ضخامت جداره</li>");
        sb.Append("<li><strong>کیترینگ:</strong> اولویت به تعداد در هر کارتن و سرعت چیدمان</li>");
        sb.Append("<li><strong>تولید غذای آماده:</strong> اولویت به تطبیق با خط بسته‌بندی</li>");
        sb.Append("</ul>");

        sb.Append("<h2>مقایسه ").Append(SeoKeywordPlacer.Marker).Append(" با ظروف پلاستیکی</h2>");
        sb.Append("<table><thead><tr><th>معیار</th><th>ظروف گیاهی آملون</th><th>پلاستیک معمولی</th></tr></thead><tbody>");
        sb.Append("<tr><td>مواد اولیه</td><td>منابع گیاهی</td><td>پلیمر پایه نفتی</td></tr>");
        sb.Append("<tr><td>تصویر برند</td><td>هم‌راستا با پیام زیست‌محیطی</td><td>حساسیت‌زا برای بخشی از مشتریان</td></tr>");
        sb.Append("<tr><td>انطباق با الزامات</td><td>سازگار با روند محدودسازی پلاستیک</td><td>در معرض محدودیت آینده</td></tr>");
        sb.Append("<tr><td>قیمت واحد</td><td>بالاتر، با ارزش برندینگ</td><td>پایین‌تر</td></tr>");
        sb.Append("</tbody></table>");

        sb.Append("<h2>مراحل سفارش عمده از این دسته</h2><ol>");
        sb.Append("<li>مدل و ظرفیت مورد نیاز را از فهرست محصولات همین صفحه انتخاب کنید.</li>");
        sb.Append("<li>مصرف ماهانه و نوع بسته‌بندی را به کارشناس فروش اعلام کنید.</li>");
        sb.Append("<li>پیش‌فاکتور شامل قیمت واحد، هزینه حمل و زمان تحویل را تأیید کنید.</li>");
        sb.Append("<li>پس از تسویه، بارگیری از انبار تهران انجام می‌شود.</li>");
        sb.Append("<li>تحویل در ۲ تا ۷ روز کاری بسته به مقصد انجام می‌شود.</li>");
        sb.Append("</ol>");
        sb.Append("<p>شرایط کامل همکاری در صفحه ")
          .Append("<a href=\"/Page/Wholesale\">شرایط پخش عمده ظروف یکبار مصرف</a> آمده است و ")
          .Append("برای شناخت تولیدکننده، ")
          .Append("<a href=\"/Page/Amelon\">معرفی برند آملون</a> را مطالعه کنید.</p>");

        AppendExpansions(sb, head, sec, count);
        AppendAeoQueryBlocks(sb, category, focus, secondary);
        var shortForm = SeoText.ShortForm(focus, category.Name);
        var html = SeoKeywordPlacer.Resolve(sb.ToString(), focus, shortForm);
        return SeoKeywordPlacer.EnforceDensityCap(html, focus, shortForm);
    }

    /// <summary>بخش‌های تکمیلی متمایز — فقط تا رسیدن به حجم هدف، بدون تکرار.</summary>
    static void AppendExpansions(StringBuilder sb, string head, string secondary, int count)
    {
        var expansions = new[]
        {
            "<h2>شرایط ارسال و حداقل سفارش</h2>" +
            "<p>ارسال به سراسر ایران انجام می‌شود و زمان تحویل معمولاً ۲ تا ۷ روز کاری بسته به مقصد است. " +
            "حداقل سفارش عمده ۱۰ میلیون تومان است و می‌تواند ترکیبی از چند محصول همین دسته باشد. " +
            "هزینه حمل بر اساس حجم، وزن و مقصد در پیش‌فاکتور اعلام می‌شود.</p>",

            "<h2>نگهداری و انبارش</h2>" +
            "<p>محصولات گیاهی به رطوبت حساس‌ترند، بنابراین انبار باید خشک و دور از تماس مستقیم آب باشد. " +
            "چیدمان کارتن‌ها روی پالت و پرهیز از فشار زیاد در ارتفاع، تغییر شکل لبه ظرف را کاهش می‌دهد. " +
            "رعایت ترتیب ورود و خروج موجودی، کیفیت را در سفارش‌های ماهانه یکنواخت نگه می‌دارد.</p>",

            "<h3>آیا امکان تأمین مستمر برای مصرف ثابت ماهانه وجود دارد؟</h3>" +
            $"<p>بله. برای مجموعه‌های زنجیره‌ای، تأمین {secondary} به‌صورت قرارداد دوره‌ای انجام می‌شود " +
            "تا قیمت و موجودی در بازه مشخص تثبیت شود. این مدل همکاری ریسک نوسان تأمین را کم می‌کند و " +
            "برنامه‌ریزی انبار آشپزخانه مرکزی را ساده‌تر می‌سازد.</p>",

            "<h2>راهنمای سریع انتخاب</h2>" +
            $"<p>سه پرسش انتخاب را قطعی می‌کند: پرشن غذا چند گرم است، ظرف باید درب داشته باشد یا نه، " +
            $"و مصرف ماهانه چند هزار عدد است. پاسخ این سه، مدل مناسب از میان {count} گزینه این دسته و " +
            "نوع بسته‌بندی را مشخص می‌کند و مستقیماً روی قیمت تمام‌شده اثر دارد.</p>",

            "<h2>پشتیبانی پس از خرید</h2>" +
            "<p>پس از تحویل، در صورت مغایرت تعداد کارتن یا آسیب حمل، تیم پشتیبانی موثقی در بازه ۴۸ ساعت اول " +
            "هماهنگی جایگزینی یا اصلاح فاکتور را انجام می‌دهد. برای سفارش‌های تکرارشونده، ثبت SKU مصرفی در " +
            "سیستم انبارداری مشتری، پیش‌بینی سفارش بعدی را ساده می‌کند و از توقف خط تولید جلوگیری می‌کند.</p>",

            "<h2>انطباق با استانداردهای بهداشت</h2>" +
            "<p>بسته‌بندی بهداشتی و کنترل کیفیت بچ، برای آشپزخانه‌های صنعتی و واحدهای HACCP اهمیت دارد. " +
            "محصولات این دسته با رویکرد یکنواختی تولید عرضه می‌شوند تا در ممیزی‌های داخلی، نوسان کیفیت بین " +
            "دوره‌های مختلف تأمین ایجاد نشود.</p>"
        };

        foreach (var section in expansions)
        {
            if (SeoText.CountWords(SeoText.StripHtml(sb.ToString())) >= TargetWordCount) break;
            sb.Append(section);
        }
    }

    /// <summary>عناوین سوالی برای پوشش Query‌های AEO (همان الگوی SeoQueryExpander).</summary>
    static void AppendAeoQueryBlocks(StringBuilder sb, Category category, string focus, string secondary)
    {
        var sec = E(secondary);
        var count = category.Products?.Count ?? 0;

        sb.Append("<h3>خرید عمده از این دسته</h3>");
        sb.Append("<p>برای خرید از این دسته، مدل و ظرفیت را از فهرست محصولات انتخاب کنید و حجم ماهانه را به کارشناس ")
          .Append("فروش اعلام کنید. حداقل سفارش عمده ۱۰ میلیون تومان است و می‌تواند ترکیبی از چند SKU باشد.</p>");

        sb.Append("<h3>قیمت و استعلام عمده</h3>");
        sb.Append("<p>خرید عمده با قیمت‌گذاری پله‌ای انجام می‌شود. پس از تأیید پیش‌فاکتور، بارگیری از انبار تهران ")
          .Append("و ارسال به سراسر کشور در ۲ تا ۷ روز کاری انجام می‌شود.</p>");

        sb.Append("<h3>مشخصات فنی مدل‌ها</h3>");
        sb.Append("<p>مشخصات هر مدل شامل ظرفیت، جنس گیاهی، نوع بسته‌بندی و تعداد در کارتن است. جدول مقایسه بالا ")
          .Append("و فهرست ").Append(count).Append(" محصول فعال، انتخاب را بر اساس نوع سرویس شما ساده می‌کند.</p>");

        sb.Append("<h3>بهترین انتخاب برای چه کسب‌وکارهایی است؟</h3>");
        sb.Append("<p>برای رستوران و دلیوری مقاومت لبه مهم است؛ برای کافه یکنواختی ظاهر؛ برای کیترینگ سرعت چیدمان. ")
          .Append("بهترین انتخاب وقتی است که مدل با پرشن غذا و خط بسته‌بندی شما هم‌خوان باشد.</p>");

        sb.Append("<h3>نحوه سفارش از فروشگاه موثقی</h3>");
        sb.Append("<p>مدل را انتخاب کنید، تعداد کارتن را مشخص کنید، پیش‌فاکتور را تأیید و تسویه کنید. ")
          .Append("جزئیات در بخش «مراحل سفارش عمده» و صفحه ")
          .Append("<a href=\"/Page/Pricing\">استعلام قیمت</a> آمده است.</p>");

        sb.Append("<h3>ارسال به تهران و شهرستان</h3>");
        sb.Append("<p>نمایندگی رسمی آملون در تهران از انبار مرکزی ارسال می‌کند. برای تحویل در تهران و شهرستان، ")
          .Append("زمان تحویل و هزینه حمل در پیش‌فاکتور شفاف اعلام می‌شود.</p>");

        sb.Append("<h3>نمایندگی رسمی آملون</h3>");
        sb.Append("<p>فروشگاه موثقی نمایندگی رسمی آملون است؛ تأمین از مسیر رسمی، ضمانت اصالت و مشاوره رایگان ")
          .Append("انتخاب مدل برای ").Append(sec).Append(" در همین دسته ارائه می‌شود.</p>");
    }

    /// <summary>بازسازی توضیحات دسته با پوشش Queryهای بی‌پاسخ گزارش تحلیل.</summary>
    public static string BuildDescription(
        Category category, string focus, string secondary, IEnumerable<string>? extraQueryHeadings)
    {
        if (extraQueryHeadings is null || !extraQueryHeadings.Any())
            return BuildDescription(category, focus, secondary);

        var sb = new StringBuilder(BuildDescription(category, focus, secondary));
        foreach (var q in extraQueryHeadings.Take(4))
        {
            var title = E(q.Trim());
            if (title.Length == 0) continue;
            sb.Append("<h3>").Append(title).Append("</h3>");
            sb.Append("<p>پاسخ کوتاه: برای این سوال، مدل مناسب را از فهرست محصولات همین دسته انتخاب کنید و ")
              .Append("برای قیمت روز و موجودی با کارشناسان موثقی تماس بگیرید. حداقل سفارش عمده ۱۰ میلیون تومان است.</p>");
        }

        var shortForm = SeoText.ShortForm(focus, category.Name);
        var html = SeoKeywordPlacer.Resolve(sb.ToString(), focus, shortForm);
        return SeoKeywordPlacer.EnforceDensityCap(html, focus, shortForm);
    }

    /// <summary>AEO — سوالات دسته با پاسخ قابل استخراج.</summary>
    public static List<SeoFaq> BuildFaqs(Category category, string focus, string secondary)
    {
        var head = SeoText.HeadPhrase(focus, category.Name);
        var name = (category.Name ?? "").Trim();
        var count = category.Products?.Count ?? 0;

        return
        [
            Faq($"قیمت {head} چقدر است؟",
                $"قیمت محصولات دسته {name} بر اساس مدل، نوع بسته‌بندی و تعداد کارتن تعیین می‌شود و نرخ ثابت واحد ندارد. " +
                "سفارش‌های بالای حداقل ۱۰ میلیون تومان مشمول قیمت‌گذاری پله‌ای هستند. " +
                "برای دریافت لیست قیمت روز با کارشناسان فروش موثقی تماس بگیرید."),

            Faq($"چند مدل در دسته {name} موجود است؟",
                $"در حال حاضر {count} مدل فعال در این دسته عرضه می‌شود. " +
                "تنوع مدل‌ها بر اساس ظرفیت، تعداد خانه و نوع بسته‌بندی است. " +
                "کارشناسان موثقی در انتخاب مدل مناسب برای نوع سرویس شما مشاوره رایگان می‌دهند."),

            Faq("حداقل سفارش عمده چقدر است؟",
                "حداقل سفارش عمده در فروشگاه موثقی ۱۰ میلیون تومان است و می‌تواند ترکیبی از چند محصول باشد. " +
                "برای مشتریان با خرید ماهانه تکرارشونده، شرایط پرداخت و تخفیف پله‌ای جداگانه بررسی می‌شود. " +
                "مشاوره تعیین سبد خرید بهینه رایگان است."),

            Faq($"آیا {secondary} این دسته اصل است؟",
                "بله. فروشگاه موثقی نمایندگی رسمی آملون در تهران است و کالا از مسیر رسمی تأمین می‌شود. " +
                "کد محصول روی فاکتور درج می‌شود و امکان پیگیری اصالت وجود دارد. " +
                "خرید از نمایندگی رسمی، تأمین مستمر انبار را هم تضمین می‌کند."),

            Faq($"چگونه از دسته {name} سفارش دهم؟",
                "ابتدا مدل و ظرفیت مورد نیاز را از فهرست همین صفحه انتخاب کنید، سپس تعداد کارتن و نوع بسته‌بندی را اعلام کنید. " +
                "پس از تأیید پیش‌فاکتور و تسویه، سفارش برای ارسال آماده می‌شود. " +
                "کل فرآیند معمولاً ۲ تا ۷ روز کاری طول می‌کشد."),

            Faq($"کاربرد {head} چیست؟",
                $"{head} برای رستوران، فست‌فود، کافه، شرکت‌های کیترینگ و واحدهای تولید غذای آماده کاربرد دارد. " +
                "در سرویس بیرون‌بر و پذیرایی مراسم، سرعت آماده‌سازی و ظاهر یکدست سرویس اهمیت زیادی دارد. " +
                "همین ویژگی‌ها این گروه را در آشپزخانه‌های صنعتی پرکاربرد کرده است."),

            Faq("شرایط ارسال به شهرستان چگونه است؟",
                "ارسال به سراسر ایران انجام می‌شود و زمان تحویل معمولاً ۲ تا ۷ روز کاری بسته به مقصد است. " +
                "برای سفارش‌های حجیم، بارگیری از انبار تهران و هماهنگی باربری توسط تیم پشتیبانی انجام می‌شود. " +
                "هزینه حمل بر اساس حجم، وزن و مقصد در پیش‌فاکتور اعلام می‌شود.")
        ];
    }

    static SeoFaq Faq(string question, string answer)
    {
        var q = question.TrimEnd();
        if (!q.EndsWith('؟') && !q.EndsWith('?')) q += "؟";

        var stop = answer.IndexOf('.');
        var shortAnswer = stop > 30 && stop < answer.Length - 1 ? answer[..(stop + 1)].Trim() : answer;
        if (SeoText.CountWords(shortAnswer) < SeoContentRules.MinFaqAnswerWords) shortAnswer = answer;

        return new SeoFaq { Question = q, Answer = answer.Trim(), ShortAnswer = shortAnswer };
    }

    static string E(string v) => v
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
