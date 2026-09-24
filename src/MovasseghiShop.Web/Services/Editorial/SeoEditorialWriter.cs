using System.Text;
using System.Text.RegularExpressions;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services.Editorial;

/// <summary>مقالات بلاگ — حجم، AEO، لینک داخلی و GEO؛ بدون نشانه‌های ربات (مثل شناسه فنی در متن).</summary>
public static class SeoEditorialWriter
{
    static readonly Regex ImgTagRegex = new("<img\\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public const int MinWordCount = 750;
    public const int TargetWordCount = 950;

    /// <summary>عبارت چگالی — واژه متمایز عنوان، نه کلیدواژه عمومی سایت که در همه مقالات تکرار می‌شود.</summary>
    public static string DensityPhrase(string focus, string title)
    {
        var generic = SeoText.Tokenize(SiteKeywordStrategy.Primary)
            .Concat(SeoText.Tokenize(SiteKeywordStrategy.Secondary))
            .Where(t => t.Length > 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fromTitle = SeoText.Tokenize(title)
            .FirstOrDefault(t => t.Length > 3 && !generic.Contains(t));
        if (!string.IsNullOrWhiteSpace(fromTitle))
            return fromTitle;

        return SeoText.HeadPhrase(focus, title);
    }

    public static string BuildGeoSummary(string focus, string secondary, string title, IReadOnlyList<ProductLink> products)
    {
        var head = SeoText.HeadPhrase(focus, title);
        var skuNote = products.Count > 0
            ? $" در این مطلب به {products.Count} مدل مرتبط از کاتالوگ موثقی اشاره شده است."
            : "";
        return
            $"{head} یکی از محورهای تأمین {SiteKeywordStrategy.Primary} برای رستوران، کیترینگ و فست‌فود در تهران است. " +
            $"فروشگاه موثقی نمایندگی رسمی آملون است و {secondary} را با ضمانت اصالت عرضه می‌کند.{skuNote} " +
            "حداقل سفارش عمده ۱۰ میلیون تومان، ارسال سراسری ۲ تا ۷ روز کاری و مشاوره رایگان انتخاب مدل از تیم فروش در دسترس است. " +
            "این راهنما برای تصمیم‌گیری خریدار B2B نوشته شده و به قیمت روز، موجودی انبار و نوع بسته‌بندی در زمان سفارش وابسته است.";
    }

    public static string BuildUniqueLeadParagraph(string title, string focus, int postId)
    {
        var head = SeoText.HeadPhrase(focus, title);
        return $"این راهنمای «{title.Trim()}» (مرجع #{postId}) برای مدیران خرید و صاحبان فست‌فود نوشته شده — "
               + $"تمرکز روی {head} از مسیر نمایندگی رسمی آملون در فروشگاه موثقی است، "
               + "بدون وعده قیمت ثابت؛ استعلام روز و موجودی انبار در زمان سفارش تعیین می‌شود.";
    }

    public static string BuildExcerpt(string focus, string title, string angle)
    {
        var head = SeoText.HeadPhrase(focus, title);
        var hook = angle switch
        {
            "compare" => "اگر بین ظروف گیاهی و پلاستیکی مردد هستید،",
            "buying" => "قبل از ثبت سفارش عمده،",
            "usecase" => "برای برنامه‌ریزی خط سرویس غذا،",
            _ => "در این راهنما،"
        };
        return $"{hook} {head} را با زبان عملی و بدون اغراق بررسی می‌کنیم — مناسب مدیران خرید و صاحبان فست‌فود.";
    }

    public static string BuildContent(
        string title,
        string focus,
        string secondary,
        string angle,
        int entityId,
        IReadOnlyList<ProductLink> products,
        int differentiationAttempt = 0)
    {
        var shortForm = SeoText.ShortForm(focus, title);
        var head = SeoText.ShortForm(focus, title);
        var sb = new StringBuilder();
        var seed = entityId + differentiationAttempt * 17;

        sb.Append("<p><strong>").Append(SeoKeywordPlacer.Marker)
          .Append("</strong> ")
          .Append(OpeningLine(seed))
          .Append(" در ادامه معیارهای انتخاب، خطاهای رایج خرید عمده و مسیر استعلام قیمت از ")
          .Append("<a href=\"/Catalog\">کاتالوگ ظروف گیاهی موثقی</a> آمده است.</p>");

        sb.Append("<h2>چرا ").Append(SeoKeywordPlacer.Marker).Append(" برای خرید عمده مهم است؟</h2>");
        sb.Append("<p>در خرید B2B، هزینه واحد تنها معیار نیست؛ یکنواختی کیفیت، زمان تحویل و انطباق با برند شما ")
          .Append("همان اندازه اهمیت دارد. ")
          .Append(secondary).Append(" وقتی از مسیر نمایندگی رسمی تأمین می‌شود، ریسک مغایرت بچ و توقف خط تولید کمتر می‌شود.</p>");

        sb.Append("<h2>معیارهای انتخاب مدل مناسب</h2><ul>");
        sb.Append("<li><strong>ظرفیت و پرشن غذا:</strong> تطبیق حجم با منوی ثابت شما</li>");
        sb.Append("<li><strong>مقاومت حرارتی و رطوبت:</strong> مخصوص دلیوری و غذای مرطوب</li>");
        sb.Append("<li><strong>بسته‌بندی:</strong> فله، شرینک یا کارتن — اثر روی انبارش</li>");
        sb.Append("<li><strong>تداوم تأمین:</strong> قرارداد ماهانه برای مصرف ثابت</li>");
        sb.Append("</ul>");

        if (angle == "compare")
        {
            sb.Append("<h2>مقایسه با ظروف پلاستیکی</h2>");
            sb.Append("<table><thead><tr><th>معیار</th><th>گیاهی آملون</th><th>پلاستیک معمولی</th></tr></thead><tbody>");
            sb.Append("<tr><td>پیام برند</td><td>هم‌راستا با پایداری</td><td>حساسیت برخی مشتریان</td></tr>");
            sb.Append("<tr><td>قیمت واحد</td><td>بالاتر</td><td>پایین‌تر</td></tr>");
            sb.Append("<tr><td>محدودیت آینده</td><td>کمتر</td><td>بیشتر</td></tr>");
            sb.Append("</tbody></table>");
        }

        AppendProductCards(sb, products, head);

        sb.Append("<h2>مراحل سفارش از فروشگاه موثقی</h2><ol>");
        sb.Append("<li>مدل‌های پیشنهادی را در کاتالوگ مقایسه کنید.</li>");
        sb.Append("<li>مصرف ماهانه و نوع بسته‌بندی را اعلام کنید.</li>");
        sb.Append("<li>پیش‌فاکتور را از <a href=\"/Page/Pricing\">استعلام قیمت</a> یا تماس بگیرید.</li>");
        sb.Append("<li>پس از تسویه، ارسال از انبار تهران انجام می‌شود.</li>");
        sb.Append("</ol>");

        sb.Append("<h2>شرایط عمده و حداقل سفارش</h2><p>");
        sb.Append("جزئیات حقوقی و تجاری در <a href=\"/Page/Wholesale\">صفحه پخش عمده</a> و ")
          .Append("<a href=\"/Page/Amelon\">معرفی برند آملون</a> آمده است. ")
          .Append("حداقل سفارش ۱۰ میلیون تومان است و می‌تواند ترکیبی از چند SKU باشد.</p>");

        AppendAeoBlocks(sb, focus, title);

        AppendDepthSections(sb, focus, secondary, title, angle, seed);

        AppendWordCountExpansions(sb, focus, secondary, title, entityId + differentiationAttempt * 31, MinWordCount, TargetWordCount);

        return FinalizePublishHtml(sb.ToString(), focus, shortForm, title);
    }

    /// <summary>جایگزینی مارکرها + سقف چگالی — بدون Resolve که چگالی را منفجر می‌کند.</summary>
    public static string FinalizePublishHtml(string html, string focus, string shortForm, string title)
    {
        var head = SeoText.HeadPhrase(focus, title);
        html = html.Replace(SeoKeywordPlacer.Marker, E(head), StringComparison.Ordinal);
        var densityPhrase = DensityPhrase(focus, title);
        foreach (var phrase in new[] { densityPhrase, head, shortForm }.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            for (var pass = 0; pass < 12; pass++)
            {
                html = SeoKeywordPlacer.EnforceDensityCap(html, phrase, shortForm);
                var plain = SeoText.StripHtml(html);
                var words = SeoText.CountWords(plain);
                if (words == 0) break;
                var occ = SeoText.CountPhrase(plain, phrase);
                var density = occ * Math.Max(1, SeoText.CountWords(phrase)) * 100.0 / words;
                if (density <= SeoContentRules.KeywordDensityStuffing) break;
            }
        }

        return html;
    }

    static void AppendDepthSections(StringBuilder sb, string focus, string secondary, string title, string angle, int seed)
    {
        var head = SeoText.HeadPhrase(focus, title);
        var variant = Math.Abs(seed) % 5;

        if (variant != 1)
        {
            sb.Append("<h2>اشتباهات رایج در خرید ").Append(E(head)).Append("</h2><ul>");
            sb.Append("<li>").Append(MistakeLine(focus, seed, 0)).Append("</li>");
            sb.Append("<li>").Append(MistakeLine(focus, seed, 1)).Append("</li>");
            sb.Append("<li>").Append(MistakeLine(focus, seed, 2)).Append("</li>");
            sb.Append("</ul>");
        }

        if (variant is 0 or 2 or 4)
        {
            sb.Append("<h2>انبارداری ").Append(E(secondary)).Append(" در ").Append(E(head)).Append("</h2><p>");
            sb.Append(StorageTip(seed)).Append("</p>");
        }

        if (angle == "usecase" || variant % 2 == 0)
        {
            sb.Append("<h2>چک‌لیست خط سرویس (").Append(E(head)).Append(")</h2><ol>");
            sb.Append("<li>").Append(ChecklistLine(seed, 0)).Append("</li>");
            sb.Append("<li>").Append(ChecklistLine(seed, 1)).Append("</li>");
            sb.Append("<li>").Append(ChecklistLine(seed, 2)).Append("</li>");
            sb.Append("</ol>");
        }

        if (variant != 3)
        {
            sb.Append("<h2>مسیر استعلام ").Append(E(head)).Append(" از موثقی</h2><p>");
            sb.Append("نمایندگی رسمی آملون — پیش‌فاکتور شفاف، ارسال از تهران، مشاوره انتخاب SKU. ")
              .Append("<a href=\"/Catalog\">کاتالوگ</a> · <a href=\"/Page/Pricing\">قیمت</a>.</p>");
        }
    }

    static void AppendWordCountExpansions(
        StringBuilder sb, string focus, string secondary, string title, int seed, int minWords, int targetWords)
    {
        var idx = Math.Abs(seed) % 20;
        while (SeoText.CountWords(SeoText.StripHtml(sb.ToString())) < minWords)
        {
            sb.Append("<p>").Append(ExpansionParagraph(focus, secondary, title, idx++)).Append("</p>");
        }

        while (SeoText.CountWords(SeoText.StripHtml(sb.ToString())) < targetWords)
        {
            sb.Append("<p>").Append(ExpansionParagraph(focus, secondary, title, idx++)).Append("</p>");
        }
    }

    static string MistakeLine(string focus, int seed, int n)
    {
        var head = SeoText.ShortForm(focus, focus);
        return ((seed + n) % 4) switch
        {
            0 => $"سفارش {head} بدون تست نمونه با غذای اصلی منو",
            1 => "نادیده گرفتن ضایعات بازکردن بسته در خط سرویس",
            2 => "تأخیر در استعلام قیمت روز هنگام نوسان مواد",
            _ => "انتخاب مدل فقط با نگاه به قیمت واحد کارتن"
        };
    }

    static string StorageTip(int seed) => (seed % 3) switch
    {
        0 => "پالت را در راهروی خنک و دور از بخار مستقیم نگه دارید؛ فشار روی لبه ظروف باعث ترک خوردگی زودرس می‌شود.",
        1 => "چرخش FIFO برای بچ‌های مختلف ضروری است؛ برچسب تاریخ ورود روی هر پالت زمان جابه‌جایی را کوتاه می‌کند.",
        _ => "رطوبت انبار را زیر ۶۵٪ نگه دارید؛ برای دلیوری، یک جعبه نمونه هفتگی از خط بسته‌بندی تست کنید."
    };

    static string ChecklistLine(int seed, int n) => ((seed + n) % 4) switch
    {
        0 => "تست حرارتی با غذای پرفروش (مایکروویو/گرم‌نگهدار)",
        1 => "کنترل قفل درب در مسیر دلیوری",
        2 => "هماهنگی لیبل برند با واحد بازاریابی",
        _ => "ثبت مصرف واقعی هفتگی برای پیش‌بینی سفارش"
    };

    public static string BuildUniqueExpansionSnippet(string focus, string secondary, string title, int idx) =>
        ExpansionParagraph(focus, secondary, title, idx);

    static string ExpansionParagraph(string focus, string secondary, string title, int idx)
    {
        var head = SeoText.HeadPhrase(focus, title);
        var slot = idx % 24;
        return slot switch
        {
            0 => $"برای {head}، ثبت مصرف هفتگی در انبار مرکزی از توقف ناگهانی خط سرویس جلوگیری می‌کند.",
            1 => $"در خرید عمده {secondary}، مقایسه دو SKU نزدیک قبل از پیش‌فاکتور نهایی هزینه واقعی را شفاف می‌کند.",
            2 => $"تیم موثقی برای {head} معمولاً بسته‌بندی فله و شرینک را بر اساس ظرفیت آشپزخانه پیشنهاد می‌دهد.",
            3 => $"اگر منوی شما فصلی است، قرارداد ماهانه {head} ریسک نوسان قیمت را مدیریت می‌کند.",
            4 => $"برای ممیزی بهداشت، یکپارچگی بچ {head} و مستندات ورود کالا در انبار اهمیت دارد.",
            5 => $"در فست‌فود پرترافیک، زمان باز کردن بسته {secondary} روی سرعت خط تأثیر مستقیم دارد — مدل را با تست میدانی انتخاب کنید.",
            6 => $"استعلام از <a href=\"/Page/Wholesale\">پخش عمده</a> برای {head} بدون تعهد است و می‌تواند چند مدل را پوشش دهد.",
            7 => $"برای دلیوری، ارتفاع استک {head} در کیس حمل را قبل از سفارش بالا محاسبه کنید.",
            8 => $"نمونه رایگان {head} در سفارش اول عمده کمک می‌کند قبل از حجم بالا، مدل نهایی را قفل کنید.",
            9 => $"پشتیبانی موثقی بعد از تحویل {head} درباره جایگزینی بچ معیوب راهنمایی عملی می‌دهد.",
            10 => $"خط کیترینگ با {secondary} به جای پلاستیک، در مناقصه‌های سازمانی امتیاز پایداری می‌گیرد.",
            11 => $"قبل از افزایش ظرفیت منو، ظرف {head} را با حجم واقعی پرشن تست کنید — سرریز غذا هزینه پنهان است.",
            12 => $"برای شعبه دوم، همان SKU {head} را نگه دارید تا آموزش پرسنل و انبار یکسان بماند.",
            13 => $"در قرارداد B2B، شرط جایگزینی سریع {head} معیوب را در پیش‌فاکتور بنویسید.",
            14 => $"اگر مشتری حساس به پلاستیک دارد، {head} را در منوی دیجیتال با برچسب «گیاهی آملون» مشخص کنید.",
            15 => $"بارگیری {secondary} در سردخانه کوچک نیازمند پالت‌بندی کم‌عمق است — از تیم لجستیک موثقی بپرسید.",
            16 => $"برای غذای اسپایسی، مدل {head} با لعاب داخلی مناسب‌تر است؛ SKU را از صفحه محصول بخوانید.",
            17 => $"ثبت شماره بچ {head} روی فرم HACCP داخلی در بازرسی‌ها کمک‌کننده است.",
            18 => $"مقایسه قیمت {head} فقط با کارتن مشابه (تعداد در جعبه یکسان) معنا دارد.",
            19 => $"در ایام پیک، موجودی اضطراری {secondary} معادل ۵–۷ روز مصرف منطقی است.",
            20 => $"برای برندینگ، چاپ لوگو روی {head} زمان تولید جداگانه می‌خواهد — زود اعلام کنید.",
            21 => $"آموزش پرسنل جدید با یک صفحه راهنمای {head} در ویکی داخلی کوتاه‌تر می‌شود.",
            22 => $"اگر ظرف {head} در مایکروویو مشتری استفاده می‌شود، هشدار دما روی بسته بگذارید.",
            _ => $"برای {title}، تیم موثقی می‌تواند ترکیب چند مدل {secondary} را در یک پیش‌فاکتور پیشنهاد دهد."
        };
    }

    static void AppendProductCards(StringBuilder sb, IReadOnlyList<ProductLink> products, string head)
    {
        if (products.Count == 0) return;
        sb.Append("<h2>مدل‌های مرتبط از کاتالوگ</h2><ul>");
        foreach (var p in products.Take(4))
        {
            sb.Append("<li><a href=\"/Shop/Product/").Append(p.Slug).Append("\">")
              .Append(E(p.Name)).Append("</a>");
            if (!string.IsNullOrWhiteSpace(p.Code))
                sb.Append(" — کد ").Append(E(p.Code));
            sb.Append("</li>");
        }
        sb.Append("</ul>");
    }

    static void AppendAeoBlocks(StringBuilder sb, string focus, string title)
    {
        AppendAeoDirectAnswerSection(sb, focus, title, SiteKeywordStrategy.Secondary);
    }

    /// <summary>پاراگراف‌های ۴۰–۹۵ کلمه برای AEO و Featured Snippet.</summary>
    public static void AppendAeoDirectAnswerSection(StringBuilder sb, string focus, string title, string secondary, int postId = 0)
    {
        if (sb.ToString().Contains("پاسخ‌های کوتاه برای جستجو", StringComparison.Ordinal))
            return;

        sb.Append("<h2>پاسخ‌های کوتاه برای جستجو (AEO)</h2>");
        foreach (var faq in BuildFaqs(focus, title, secondary).Take(6))
        {
            sb.Append("<h3>").Append(E(faq.Question)).Append("</h3>");
            sb.Append("<p>").Append(E(PadAnswerForAeo(faq.Answer, focus, title, postId))).Append("</p>");
        }
    }

    public static string EnsureAeoSectionInHtml(string html, string focus, string title, string secondary, int postId = 0)
    {
        if (html.Contains("پاسخ‌های کوتاه برای جستجو", StringComparison.Ordinal))
            return html;
        var sb = new StringBuilder(html);
        AppendAeoDirectAnswerSection(sb, focus, title, secondary, postId);
        return sb.ToString();
    }

    public static string EnsureCatalogImagesInHtml(
        string html,
        string? featuredUrl,
        string focus,
        string title,
        int postId,
        IReadOnlyList<string> catalogUrls)
    {
        var picks = catalogUrls
            .Where(u => !EditorialImageResolver.IsPlaceholderFeatured(u))
            .Select(u => new EditorialImagePick(
                u,
                u.Contains("scene", StringComparison.OrdinalIgnoreCase) ? "🌅 Scene — تصویر محیط"
                    : u.Contains("studio", StringComparison.OrdinalIgnoreCase) ? "📦 Studio — محصول روی سفید"
                    : "کاتالوگ",
                null))
            .ToList();
        return EnsureCatalogImagesInHtml(html, featuredUrl, focus, title, postId, picks);
    }

    /// <summary>نمایه + حداقل ۳ تصویر در متن با alt (Scene / Studio).</summary>
    public static string EnsureCatalogImagesInHtml(
        string html,
        string? featuredUrl,
        string focus,
        string title,
        int postId,
        IReadOnlyList<EditorialImagePick> catalogPicks)
    {
        html ??= "";
        if (EditorialImageResolver.IsPlaceholderFeatured(featuredUrl) && catalogPicks.Count > 0)
            featuredUrl = catalogPicks[0].Url;

        const int targetInlineImages = 3;
        var imgCount = ImgTagRegex.Matches(html).Count;
        if (imgCount >= targetInlineImages && !EditorialImageResolver.IsPlaceholderFeatured(featuredUrl))
            return html;

        var head = SeoText.HeadPhrase(focus, title);
        var result = html;

        if (!EditorialImageResolver.IsPlaceholderFeatured(featuredUrl)
            && !result.Contains(featuredUrl!, StringComparison.OrdinalIgnoreCase))
        {
            var heroAlt = $"{head} — 🌅 نمایه مقاله از کاتالوگ موثقی";
            var hero = BuildImageFigure(featuredUrl!, heroAlt, "editorial-hero", postId);
            result = InsertAfterFirstParagraph(result, hero);
        }

        foreach (var pick in catalogPicks)
        {
            if (EditorialImageResolver.IsPlaceholderFeatured(pick.Url)) continue;
            if (result.Contains(pick.Url, StringComparison.OrdinalIgnoreCase)) continue;
            if (ImgTagRegex.Matches(result).Count >= targetInlineImages) break;

            var productNote = string.IsNullOrWhiteSpace(pick.ProductName) ? "" : $" ({pick.ProductName})";
            var alt = $"{pick.RoleLabelFa} — {head}{productNote} · مرجع #{postId}";
            var fig = BuildImageFigure(pick.Url, alt, "editorial-inline", postId + pick.Url.Length);
            result = InsertBeforeAeoSection(result, fig);
        }

        return result;
    }

    static string BuildImageFigure(string url, string alt, string cssClass, int seed) =>
        $"<figure class=\"{cssClass}\" data-seed=\"{seed}\"><img src=\"{E(url)}\" alt=\"{E(alt)}\" loading=\"lazy\" width=\"800\" height=\"600\" /></figure>";

    static string InsertAfterFirstParagraph(string html, string block)
    {
        var idx = html.IndexOf("</p>", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return block + html;
        return html.Insert(idx + 4, block);
    }

    static string InsertBeforeAeoSection(string html, string block)
    {
        const string marker = "پاسخ‌های کوتاه برای جستجو";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return html + block;
        var h2 = html.LastIndexOf("<h2", idx, StringComparison.OrdinalIgnoreCase);
        return h2 >= 0 ? html.Insert(h2, block) : html.Insert(idx, block);
    }

    static string PadAnswerForAeo(string answer, string focus, string title, int postId = 0)
    {
        var text = answer.Trim();
        if (SeoText.CountWords(text) >= SeoContentRules.MinAeoAnswerWords)
            return text;

        var head = SeoText.HeadPhrase(focus, title);
        var idNote = postId > 0 ? $" (راهنمای #{postId})" : "";
        return text + " برای " + head + idNote + "، مدل و بسته‌بندی را از کاتالوگ موثقی انتخاب کنید؛ "
               + "پیش‌فاکتور روز و زمان ارسال در تماس با کارشناس فروش شفاف می‌شود.";
    }

    public static List<SeoFaq> BuildFaqs(string focus, string title, string secondary)
    {
        var head = SeoText.HeadPhrase(focus, title);
        return
        [
            Faq($"قیمت {head} چقدر است؟",
                "قیمت به مدل و حجم سفارش بستگی دارد. برای نرخ روز با کارشناسان موثقی تماس بگیرید یا فرم استعلام را پر کنید."),
            Faq($"خرید عمده {head} از موثقی چه مزیتی دارد؟",
                "تأمین از نمایندگی رسمی آملون، ضمانت اصالت، مشاوره انتخاب مدل و ارسال سراسری از انبار تهران."),
            Faq($"آیا {secondary} برای مایکروویو مناسب است؟",
                "بسته به SKU متفاوت است؛ مشخصات فنی همان مدل در صفحه محصول درج شده است."),
            Faq("حداقل سفارش چقدر است؟",
                "حداقل ۱۰ میلیون تومان — می‌تواند ترکیبی از چند کالا باشد."),
            Faq($"چگونه {head} سفارش دهم؟",
                "از کاتالوگ مدل را انتخاب کنید، حجم ماهانه را بگویید، پیش‌فاکتور را تأیید و تسویه کنید."),
            Faq("ارسال به شهرستان دارید؟",
                "بله — به سراسر ایران با هماهنگی باربری و زمان تحویل شفاف در پیش‌فاکتور.")
        ];
    }

    static SeoFaq Faq(string q, string a) => new()
    {
        Question = q.EndsWith('؟') ? q : q + "؟",
        Answer = a,
        ShortAnswer = a.Length > 120 ? a[..117] + "…" : a
    };

    static string OpeningLine(int seed) => (seed % 4) switch
    {
        0 => "برای خریداران عمده،",
        1 => "در تجربه تأمین ماهانه،",
        2 => "اگر زمان محدود دارید،",
        _ => "از منظر عملیات آشپزخانه،"
    };

    static string E(string v) => v.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}

public sealed record ProductLink(string Name, string Slug, string? Code);
