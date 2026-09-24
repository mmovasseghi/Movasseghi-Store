using System.Text.Json;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Data;

public static class StudioSeedData
{
    public static IEnumerable<HomeSection> GetHomeSections()
    {
        var heroJson = JsonSerializer.Serialize(new
        {
            slides = new[]
            {
                new { kicker = "✦ نمایندگی رسمی آملون", title = "ظروف ", titleEm = "گیاهی", desc = $"پخش عمده <strong>{SiteKeywordStrategy.Primary}</strong> — {SiteKeywordStrategy.Secondary} · {SiteKeywordStrategy.Tertiary}", cta1Text = "ورود به فروشگاه", cta1Url = "/Catalog", cta2Text = "تماس فوری", cta2Url = "tel:09125199105" },
                new { kicker = "✦ " + SiteKeywordStrategy.Secondary, title = SiteKeywordStrategy.Tertiary.Split(' ').Last() + " ", titleEm = "آملون", desc = $"{SiteKeywordStrategy.Tertiary} · تحویل ۲–۷ روز · ضمانت اصالت", cta1Text = "مشاهده کاتالوگ", cta1Url = "/Catalog", cta2Text = "درباره موثقی", cta2Url = "/Page/About" },
                new { kicker = "✦ خرید عمده", title = "پخش ", titleEm = "عمده", desc = $"{SiteKeywordStrategy.Primary} — کاسه، لیوان، ظرف · {SiteKeywordStrategy.Secondary}", cta1Text = "خرید عمده", cta1Url = "/Catalog", cta2Text = "پخش عمده", cta2Url = "/Page/Wholesale" }
            },
            trustItems = new[] { "🌿 ۱۰۰٪ گیاهی", "📦 پخش عمده", "✓ اصل آملون", "🚚 ارسال سراسری" }
        });

        var featuresJson = JsonSerializer.Serialize(new
        {
            bandText = "چرا فروشگاه موثقی؟",
            cards = new[]
            {
                new { icon = "official", title = "نمایندگی رسمی", desc = "تنها نماینده آملون در تهران" },
                new { icon = "shield", title = "ضمانت اصالت", desc = "محصول مستقیم از کارخانه" },
                new { icon = "box", title = "پخش عمده", desc = "حداقل ۱۰ میلیون تومان" },
                new { icon = "truck", title = "ارسال سراسری", desc = "تحویل ۲ تا ۷ روز کاری" }
            }
        });

        var wholesaleJson = JsonSerializer.Serialize(new
        {
            badge = "خرید عمده",
            segments = new[]
            {
                new { title = "رستوران و فست‌فود", desc = "ظروف غذای داغ، مایکروویوی و پذیرایی", icon = "restaurant", url = "/Catalog?q=غذا" },
                new { title = "کافه و قنادی", desc = "لیوان، دسر و سرو نوشیدنی", icon = "cafe", url = "/Catalog/ByType?type=" + Uri.EscapeDataString("لیوان") },
                new { title = "کیترینگ و مهمانی", desc = "حجم بالا · بسته‌بندی یکبار مصرف", icon = "catering", url = "/Catalog" },
                new { title = "فروشگاه و پخش", desc = "خرید عمده از ۱۰ میلیون تومان", icon = "store", url = "/Catalog" }
            }
        });

        var offersJson = JsonSerializer.Serialize(new { badge = "پیشنهاد ویژه موثقی", subtitle = "فرصت محدود خرید عمده" });
        var featuredJson = JsonSerializer.Serialize(new { badge = "⭐ منتخب", subtitle = "جدیدترین کاتالوگ آملون" });

        return
        [
            new HomeSection { Key = "hero", Title = "Hero", ConfigJson = heroJson, SortOrder = 1 },
            new HomeSection { Key = "features", Title = "چرا موثقی", ConfigJson = featuresJson, SortOrder = 2 },
            new HomeSection { Key = "offers", Title = "پیشنهاد ویژه", ConfigJson = offersJson, SortOrder = 3 },
            new HomeSection { Key = "featured", Title = "محصولات برتر", ConfigJson = featuredJson, SortOrder = 4 },
            new HomeSection { Key = "wholesale", Title = "خرید عمده", Subtitle = "مناسب چه کسب‌وکارهایی؟", ConfigJson = wholesaleJson, SortOrder = 5 }
        ];
    }

    public static IEnumerable<NavItem> GetNavItems() =>
    [
        new() { Zone = "header", Label = "صفحه اصلی", Url = "/", SortOrder = 1 },
        new() { Zone = "header", Label = "محصولات", Url = "/Catalog", SortOrder = 2 },
        new() { Zone = "header", Label = "اخبار", Url = "/News", SortOrder = 3 },
        new() { Zone = "header", Label = "وبلاگ", Url = "/Blog", SortOrder = 4 },
        new() { Zone = "header", Label = "پخش عمده", Url = "/Page/Wholesale", SortOrder = 5 },
        new() { Zone = "header", Label = "درباره ما", Url = "/Page/About", SortOrder = 6 },
        new() { Zone = "header", Label = "تماس", Url = "/Page/Contact", SortOrder = 7 },
        new() { Zone = "drawer-main", Label = "صفحه اصلی", Url = "/", Icon = "🏠", SortOrder = 1 },
        new() { Zone = "drawer-main", Label = "همه محصولات", Url = "/Catalog", Icon = "📦", SortOrder = 2 },
        new() { Zone = "drawer-main", Label = "اخبار", Url = "/News", Icon = "📢", SortOrder = 3 },
        new() { Zone = "drawer-main", Label = "وبلاگ", Url = "/Blog", Icon = "📰", SortOrder = 4 },
        new() { Zone = "drawer-main", Label = "پیگیری سفارش", Url = "/Order/Track", Icon = "🚚", SortOrder = 5 },
        new() { Zone = "drawer-more", Label = "درباره ما", Url = "/Page/About", Icon = "ℹ️", SortOrder = 1 },
        new() { Zone = "drawer-more", Label = "تماس", Url = "/Page/Contact", Icon = "📞", SortOrder = 2 },
        new() { Zone = "drawer-more", Label = "سوالات متداول", Url = "/Page/Faq", Icon = "❓", SortOrder = 3 },
        new() { Zone = "drawer-more", Label = "پخش عمده", Url = "/Page/Wholesale", Icon = "📦", SortOrder = 4 },
        new() { Zone = "drawer-more", Label = "قیمت عمده", Url = "/Page/Pricing", Icon = "💰", SortOrder = 5 },
        new() { Zone = "footer-shop", Label = "همه محصولات", Url = "/Catalog", SortOrder = 1 },
        new() { Zone = "footer-shop", Label = "ظرف غذا", Url = "/Catalog/ByType?type=" + Uri.EscapeDataString("ظرف غذا"), SortOrder = 2 },
        new() { Zone = "footer-shop", Label = "لیوان", Url = "/Catalog/ByType?type=" + Uri.EscapeDataString("لیوان"), SortOrder = 3 },
        new() { Zone = "footer-shop", Label = "کاسه", Url = "/Catalog/ByType?type=" + Uri.EscapeDataString("کاسه"), SortOrder = 4 },
        new() { Zone = "footer-support", Label = "تماس با ما", Url = "/Page/Contact", SortOrder = 1 },
        new() { Zone = "footer-support", Label = "سوالات متداول", Url = "/Page/Faq", SortOrder = 2 },
        new() { Zone = "footer-support", Label = "پیگیری سفارش", Url = "/Order/Track", SortOrder = 3 },
        new() { Zone = "footer-support", Label = "درباره موثقی", Url = "/Page/About", SortOrder = 4 },
        new() { Zone = "footer-wholesale", Label = "پخش عمده", Url = "/Page/Wholesale", SortOrder = 1 },
        new() { Zone = "footer-wholesale", Label = "قیمت عمده", Url = "/Page/Pricing", SortOrder = 2 },
        new() { Zone = "footer-wholesale", Label = "ظروف آملون", Url = "/Page/Amelon", SortOrder = 3 },
        new() { Zone = "footer-wholesale", Label = "حداقل ۱۰ میلیون", Url = "/Catalog", SortOrder = 4 },
        new() { Zone = "footer-wholesale", Label = "قوانین", Url = "/Page/Terms", SortOrder = 5 },
        new() { Zone = "footer-wholesale", Label = "حریم خصوصی", Url = "/Page/Privacy", SortOrder = 6 }
    ];

    public static IEnumerable<ContentAtom> GetContentAtoms() =>
    [
        new ContentAtom
        {
            Key = "promo-bar",
            Title = "بنر بالای سایت",
            Body = JsonSerializer.Serialize(new { sloganFull = $"با <strong>موثقی</strong> {SiteKeywordStrategy.Primary} — {SiteKeywordStrategy.Tertiary}", sloganShort = SiteKeywordStrategy.Secondary + " · آملون · عمده", chips = new[] { "🌿 ۱۰۰٪ گیاهی", "💰 تخفیف پلکانی", "🛡️ ضمانت اصالت", "🚚 ارسال سراسری" } })
        },
        new ContentAtom { Key = "cart-empty", Title = "سبد خالی", Body = "سبد خرید خالی است", CtaText = "مشاهده محصولات", CtaUrl = "/Catalog" },
        new ContentAtom { Key = "catalog-empty", Title = "کاتالوگ خالی", Body = "محصولی یافت نشد. فیلترها را تغییر دهید یا جستجوی دیگری انجام دهید.", CtaText = "پاک کردن فیلترها", CtaUrl = "/Catalog" },
        new ContentAtom { Key = "catalog-search-empty", Title = "جستجو بدون نتیجه", Body = "عبارت دیگری امتحان کنید یا از دسته‌بندی‌ها انتخاب کنید.", CtaText = "همه محصولات", CtaUrl = "/Catalog" }
    ];

    public static IEnumerable<AuthorityCheckItem> GetAuthorityItems() =>
    [
        new() { Key = "gbp", Title = "Google Business Profile", Description = "پروفایل کسب‌وکار گوگل برای تهران با آدرس، تلفن و لینk سایت", HelpUrl = "https://business.google.com", SortOrder = 1 },
        new() { Key = "instagram", Title = "اینستاگرام + لینk سایت", Description = "صفحه @MovasseghiStore با bio لینk به فروشگاه", SortOrder = 2 },
        new() { Key = "local-schema", Title = "Schema LocalBusiness", Description = "JSON-LD کسب‌وکار محلی روی صفحه تماس", HelpUrl = "/Page/Contact", SortOrder = 3 },
        new() { Key = "backlinks-b2b", Title = "ثبت در دایرکتوری B2B", Description = "لیست فروشگاه در ۲–۳ دایرکتوری عمده‌فروشی ایران", SortOrder = 4 },
        new() { Key = "blog-outreach", Title = "mention در وبلاگ/خبر", Description = "حداقل ۱ مقاله یا خبر مهمان با لینk به موثقی", SortOrder = 5 },
        new() { Key = "sitemap-gsc", Title = "Google Search Console", Description = "ثبت sitemap.xml در Search Console", HelpUrl = "https://search.google.com/search-console", SortOrder = 6 }
    ];
}
