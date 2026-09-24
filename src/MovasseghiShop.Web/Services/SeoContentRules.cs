using System.Text.RegularExpressions;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

/// <summary>قوانین سخت‌گیرانه SEO + AEO + GEO — هدف رتبه ۱.</summary>
public static class SeoContentRules
{
    /* ── محتوا ── */
    public const int MinWordCount = 1000;
    public const int TargetWordCount = 1250;
    public const int MinParagraphCount = 8;
    public const int MinH2Count = 5;
    public const int MinH2WithKeyword = 3;
    public const int MinH3Count = 2;
    public const int MinListItems = 6;
    public const int MinInternalLinks = 3;
    public const int MinDistinctInternalTargets = 3;
    public const int MinQuestionHeadings = 2;

    /* ── Meta ── */
    public const int MetaTitleMin = 30;
    public const int MetaTitleMax = 60;
    public const int MetaDescMin = 140;
    public const int MetaDescMax = 160;

    /* ── Keyword ── */
    public const double KeywordDensityMin = 0.8;
    public const double KeywordDensityMax = 2.2;
    /// <summary>سقف مطلق — بالاتر از این Keyword Stuffing است و جریمه دارد.</summary>
    public const double KeywordDensityStuffing = 3.0;
    public const int IntroKeywordMaxChars = 320;
    public const int MinSecondaryMentions = 2;

    /* ── AEO (Answer Engine Optimization) ── */
    public const int MinFaqCount = 6;
    /// <summary>حداقل طول پاسخ FAQ به «کاراکتر».</summary>
    public const int MinFaqAnswerChars = 120;
    /// <summary>حداقل طول پاسخ FAQ به «کلمه» — قابل استخراج برای Featured Snippet.</summary>
    public const int MinFaqAnswerWords = 20;
    /// <summary>سقف پاسخ FAQ — پاسخ بلندتر از این برای Snippet بریده می‌شود.</summary>
    public const int MaxFaqAnswerWords = 70;
    public const int MinDirectAnswers = 3;
    public const int MinAeoAnswerWords = 40;
    public const int MaxAeoAnswerWords = 95;

    /* ── GEO (Generative Engine Optimization) ── */
    public const int MinGeoSummaryLength = 320;
    public const int MinGeoEntityMentions = 5;

    /* ── Media ── */
    public const int MinProductImages = 2;
    public const int MinAltChars = 25;
    public const int MaxAltChars = 125;

    /* ── کیفیت محتوا ── */
    /// <summary>سقف ریسک محتوای کلیشه‌ای AI.</summary>
    public const double MaxGenericRisk = 12.0;
    /// <summary>سقف ریسک تکرار پاراگراف.</summary>
    public const double MaxDuplicationRisk = 15.0;

    /* ── Publish gate — حداقل هر بُعد (واقع‌بینانه برای فعال‌سازی فروشگاه) ── */
    public const int PublishThreshold = 85;
    public const int MinOnPageScore = 85;
    public const int MinContentScore = 80;
    public const int MinAeoScore = 80;
    public const int MinGeoScore = 80;
    public const int MinImageScore = 80;
    public const int MinSemanticScore = 80;
    public const int MinIntentScore = 80;
    public const int MinTechnicalScore = 85;
    public const int MinProductDataScore = 85;
    public const int MinSchemaScore = 80;
    /// <summary>حتی با نمره کامل، زیر این عدد ادعای «آماده رتبه ۱» نمی‌کنیم.</summary>
    public const int MinCompetitiveStrength = 72;
}

public sealed class SeoContentAnalysis
{
    public int WordCount { get; init; }
    /// <summary>چگالی عبارت کلیدی (occurrences × طول عبارت ÷ کل کلمات).</summary>
    public double FocusDensity { get; init; }
    public int FocusOccurrences { get; init; }
    public int H2Count { get; init; }
    public int H2WithFocus { get; init; }
    public int H3Count { get; init; }
    public int ParagraphCount { get; init; }
    public int InternalLinkCount { get; init; }
    public int DistinctInternalTargets { get; init; }
    public int DistinctAnchorCount { get; init; }
    public int ExternalLinkCount { get; init; }
    public int ListItemCount { get; init; }
    public int QuestionHeadingCount { get; init; }
    public int DirectAnswerCount { get; init; }
    public int SecondaryMentionCount { get; init; }
    public bool HasTable { get; init; }
    public bool HasSpecTable { get; init; }
    public bool HasStepList { get; init; }
    public bool HasComparisonSection { get; init; }
    public bool FocusInIntro { get; init; }
    public bool FocusInFirstH2 { get; init; }
    public double DuplicationRisk { get; init; }
    public double GenericRisk { get; init; }
    public double AvgParagraphWords { get; init; }
    public IReadOnlyList<string> H2Texts { get; init; } = [];
    public IReadOnlyList<string> H3Texts { get; init; } = [];
    public IReadOnlyList<string> ParagraphTexts { get; init; } = [];
    public string PlainText { get; init; } = "";

    public bool MeetsWordCount => WordCount >= SeoContentRules.MinWordCount;

    /// <summary>بدون Focus Keyword، چگالی معنا ندارد و نباید تخلف شمرده شود.</summary>
    public bool MeetsDensity => FocusOccurrences == 0 && FocusDensity == 0
        ? false
        : FocusDensity is >= SeoContentRules.KeywordDensityMin and <= SeoContentRules.KeywordDensityMax;

    public bool IsStuffed => FocusDensity > SeoContentRules.KeywordDensityStuffing;

    public bool MeetsStructure => H2Count >= SeoContentRules.MinH2Count
        && H2WithFocus >= SeoContentRules.MinH2WithKeyword
        && H3Count >= SeoContentRules.MinH3Count;

    public bool MeetsLinks => InternalLinkCount >= SeoContentRules.MinInternalLinks
        && DistinctInternalTargets >= SeoContentRules.MinDistinctInternalTargets;

    public bool MeetsLists => ListItemCount >= SeoContentRules.MinListItems;
    public bool MeetsParagraphs => ParagraphCount >= SeoContentRules.MinParagraphCount;
    public bool MeetsQuestionHeadings => QuestionHeadingCount >= SeoContentRules.MinQuestionHeadings;
    public bool MeetsDirectAnswers => DirectAnswerCount >= SeoContentRules.MinDirectAnswers;
    public bool MeetsSecondary => SecondaryMentionCount >= SeoContentRules.MinSecondaryMentions;
    public bool MeetsOriginality => GenericRisk <= SeoContentRules.MaxGenericRisk
        && DuplicationRisk <= SeoContentRules.MaxDuplicationRisk;

    public bool MeetsContentRules =>
        MeetsWordCount && MeetsDensity && !IsStuffed && FocusInIntro && MeetsStructure
        && MeetsLinks && MeetsLists && MeetsParagraphs && MeetsQuestionHeadings
        && MeetsDirectAnswers && MeetsSecondary && MeetsOriginality
        && HasSpecTable && HasComparisonSection && HasStepList;
}

public sealed class SeoMediaAnalysis
{
    public bool HasScene { get; init; }
    public bool HasStudio { get; init; }
    public int ImageCount { get; init; }
    public bool AllAltFilled { get; init; }
    /// <summary>Alt همه تصاویر به‌اندازه کافی به Keyword مرتبط است (بدون Stuffing).</summary>
    public bool AllAltWithFocus { get; init; }
    /// <summary>Alt توصیفی و در بازه طول مناسب است.</summary>
    public bool AllAltDescriptive { get; init; }
    /// <summary>Alt یکسان روی چند تصویر — ضدالگوی Image SEO.</summary>
    public bool HasDuplicateAlt { get; init; }
    public bool HasPrimary { get; init; }
    public bool ImagesLocal { get; init; }

    public bool MeetsYoastMedia =>
        HasScene && HasStudio && AllAltFilled && AllAltWithFocus
        && AllAltDescriptive && !HasDuplicateAlt && HasPrimary;
}

public static class SeoProductDataHeuristics
{
    public static void ApplyDefaults(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Material))
            product.Material = "گیاهی آملون";

        if (product.CapacityCc is not > 0)
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                product.Name ?? "", @"(\d{2,4})\s*سی\s*سی", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var cc))
                product.CapacityCc = cc;
        }

        if (string.IsNullOrWhiteSpace(product.Dimensions) && product.CapacityCc is > 0)
            product.Dimensions = $"{product.CapacityCc} سی‌سی";
    }
}

public static class SeoMediaAnalyzer
{
    /// <summary>اگر اپراتور نقش نزده، اولین تصویر Studio و دومین Scene می‌شود.</summary>
    public static void EnsureImageRoles(Product product)
    {
        ProductImageHelper.ApplyGalleryRolesFromUrls(product);

        var images = product.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToList();
        if (images.Count == 0) return;

        if (images.Count == 1)
        {
            var only = images[0];
            if (only.Role is not (ProductImageRole.Scene or ProductImageRole.Studio))
            {
                if (ProductImageHelper.UrlIsScene(only.Url))
                {
                    only.Role = ProductImageRole.Scene;
                    only.IsPrimary = false;
                }
                else
                {
                    only.Role = ProductImageRole.Studio;
                    only.IsPrimary = true;
                }
            }
            return;
        }

        var hasStudio = images.Any(i => i.Role == ProductImageRole.Studio);
        var hasScene = images.Any(i => i.Role == ProductImageRole.Scene)
                       || images.Any(i => ProductImageHelper.UrlIsScene(i.Url));
        if (!hasStudio)
        {
            var studioCandidate = images.FirstOrDefault(i => ProductImageHelper.UrlIsStudio(i.Url))
                                  ?? images.FirstOrDefault(i => !ProductImageHelper.UrlIsScene(i.Url))
                                  ?? images[0];
            studioCandidate.Role = ProductImageRole.Studio;
            studioCandidate.IsPrimary = true;
        }
        if (!hasScene)
        {
            var sceneCandidate = images.FirstOrDefault(i => ProductImageHelper.UrlIsScene(i.Url))
                                 ?? images.FirstOrDefault(i => i.Role != ProductImageRole.Studio);
            if (sceneCandidate != null)
            {
                sceneCandidate.Role = ProductImageRole.Scene;
                sceneCandidate.SortOrder = 0;
                sceneCandidate.IsPrimary = false;
            }
        }
    }

    public static SeoMediaAnalysis Analyze(Product product, string focus)
    {
        var images = product.Images.ToList();
        var alts = images.Select(i => (i.AltText ?? "").Trim()).ToList();

        var allAlt = images.Count > 0 && alts.All(a => a.Length > 0);
        var allFocus = images.Count > 0 && !string.IsNullOrWhiteSpace(focus)
            && alts.All(a => SeoText.IsKeywordish(a, focus));
        var allDescriptive = images.Count > 0
            && alts.All(a => a.Length is >= SeoContentRules.MinAltChars and <= SeoContentRules.MaxAltChars);
        var dupAlt = alts.Where(a => a.Length > 0)
            .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Count() > 1);

        return new SeoMediaAnalysis
        {
            HasScene = images.Any(i => i.Role == Models.Enums.ProductImageRole.Scene)
                       || images.Any(i => ProductImageHelper.UrlIsScene(i.Url)),
            HasStudio = images.Any(i => i.Role == Models.Enums.ProductImageRole.Studio)
                        || images.Any(i => ProductImageHelper.UrlIsStudio(i.Url)),
            ImageCount = images.Count,
            AllAltFilled = allAlt,
            AllAltWithFocus = allFocus,
            AllAltDescriptive = allDescriptive,
            HasDuplicateAlt = dupAlt,
            HasPrimary = images.Any(i => i.IsPrimary),
            ImagesLocal = images.Count > 0 && images.All(i =>
                i.Url.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                || i.Url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        };
    }

    /// <summary>
    /// Alt توصیفی و یکتا برای هر تصویر — تصویر را توصیف می‌کند، نه اینکه Keyword را تکرار کند.
    /// </summary>
    public static void ApplySeoAltTexts(Product product, string focus, string secondary)
    {
        var head = SeoText.HeadPhrase(focus, product.Name);
        var material = string.IsNullOrWhiteSpace(product.Material) ? "گیاهی" : product.Material.Trim();
        var size = product.CapacityCc is > 0 ? $"{product.CapacityCc} سی‌سی"
            : !string.IsNullOrWhiteSpace(product.Dimensions) ? product.Dimensions!.Trim()
            : "";

        var sceneIndex = 0;
        var studioIndex = 0;
        var otherIndex = 0;

        foreach (var img in product.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.Id))
        {
            string alt;
            switch (img.Role)
            {
                case Models.Enums.ProductImageRole.Scene:
                    alt = sceneIndex++ switch
                    {
                        0 => $"{head} {material} روی میز سرو رستوران",
                        1 => $"چیدمان {head} در پذیرایی کیترینگ",
                        _ => $"{head} در محیط واقعی استفاده — تصویر {sceneIndex}"
                    };
                    break;
                case Models.Enums.ProductImageRole.Studio:
                    alt = studioIndex++ switch
                    {
                        0 => string.IsNullOrEmpty(size)
                            ? $"{head} {material} روی پس‌زمینه سفید"
                            : $"{head} {material} {size} روی پس‌زمینه سفید",
                        1 => $"نمای نزدیک جداره و لبه {head}",
                        _ => $"{head} — تصویر استودیویی {studioIndex}"
                    };
                    break;
                default:
                    alt = $"{head} — {secondary} تصویر {++otherIndex}";
                    break;
            }

            if (alt.Length > SeoContentRules.MaxAltChars)
                alt = alt[..SeoContentRules.MaxAltChars].TrimEnd();
            if (alt.Length < SeoContentRules.MinAltChars)
                alt = $"{alt} — {SiteKeywordStrategy.Tertiary}";

            img.AltText = alt;
        }

        var studio = product.Images.FirstOrDefault(i => i.Role == Models.Enums.ProductImageRole.Studio);
        if (studio != null)
        {
            foreach (var img in product.Images) img.IsPrimary = false;
            studio.IsPrimary = true;
        }
        else if (product.Images.Count > 0 && !product.Images.Any(i => i.IsPrimary))
        {
            product.Images.First().IsPrimary = true;
        }
    }

    /// <summary>
    /// تصویر Scene در بدنه مقاله با Alt متفاوت از Alt گالری — برای SEO تصویر و محتوا.
    /// </summary>
    public static string EnsureSceneFigureInProse(Product product, string focus, string prose)
    {
        if (string.IsNullOrWhiteSpace(prose)) return prose;

        var scene = product.Images
            .Where(i => i.Role == ProductImageRole.Scene)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .FirstOrDefault();
        if (scene is null || string.IsNullOrWhiteSpace(scene.Url))
            return prose;

        var path = scene.Url.Trim();
        if (!path.StartsWith('/')) path = "/" + path;
        if (prose.Contains(path, StringComparison.OrdinalIgnoreCase))
            return prose;

        var galleryAlt = (scene.AltText ?? "").Trim();
        var articleAlt = BuildInArticleSceneAlt(product, focus, galleryAlt);
        var encUrl = System.Net.WebUtility.HtmlEncode(path);
        var encAlt = System.Net.WebUtility.HtmlEncode(articleAlt);
        var figure =
            $"<p class=\"product-scene-embed\"><img src=\"{encUrl}\" alt=\"{encAlt}\" loading=\"lazy\" decoding=\"async\" /></p>";

        var insertAt = prose.IndexOf("<h2>", StringComparison.OrdinalIgnoreCase);
        if (insertAt < 0) insertAt = prose.IndexOf("</p>", StringComparison.OrdinalIgnoreCase);
        if (insertAt < 0) return figure + prose;

        if (prose.AsSpan(insertAt).StartsWith("<h2>", StringComparison.OrdinalIgnoreCase))
            return prose.Insert(insertAt, figure);

        var closeP = prose.IndexOf("</p>", StringComparison.OrdinalIgnoreCase);
        if (closeP < 0) return prose + figure;
        return prose.Insert(closeP + 4, figure);
    }

    static string BuildInArticleSceneAlt(Product product, string focus, string galleryAlt)
    {
        var head = SeoText.HeadPhrase(focus, product.Name);
        var material = string.IsNullOrWhiteSpace(product.Material) ? "گیاهی" : product.Material.Trim();
        var alt = $"{head} {material} در حال استفاده در فست‌فود و کافه — نمای کاربردی";
        if (string.Equals(alt.Trim(), galleryAlt, StringComparison.OrdinalIgnoreCase))
            alt = $"سرو روزانه {head} در محیط رستورانی — تصویر محتوا";
        if (alt.Length > SeoContentRules.MaxAltChars)
            alt = alt[..SeoContentRules.MaxAltChars].TrimEnd();
        if (alt.Length < SeoContentRules.MinAltChars)
            alt = $"{alt} — {SiteKeywordStrategy.Tertiary}";
        return alt;
    }
}

/// <summary>ابزارهای متنی مشترک — توکن‌سازی فارسی، تطبیق نرم Keyword، شباهت.</summary>
public static partial class SeoText
{
    static readonly char[] Separators = ['—', '–', '-', '|', '،', ',', ':', '·'];

    public static string StripHtml(string? html) =>
        string.IsNullOrWhiteSpace(html) ? "" : HtmlTagRegex().Replace(html, " ").Trim();

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return text
            .Replace('\u064a', '\u06cc')   // ي عربی → ی فارسی
            .Replace('\u0643', '\u06a9')   // ك عربی → ک فارسی
            .Replace("\u200c", " ")        // نیم‌فاصله → فاصله
            .Replace('\u0623', '\u0627')
            .Replace('\u0625', '\u0627')
            .Replace('\u0622', '\u0627')
            .ToLowerInvariant();
    }

    public static List<string> Tokenize(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : WordRegex().Matches(Normalize(text)).Select(m => m.Value).ToList();

    public static int CountWords(string? text) =>
        string.IsNullOrWhiteSpace(text) ? 0 : WordRegex().Matches(text).Count;

    public static int CountPhrase(string? text, string? phrase)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase)) return 0;
        var haystack = Normalize(text);
        var needle = Normalize(phrase);
        if (needle.Length == 0) return 0;

        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }

    public static bool Contains(string? text, string? phrase) => CountPhrase(text, phrase) > 0;

    /// <summary>
    /// تطبیق نرم — عبارت‌های کلیدی بلند فارسی هرگز کلمه‌به‌کلمه در H2 نمی‌آیند،
    /// پس پوشش ≥۶۰٪ توکن‌های معنادار را «حضور Keyword» می‌شماریم.
    /// </summary>
    public static bool IsKeywordish(string? text, string? phrase, double threshold = 0.6)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase)) return false;
        if (Contains(text, phrase)) return true;

        var need = Tokenize(phrase).Where(t => t.Length > 2).Distinct().ToList();
        if (need.Count == 0) return false;
        var have = Tokenize(text).ToHashSet();
        var hits = need.Count(have.Contains);
        return hits >= Math.Max(1, (int)Math.Ceiling(need.Count * threshold));
    }

    /// <summary>شباهت Jaccard توکنی — برای تشخیص پاراگراف تکراری.</summary>
    public static double Similarity(string a, string b)
    {
        var ta = Tokenize(a).ToHashSet();
        var tb = Tokenize(b).ToHashSet();
        if (ta.Count == 0 || tb.Count == 0) return 0;
        var union = ta.Count + tb.Count - ta.Intersect(tb).Count();
        return union == 0 ? 0 : ta.Intersect(tb).Count() * 1.0 / union;
    }

    /// <summary>
    /// «سرِ» عبارت کلیدی — قبل از هر جداکننده. برای Alt، H2 و anchor طبیعی لازم است
    /// تا عبارت‌های ترکیبی مثل «کاسه گیاهی — ظروف یکبار مصرف آملون» خرد شوند.
    /// </summary>
    public static string HeadPhrase(string? focus, string? fallback = null)
    {
        var src = !string.IsNullOrWhiteSpace(focus) ? focus! : (fallback ?? "");
        var head = src.Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? src;
        head = head.Trim();
        return head.Length >= 3 ? head : (fallback ?? src).Trim();
    }

    static readonly string[] BrandTokens = ["آملون", "amelon", "موثقی"];
    static readonly string[] UnitPrefixes = ["سی", "میلی", "لیتر", "سانتی", "گرم", "cc", "ml"];
    /// <summary>واژه‌های عمومی که حذفشان معنای عبارت را از بین نمی‌برد.</summary>
    static readonly string[] GenericQualifiers = ["یکبار", "مصرف", "ظروف", "پذیر"];
    /// <summary>صفت‌ها — اگر تنها بازمانده عبارت باشند، عبارت بی‌معنا می‌شود.</summary>
    static readonly string[] AdjectiveTokens = ["گیاهی", "نشاسته", "زیست", "تخریب", "مایکروویوی"];

    /// <summary>
    /// شکل کوتاه و طبیعی عبارت کلیدی برای استفاده در بدنه متن.
    /// خروجی تضمیناً عبارت کامل را در خود ندارد، بنابراین در محاسبه چگالی شمرده نمی‌شود؛
    /// همین باعث می‌شود بتوان بدون Keyword Stuffing درباره محصول زیاد نوشت.
    /// </summary>
    public static string ShortForm(string? phrase, string? fallback = null)
    {
        var src = HeadPhrase(phrase, fallback);
        if (string.IsNullOrWhiteSpace(src)) return "";

        var words = src.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !DigitRegex().IsMatch(w))
            .Where(w => !BrandTokens.Any(b => w.Contains(b, StringComparison.OrdinalIgnoreCase)))
            .Where(w => !UnitPrefixes.Any(u => Normalize(w).StartsWith(Normalize(u), StringComparison.Ordinal)))
            .ToList();

        if (words.Count == 0) return src;

        var candidate = string.Join(' ', words);
        if (!Contains(candidate, phrase)) return candidate.Trim();
        if (words.Count == 1) return candidate.Trim();

        // هیچ توکنی حذف نشد (مثل «کاسه یکبار مصرف») — واژه‌های عمومی را برمی‌داریم
        var trimmed = words
            .Where(w => !GenericQualifiers.Any(g =>
                Normalize(w).Equals(Normalize(g), StringComparison.Ordinal)))
            .ToList();

        var hasMeaningfulNoun = trimmed.Any(w =>
            w.Length >= 3 && !AdjectiveTokens.Any(a =>
                Normalize(w).Equals(Normalize(a), StringComparison.Ordinal)));

        if (trimmed.Count > 0 && hasMeaningfulNoun)
            return string.Join(' ', trimmed).Trim();

        // در غیر این صورت فقط آخرین واژه حذف می‌شود («ظروف یکبار مصرف گیاهی» → «ظروف یکبار مصرف»)
        return string.Join(' ', words.Take(words.Count - 1)).Trim();
    }

    [GeneratedRegex(@"[0-9\u06f0-\u06f9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.Compiled)]
    private static partial Regex WordRegex();
}

public static partial class SeoContentAnalyzer
{
    /// <summary>آستانه ارتباط موضوعی عنوان با عبارت کلیدی.</summary>
    public const double HeadingRelevance = 0.45;

    public static SeoContentAnalysis Analyze(string? html, string focus, string secondary)
    {
        html ??= "";
        var plain = SeoText.StripHtml(html);
        var words = SeoText.CountWords(plain);

        // چگالی صحیح: تعداد تکرار × طول عبارت ÷ کل کلمات (روش Yoast)
        var occurrences = SeoText.CountPhrase(plain, focus);
        var phraseWords = Math.Max(1, SeoText.CountWords(focus));
        var density = words > 0 && !string.IsNullOrWhiteSpace(focus)
            ? occurrences * phraseWords * 100.0 / words
            : 0;

        var h2Texts = H2Regex().Matches(html).Select(m => SeoText.StripHtml(m.Value)).ToList();
        var h3Texts = H3Regex().Matches(html).Select(m => SeoText.StripHtml(m.Value)).ToList();

        // عنوان‌ها کوتاه‌اند؛ اگر بخواهیم عبارت کلیدی کامل در H2 بیاید،
        // برای رسیدن به حد نصاب مجبور به Keyword Stuffing می‌شویم.
        // پس «مرتبط بودن موضوعی» عنوان معیار است، نه تکرار عین عبارت.
        var h2WithFocus = h2Texts.Count(t => SeoText.IsKeywordish(t, focus, HeadingRelevance));
        var questionHeadings = h2Texts.Concat(h3Texts).Count(IsQuestion);

        var introLen = Math.Min(plain.Length, SeoContentRules.IntroKeywordMaxChars);
        var intro = plain.Length > 0 ? plain[..introLen] : "";

        var paragraphTexts = PRegex().Matches(html)
            .Select(m => SeoText.StripHtml(m.Value))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        var internalHrefs = InternalLinkRegex().Matches(html)
            .Select(m => m.Groups[1].Value.Trim())
            .Where(h => h.Length > 1)
            .ToList();
        var anchors = AnchorRegex().Matches(html)
            .Select(m => SeoText.Normalize(SeoText.StripHtml(m.Groups[1].Value)))
            .Where(a => a.Length > 1)
            .ToList();

        return new SeoContentAnalysis
        {
            WordCount = words,
            FocusDensity = Math.Round(density, 2),
            FocusOccurrences = occurrences,
            H2Count = h2Texts.Count,
            H2WithFocus = h2WithFocus,
            H3Count = h3Texts.Count,
            ParagraphCount = paragraphTexts.Count,
            InternalLinkCount = internalHrefs.Count,
            DistinctInternalTargets = internalHrefs.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            DistinctAnchorCount = anchors.Distinct().Count(),
            ExternalLinkCount = ExternalLinkRegex().Matches(html).Count,
            ListItemCount = LiRegex().Matches(html).Count,
            QuestionHeadingCount = questionHeadings,
            DirectAnswerCount = CountDirectAnswerBlocks(paragraphTexts, focus),
            SecondaryMentionCount = SeoText.CountPhrase(plain, secondary),
            HasTable = TableRegex().IsMatch(html),
            HasSpecTable = TableRegex().IsMatch(html)
                && SeoText.Contains(plain, "مشخصات"),
            HasStepList = OlRegex().IsMatch(html),
            HasComparisonSection = h2Texts.Any(t =>
                    SeoText.Contains(t, "مقایسه") || SeoText.Contains(t, "تفاوت") || SeoText.Contains(t, "در مقابل"))
                || (TableRegex().IsMatch(html) && SeoText.Contains(plain, "مقایسه")),
            FocusInIntro = SeoText.Contains(intro, focus) || SeoText.IsKeywordish(intro, focus, 0.75),
            FocusInFirstH2 = h2Texts.Count > 0 && SeoText.IsKeywordish(h2Texts[0], focus),
            DuplicationRisk = ComputeDuplicationRisk(paragraphTexts),
            GenericRisk = SeoGenericDetector.Score(paragraphTexts),
            AvgParagraphWords = paragraphTexts.Count == 0
                ? 0
                : Math.Round(paragraphTexts.Average(p => SeoText.CountWords(p)), 1),
            H2Texts = h2Texts,
            H3Texts = h3Texts,
            ParagraphTexts = paragraphTexts,
            PlainText = plain
        };
    }

    public static bool IsQuestion(string text) =>
        text.Contains('؟') || text.Contains('?')
        || text.StartsWith("چگونه", StringComparison.Ordinal)
        || text.StartsWith("چرا", StringComparison.Ordinal)
        || text.StartsWith("آیا", StringComparison.Ordinal);

    /// <summary>درصد پاراگراف‌هایی که تقریباً کپی پاراگراف دیگری هستند.</summary>
    static double ComputeDuplicationRisk(List<string> paragraphs)
    {
        var meaningful = paragraphs.Where(p => SeoText.CountWords(p) >= 12).ToList();
        if (meaningful.Count < 2) return 0;

        var duplicates = 0;
        for (var i = 0; i < meaningful.Count; i++)
        {
            for (var j = i + 1; j < meaningful.Count; j++)
            {
                if (SeoTemplateTokens.DistinctiveSimilarity(meaningful[i], meaningful[j]) >= 0.82)
                {
                    duplicates++;
                    break;
                }
            }
        }
        return Math.Round(duplicates * 100.0 / meaningful.Count, 1);
    }

    static int CountDirectAnswerBlocks(List<string> paragraphs, string focus)
    {
        var count = 0;
        foreach (var text in paragraphs)
        {
            var wc = SeoText.CountWords(text);
            if (wc is < SeoContentRules.MinAeoAnswerWords or > SeoContentRules.MaxAeoAnswerWords) continue;

            var isAnswerLike = SeoText.IsKeywordish(text, focus, 0.5)
                || text.Contains("است.", StringComparison.Ordinal)
                || text.Contains("می‌شود", StringComparison.Ordinal)
                || text.Contains("بله", StringComparison.Ordinal)
                || text.Contains("خیر", StringComparison.Ordinal);
            if (isAnswerLike) count++;
        }
        return count;
    }

    public static int CountWords(string text) => SeoText.CountWords(text);
    public static int CountPhrase(string text, string phrase) => SeoText.CountPhrase(text, phrase);

    [GeneratedRegex("<h2[^>]*>.*?</h2>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex H2Regex();

    [GeneratedRegex("<h3[^>]*>.*?</h3>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex H3Regex();

    [GeneratedRegex("<p[^>]*>.*?</p>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex PRegex();

    [GeneratedRegex("<table[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex TableRegex();

    [GeneratedRegex("<ol[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex OlRegex();

    /// <summary>فقط لینک‌های داخلی (نسبی) — قبلاً لینک خارجی هم داخلی شمرده می‌شد.</summary>
    [GeneratedRegex("<a\\s[^>]*href\\s*=\\s*[\"'](/[^\"']*)[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex InternalLinkRegex();

    [GeneratedRegex("<a\\s[^>]*href\\s*=\\s*[\"']https?://[^\"']*[\"']", RegexOptions.IgnoreCase)]
    private static partial Regex ExternalLinkRegex();

    [GeneratedRegex("<a\\s[^>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex AnchorRegex();

    [GeneratedRegex("<li[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex LiRegex();
}
