namespace MovasseghiShop.Web.Models.Entities;

/// <summary>
/// یک بازدید صفحه. پایه تمام گزارش‌های ترافیک، صفحات پربازدید،
/// منبع ورود، نرخ خروج و قیف تبدیل.
/// عمداً بدون IP خام و بدون کوکی ردیابی شخص ثالث ذخیره می‌شود:
/// شناسه بازدیدکننده یک هش بی‌بازگشت است تا حریم خصوصی حفظ شود.
/// </summary>
public class PageVisit
{
    public long Id { get; set; }

    /// <summary>مسیر نرمال‌شده صفحه — مثلاً <c>/Shop/Product/kase-280</c>.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Query string بدون داده حساس — برای تحلیل فیلترهای پرکاربرد کاتالوگ.</summary>
    public string? Query { get; set; }

    /// <summary>نوع صفحه: home · catalog · product · category · blog · news · page · cart · checkout · other</summary>
    public string PageType { get; set; } = "other";

    /// <summary>شناسه موجودیت مرتبط (محصول/دسته/مقاله) برای گزارش «پرفروش‌ترین صفحه محصول».</summary>
    public int? EntityId { get; set; }

    /// <summary>عنوان صفحه در لحظه بازدید — گزارش بدون Join خوانا می‌شود.</summary>
    public string? Title { get; set; }

    /// <summary>هش نشست — بازدید یکتا و «صفحه در هر نشست» را می‌سازد.</summary>
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>هش بازدیدکننده (پایدارتر از نشست) — تفکیک بازدیدکننده جدید و بازگشتی.</summary>
    public string VisitorKey { get; set; } = string.Empty;

    public bool IsNewVisitor { get; set; }
    public bool IsAuthenticated { get; set; }

    /// <summary>دامنه ارجاع‌دهنده — google.com، instagram.com، direct، internal</summary>
    public string? ReferrerHost { get; set; }

    /// <summary>دسته منبع ورود: organic · social · direct · referral · internal · paid</summary>
    public string Channel { get; set; } = "direct";

    /// <summary>کلیدواژه ورودی اگر ارجاع‌دهنده آن را فرستاده باشد.</summary>
    public string? SearchTerm { get; set; }

    /// <summary>mobile · tablet · desktop · bot</summary>
    public string DeviceType { get; set; } = "desktop";
    public bool IsBot { get; set; }

    /// <summary>UTM برای سنجش کمپین‌ها.</summary>
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }

    /// <summary>مدت پاسخ سرور به میلی‌ثانیه — پایه گزارش کندترین صفحات (مهم برای CWV).</summary>
    public int ServerMs { get; set; }
    public int StatusCode { get; set; } = 200;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>تاریخ به‌صورت عدد <c>yyyyMMdd</c> — گروه‌بندی روزانه بدون تابع تاریخ SQL.</summary>
    public int DateKey { get; set; }

    /// <summary>ساعت روز (۰–۲۳) — گزارش «پرترافیک‌ترین ساعت برای تماس فروش».</summary>
    public int HourOfDay { get; set; }
}

/// <summary>
/// رویداد تبدیل و تعامل: افزودن به سبد، شروع پرداخت، ثبت سفارش،
/// ارسال استعلام، کلیک تماس. پایه قیف فروش و نرخ کلیک واقعی سایت.
/// </summary>
public class SiteEvent
{
    public long Id { get; set; }

    /// <summary>
    /// نام رویداد: view_product · add_to_cart · begin_checkout · purchase
    /// · submit_inquiry · click_phone · click_whatsapp · search · filter_use
    /// </summary>
    public string Name { get; set; } = string.Empty;

    public string? Path { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public string VisitorKey { get; set; } = string.Empty;

    public int? EntityId { get; set; }
    public string? EntityType { get; set; }

    /// <summary>برچسب خوانا — نام محصول، عبارت جستجو، نام فیلتر.</summary>
    public string? Label { get; set; }

    /// <summary>ارزش ریالی رویداد (سفارش/سبد) — محاسبه درآمد قیف.</summary>
    public decimal? Value { get; set; }
    public int? Quantity { get; set; }

    public string Channel { get; set; } = "direct";
    public string DeviceType { get; set; } = "desktop";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int DateKey { get; set; }
}
