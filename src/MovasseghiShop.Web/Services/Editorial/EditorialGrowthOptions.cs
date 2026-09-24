namespace MovasseghiShop.Web.Services.Editorial;

public sealed class EditorialGrowthOptions
{
    public const string SectionName = "EditorialGrowth";

    /// <summary>سرویس پس‌زمینه روزانه فعال باشد.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>حداکثر انتشار در روز (تهران) — شبیه‌سازی ریتم انسانی.</summary>
    public int MaxPostsPerDay { get; set; } = 2;

    /// <summary>حداقل فاصله بین دو انتشار (ساعت).</summary>
    public int MinHoursBetweenPosts { get; set; } = 6;

    /// <summary>ساعت شروع پنجره انتشار (تهران، ۰–۲۳).</summary>
    public int PublishHourStartTehran { get; set; } = 9;

    /// <summary>ساعت پایان پنجره انتشار (تهران).</summary>
    public int PublishHourEndTehran { get; set; } = 20;

    /// <summary>اگر پس از Auto-Fix دروازه انتشار باز شد، خودکار منتشر شود.</summary>
    public bool AutoPublishWhenReady { get; set; } = true;

    /// <summary>حداکثر موضوع در صف (برای کنترل حجم).</summary>
    public int MaxQueuedTopics { get; set; } = 40;

    /// <summary>روزهای بدون انتشار: 0=یکشنبه … 6=شنبه (مثلاً "5" = جمعه).</summary>
    public string SkipWeekdays { get; set; } = "";

    /// <summary>بازه بررسی سرویس پس‌زمینه (دقیقه).</summary>
    public int CheckIntervalMinutes { get; set; } = 45;

    /// <summary>پس از انتشار، رتبه کلیدواژه مقاله از SerpAPI ثبت شود.</summary>
    public bool SyncRankAfterPublish { get; set; } = true;

    /// <summary>فقط برای تست محلی — دور زدن پنجره ۹–۲۰ تهران.</summary>
    public bool IgnorePublishWindow { get; set; }

    /// <summary>هر N ساعت یک بار همگام‌سازی رتبه pillar+blog (۰ = غیرفعال).</summary>
    public int PeriodicRankSyncHours { get; set; } = 12;

    /// <summary>مقالات منتشرشده با امتیاز زیر این آستانه دوباره Auto-Fix می‌شوند.</summary>
    public int RepublishBoostScoreThreshold { get; set; } = 76;

    /// <summary>حداقل امتیاز نمایشی برای نگه‌داشتن / انتشار مقالهٔ خودکار.</summary>
    public int MinPublishDisplayScore { get; set; } = 78;

    /// <summary>حداکثر تلاش ساخت پیش‌نویس برای یک موضوع قبل از رد شدن.</summary>
    public int MaxDraftAttemptsPerTopic { get; set; } = 6;

    /// <summary>هدف تعداد مقالات باکیفیت در هر «پاکسازی و تولید مجدد».</summary>
    public int TargetArticlesPerQualitySweep { get; set; } = 10;
}
