namespace MovasseghiShop.Web.Services;

using System.Net;
using MovasseghiShop.Web.Models.Entities;

/// <summary>
/// ۲۰ کلمه کلیدی اصلی سایت — اولویت ۱ تا ۳ ثابت (درخواست کاربر).
/// </summary>
public static class SiteKeywordStrategy
{
    public const string Primary = "ظروف یکبار مصرف گیاهی";
    public const string Secondary = "ظروف یکبار مصرف";
    public const string Tertiary = "ظروف یکبار مصرف آملون";

    public static readonly IReadOnlyList<PillarKeywordDef> Pillars =
    [
        new(1,  Primary,                          "general",   "home",      "commercial"),
        new(2,  Secondary,                        "general",   "catalog",   "commercial"),
        new(3,  Tertiary,                         "brand",     "amelon",    "commercial"),
        new(4,  "پخش عمده ظروف یکبار مصرف گیاهی", "b2b",       "wholesale", "commercial"),
        new(5,  "خرید عمده ظروف یکبار مصرف",       "b2b",       "wholesale", "commercial"),
        new(6,  "ظروف یکبار مصرف گیاهی آملون",    "brand",     "amelon",    "commercial"),
        new(7,  "پخش عمده ظروف یکبار مصرف",       "b2b",       "wholesale", "commercial"),
        new(8,  "قیمت ظروف یکبار مصرف آملون",     "price",     "pricing",   "commercial"),
        new(9,  "خرید عمده ظروف یکبار مصرف آملون","b2b",       "wholesale", "commercial"),
        new(10, "قیمت ظروف یکبار مصرف",           "price",     "pricing",   "commercial"),
        new(11, "ظروف گیاهی یکبار مصرف",          "general",   "catalog",   "commercial"),
        new(12, "ظروف نشاسته یکبار مصرف",         "general",   "catalog",   "informational"),
        new(13, "کاسه یکبار مصرف گیاهی",          "product",   "catalog",   "commercial"),
        new(14, "لیوان یکبار مصرف گیاهی",         "product",   "catalog",   "commercial"),
        new(15, "ظروف مایکروویوی یکبار مصرف",     "product",   "catalog",   "commercial"),
        new(16, "ظروف فست فود یکبار مصرف",        "usecase",   "catalog",   "commercial"),
        new(17, "ظروف کترینگ یکبار مصرف",         "usecase",   "catalog",   "commercial"),
        new(18, "نمایندگی آملون تهران",           "brand",     "about",     "informational"),
        new(19, "فروش عمده ظروف گیاهی",           "b2b",       "wholesale", "commercial"),
        new(20, "ظروف زیست تخریب پذیر یکبار مصرف","general",   "catalog",   "informational"),
    ];

    public static string SiteTitle =>
        $"فروشگاه موثقی | {Primary} | پخش عمده آملون";

    public static string SiteDescription =>
        $"فروشگاه موثقی — نمایندگی رسمی آملون در تهران. {Primary}، {Secondary} و {Tertiary}. " +
        "خرید عمده با حداقل ۱۰ میلیون تومان، ارسال سراسری، ضمانت اصالت ۱۰۰٪.";

    public static string MetaKeywords =>
        string.Join("، ", Pillars.Select(p => p.Phrase));

    public static string MetaKeywordsTop(int count = 10) =>
        string.Join("، ", Pillars.Take(count).Select(p => p.Phrase));

    /// <summary>کلیدواژه‌های متا مخصوص هر محصول — نه لیست ثابت سراسری.</summary>
    public static string SuggestProductMetaKeywords(Product product, string focus, string secondary, int count = 10)
    {
        var keywords = new List<string>();
        void Add(string? phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase)) return;
            if (!keywords.Contains(phrase, StringComparer.OrdinalIgnoreCase))
                keywords.Add(phrase.Trim());
        }

        Add(focus);
        Add(secondary);
        Add(SuggestProductSecondary(product));

        var name = product.Name ?? "";
        var type = product.ProductType ?? "";
        foreach (var pillar in Pillars.Where(p => p.Cluster is "product" or "usecase"))
        {
            if (name.Contains(pillar.Phrase, StringComparison.OrdinalIgnoreCase)
                || type.Contains(pillar.Phrase, StringComparison.OrdinalIgnoreCase))
                Add(pillar.Phrase);
        }

        if (product.MicrowaveSafe)
            Add("ظروف مایکروویوی یکبار مصرف");
        if (product.CapacityCc is > 0)
            Add($"{product.CapacityCc} سی‌سی ظروف گیاهی");

        Add(Tertiary);
        Add(Primary);

        if (keywords.Count < count)
        {
            foreach (var pillar in Pillars)
            {
                Add(pillar.Phrase);
                if (keywords.Count >= count) break;
            }
        }

        return string.Join("، ", keywords.Take(count));
    }

    /// <summary>کلمه فرعی پیشنهادی برای محصول بر اساس نوع.</summary>
    public static string SuggestProductSecondary(Product product)
    {
        var name = product.Name;
        var type = product.ProductType ?? "";
        if (type.Contains("کاسه", StringComparison.OrdinalIgnoreCase) || name.Contains("کاسه", StringComparison.OrdinalIgnoreCase))
            return "کاسه یکبار مصرف گیاهی";
        if (type.Contains("لیوان", StringComparison.OrdinalIgnoreCase) || name.Contains("لیوان", StringComparison.OrdinalIgnoreCase))
            return "لیوان یکبار مصرف گیاهی";
        if (name.Contains("مایکرو", StringComparison.OrdinalIgnoreCase) || product.MicrowaveSafe)
            return "ظروف مایکروویوی یکبار مصرف";
        if (name.Contains("فست", StringComparison.OrdinalIgnoreCase))
            return "ظروف فست فود یکبار مصرف";
        return Secondary;
    }

    /// <summary>
    /// Focus Keyword باید یک عبارت جستجوی طبیعی و کوتاه باشد.
    /// نسخه قبلی عبارت‌های ترکیبی با خط تیره می‌ساخت («کاسه گیاهی — ظروف یکبار مصرف آملون»)
    /// که نه کسی جستجو می‌کرد، نه در Title/H2 جا می‌شد و نه چگالی درستی می‌گرفت.
    /// </summary>
    public static string SuggestProductPrimary(Product product)
    {
        var name = WebUtility.HtmlDecode(product.Name ?? "").Trim();
        if (name.Length == 0) return Primary;

        // فقط بخش اول نام — پسوندهای توضیحی/برندی حذف می‌شوند
        var head = name
            .Split(['—', '–', '|', '،', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? name;
        if (head.Length < 3) head = name;

        var hasBrand = head.Contains("آملون", StringComparison.OrdinalIgnoreCase);
        var hasNature = head.Contains("گیاهی", StringComparison.OrdinalIgnoreCase)
            || head.Contains("یکبار مصرف", StringComparison.OrdinalIgnoreCase);

        var keyword = head;
        if (!hasNature) keyword = $"{keyword} گیاهی";
        if (!hasBrand && keyword.Length + 7 <= 45) keyword = $"{keyword} آملون";

        return keyword.Length <= 55 ? keyword : keyword[..55].Trim();
    }
}

public readonly record struct PillarKeywordDef(
    int Priority,
    string Phrase,
    string Cluster,
    string OwnerPageKey,
    string SearchIntent);
