namespace MovasseghiShop.Web.Services;

/// <summary>برچسب‌های یکسان UI برای دروازه SEO محصولات.</summary>
public static class SeoPublishGateLabels
{
    public const string Verified = "SEO تأییدشده";
    public const string VerifiedBadge = "✅ SEO تأییدشده";
    public const string NotVerified = "SEO ناقص";
    public const string StatLabel = "SEO تأییدشده";
    public const string NeedsWork = "نیاز به بهبود SEO";
    public static string GateHint =>
        "دروازه انتشار: بدون مشکل بحرانی، حداقل هر بُعد (مثلاً تصویر ≥" + SeoContentRules.MinImageScore
        + "، محتوا ≥" + SeoContentRules.MinContentScore + ")، "
        + "و امتیاز نمایشی منطبق با آمادگی رتبه‌گیری و قدرت رقابتی (حداقل "
        + SeoContentRules.MinCompetitiveStrength + "). "
        + "امتیاز نمایشی = کمینهٔ میانگین وزنی، آمادگی و رقابتی — نه یک بُعد تکی.";

    public static string ActivationBlockedMessage(int displayScore) =>
        $"فعال‌سازی در فروشگاه ممکن نیست — امتیاز نمایشی {displayScore}/100 هنوز دروازه انتشار را باز نمی‌کند. "
        + GateHint;

    public static string ActiveDespiteLowScoreMessage(int displayScore) =>
        $"محصول در فروشگاه فعال ماند (امتیاز نمایشی {displayScore}/100). برای بهبود SEO از Auto-Fix استفاده کنید — خاموش کردن فقط با برداشتن تیک «فعال».";

    public static string AutoFixFollowUpMessage(Models.SeoScoreResult score)
    {
        var critical = score.Ultimate?.CriticalIssues.Count ?? 0;
        if (score.IsPublishReady)
            return string.Empty;
        if (critical > 0)
            return $"{critical} مشکل بحرانی باقی است — جزئیات در پنل SEO.";
        return $"دروازه انتشار هنوز باز نیست. {GateHint}";
    }

    public const string AutoFixButton = "✨ درست‌سازی خودکار SEO";
    public const string AutoFixPanelTitle = "درست‌سازی خودکار SEO";

    public const string ReadinessLabel = "آمادگی رتبه‌گیری";
    public const string ReadinessHint = "میانگین Ranking Readiness — ترکیب امتیاز صفحه و قدرت رقابتی نسبت به SERP";

    public static string ReadinessVerdict(int readiness) => readiness switch
    {
        >= 90 => "موقعیت رقابتی قوی",
        >= 80 => "موقعیت رقابتی مناسب",
        >= 65 => "فاصله رقابتی باقی است",
        >= 40 => "نیازمند کار جدی",
        _ => "بحرانی"
    };
}
