using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;

namespace MovasseghiShop.Web.Services.Editorial;

/// <summary>زمان‌بندی انتشار با jitter — ریتم اپراتور انسانی، نه ربات ثانیه‌ای.</summary>
public static class EditorialHumanSchedule
{
    static readonly TimeZoneInfo Tehran = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Iran Standard Time" : "Asia/Tehran");

    public static DateTime ToUtc(DateTime tehranLocal) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(tehranLocal, DateTimeKind.Unspecified), Tehran);

    public static DateTime NowTehran() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tehran);

    public static DateTime ToTehran(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tehran);

    public static string FormatTehran(DateTime utc) =>
        ToTehran(utc).ToString("yyyy/MM/dd HH:mm");

    static readonly string[] WeekdayFa =
        ["یکشنبه", "دوشنبه", "سه‌شنبه", "چهارشنبه", "پنج‌شنبه", "جمعه", "شنبه"];

    public static string WeekdayNameFa(DateTime tehranLocal) =>
        WeekdayFa[(int)tehranLocal.DayOfWeek];

    public static string DescribeSkipWeekdays(string? skipCsv)
    {
        if (string.IsNullOrWhiteSpace(skipCsv)) return "—";
        return skipCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var d) && d is >= 0 and <= 6 ? WeekdayFa[d] : s)
            .Aggregate((a, b) => $"{a}، {b}");
    }

    public static bool IsSkippedWeekday(EditorialGrowthOptions opt)
    {
        if (string.IsNullOrWhiteSpace(opt.SkipWeekdays)) return false;
        var dow = (int)NowTehran().DayOfWeek;
        return opt.SkipWeekdays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(s => int.TryParse(s, out var d) && d == dow);
    }

    public static bool IsWithinPublishWindow(EditorialGrowthOptions opt)
    {
        var t = NowTehran();
        return t.Hour >= opt.PublishHourStartTehran && t.Hour < opt.PublishHourEndTehran;
    }

    /// <summary>زمان انتشار پیشنهادی با jitter تصادفی در پنجره امروز یا فردا.</summary>
    public static DateTime SuggestNextPublishUtc(EditorialGrowthOptions opt, Random? rng = null)
    {
        rng ??= Random.Shared;
        var local = NowTehran();
        var hour = rng.Next(opt.PublishHourStartTehran, Math.Max(opt.PublishHourStartTehran + 1, opt.PublishHourEndTehran));
        var minute = rng.Next(3, 58);
        var candidate = new DateTime(local.Year, local.Month, local.Day, hour, minute, 0);

        if (candidate <= local || !IsWithinPublishWindow(opt))
            candidate = candidate.AddDays(1).AddHours(rng.Next(0, Math.Max(1, opt.PublishHourEndTehran - opt.PublishHourStartTehran)));

        return ToUtc(candidate);
    }

    public static async Task<bool> CanPublishAnotherTodayAsync(
        ApplicationDbContext db, EditorialGrowthOptions opt, CancellationToken ct)
    {
        var startTehran = NowTehran().Date;
        var startUtc = ToUtc(startTehran);
        var publishedToday = await db.BlogPosts.CountAsync(
            b => b.IsPublished && b.PublishedAt >= startUtc, ct);
        return publishedToday < opt.MaxPostsPerDay;
    }

    public static async Task<bool> RespectsCooldownAsync(
        ApplicationDbContext db, EditorialGrowthOptions opt, CancellationToken ct)
    {
        var last = await db.BlogPosts
            .Where(b => b.IsPublished && b.PublishedAt != null)
            .OrderByDescending(b => b.PublishedAt)
            .Select(b => b.PublishedAt)
            .FirstOrDefaultAsync(ct);
        if (last == null) return true;
        return DateTime.UtcNow - last.Value >= TimeSpan.FromHours(opt.MinHoursBetweenPosts);
    }

    public static async Task<int> CountPublishedTodayTehranAsync(
        ApplicationDbContext db, CancellationToken ct = default)
    {
        var startTehran = NowTehran().Date;
        var startUtc = ToUtc(startTehran);
        return await db.BlogPosts.CountAsync(
            b => b.IsPublished && b.PublishedAt >= startUtc, ct);
    }

    public static IReadOnlyList<EditorialTimelineStep> BuildPublishTimeline(
        EditorialGrowthOptions opt,
        DateTime? nextScheduledUtc,
        int publishedTodayTehran)
    {
        var now = NowTehran();
        var nextFa = nextScheduledUtc.HasValue ? FormatTehran(nextScheduledUtc.Value) : "بعد از ساخت پیش‌نویس آماده";
        var window = $"{opt.PublishHourStartTehran}:۰۰–{opt.PublishHourEndTehran}:۰۰ تهران";
        var skip = DescribeSkipWeekdays(opt.SkipWeekdays);

        return
        [
            new(1, "بیدار شدن سرویس",
                $"هر {opt.CheckIntervalMinutes} دقیقه پس‌زمینه چک می‌کند (الان: {FormatTehran(DateTime.UtcNow)} تهران).",
                "خودکار"),
            new(2, "روز و پنجرهٔ مجاز",
                $"امروز {WeekdayNameFa(now)} — انتشار فقط بین {window}. روزهای تعطیل: {skip}.",
                opt.IgnorePublishWindow ? "Dev: پنجره باز" : null),
            new(3, "سقف روزانه",
                $"امروز {publishedTodayTehran} از {opt.MaxPostsPerDay} پست منتشر شده؛ فاصلهٔ حداقل {opt.MinHoursBetweenPosts} ساعت بین دو انتشار.",
                $"{publishedTodayTehran}/{opt.MaxPostsPerDay}"),
            new(4, "انتخاب موضوع",
                "از صف planned: بالاترین Opportunity + اولویت pillar (کلیدواژه‌های استراتژیک سایت).",
                "صف"),
            new(5, "تولید محتوا",
                "متن + FAQ + نمایه 🌅 Scene + حداقل ۳ عکس در متن (Scene و 📦 Studio) با alt.",
                "کیفیت"),
            new(6, "Auto-Fix SEO",
                $"تا باز شدن دروازه انتشار و امتیاز نمایشی ≥ {opt.MinPublishDisplayScore}؛ در غیر این صورت حذف و تلاش مجدد.",
                $"≥ {opt.MinPublishDisplayScore}"),
            new(7, "زمان‌بندی انتشار",
                $"زمان پیشنهادی با jitter انسانی در پنجرهٔ تهران — نزدیک‌ترین: {nextFa}.",
                nextFa),
            new(8, "انتشار",
                opt.AutoPublishWhenReady
                    ? "در زمان Scheduled و اگر SEO آماده باشد → منتشر می‌شود."
                    : "فقط پیش‌نویس می‌ماند تا شما در ادمین منتشر کنید.",
                opt.AutoPublishWhenReady ? "Auto" : "دستی")
        ];
    }
}
