using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface IAuthorityChecklistService
{
    Task<List<AuthorityCheckItem>> GetAllAsync(CancellationToken ct = default);
    Task ToggleAsync(int id, bool isDone, CancellationToken ct = default);
    Task<int> GetScorePercentAsync(CancellationToken ct = default);
}

public class AuthorityChecklistService(ApplicationDbContext db) : IAuthorityChecklistService
{
    public async Task<List<AuthorityCheckItem>> GetAllAsync(CancellationToken ct = default) =>
        await db.AuthorityCheckItems.OrderBy(a => a.SortOrder).ToListAsync(ct);

    public async Task ToggleAsync(int id, bool isDone, CancellationToken ct = default)
    {
        var item = await db.AuthorityCheckItems.FindAsync([id], ct);
        if (item == null) return;
        item.IsDone = isDone;
        item.CompletedAt = isDone ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> GetScorePercentAsync(CancellationToken ct = default)
    {
        var items = await db.AuthorityCheckItems.ToListAsync(ct);
        if (items.Count == 0) return 0;
        return (int)Math.Round(100.0 * items.Count(i => i.IsDone) / items.Count);
    }
}

public interface IGrowthReportService
{
    Task<GrowthReport> GenerateWeeklyReportAsync(CancellationToken ct = default);
    Task<List<GrowthReport>> GetReportsAsync(int take = 12, CancellationToken ct = default);
    Task<GrowthReport?> GetReportAsync(int id, CancellationToken ct = default);
}

public class GrowthReportService(
    ApplicationDbContext db,
    IRankTrackerService rankTracker,
    IAuthorityChecklistService authority) : IGrowthReportService
{
    public async Task<GrowthReport> GenerateWeeklyReportAsync(CancellationToken ct = default)
    {
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var radar = await rankTracker.GetRadarAsync(ct);
        var authScore = await authority.GetScorePercentAsync(ct);
        var profiles = await db.SeoProfiles.Where(p => p.EntityType == "product").ToListAsync(ct);
        var totalProducts = await db.Products.CountAsync(ct);
        var ready = profiles.Count(p => p.IsPublishReady);
        var blocked = profiles.Count(p => !p.IsPublishReady && p.TotalScore > 0);
        var noProfile = totalProducts - profiles.Count;
        var avg = profiles.Count > 0 ? profiles.Average(p => p.TotalScore) : 0;

        var actions = await db.GrowthActions.Where(a => a.Status == "open")
            .OrderByDescending(a => a.Priority == "high").Take(5).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine($"# گزارش هفتگی Growth — {weekStart:yyyy/MM/dd}");
        sb.AppendLine();
        sb.AppendLine("## خلاصه");
        sb.AppendLine($"- میانگین SEO محصولات: **{avg:0}**/100");
        sb.AppendLine($"- {SeoPublishGateLabels.Verified}: **{ready}** | {SeoPublishGateLabels.NeedsWork}: **{blocked + noProfile}**");
        sb.AppendLine($"- Rank Radar — تاپ ۱۰: **{radar.Top10Count}** | تاپ ۳: **{radar.Top3Count}**");
        sb.AppendLine($"- Authority Score: **{authScore}%**");
        sb.AppendLine();
        sb.AppendLine("## رتبه Pillar (آخرین ثبت)");
        foreach (var row in radar.Rows.Where(r => r.KeywordType == "pillar").Take(10))
        {
            var pos = row.LastPosition.HasValue ? row.LastPosition.ToString() : "—";
            sb.AppendLine($"- {row.Keyword}: **{pos}**");
        }
        sb.AppendLine();
        sb.AppendLine("## ۵ کار اولویت‌دار");
        if (actions.Count == 0)
            sb.AppendLine("- همه کارها انجام شده — عالی!");
        else
            foreach (var a in actions)
                sb.AppendLine($"- [{a.Priority}] {a.Title}");
        sb.AppendLine();
        sb.AppendLine("## وضعیت محصولات");
        sb.AppendLine($"- کل محصولات: **{totalProducts}**");
        sb.AppendLine($"- میانگین امتیاز SEO: **{avg:0}**/100");
        sb.AppendLine($"- {SeoPublishGateLabels.Verified}: **{ready}** | نیاز به کار: **{blocked + noProfile}**");
        sb.AppendLine();
        sb.AppendLine("## پیشنهاد اقدام این هفته");
        if (actions.Count == 0)
            sb.AppendLine("- همه کارهای باز انجام شده — روی E-E-A-T و رتبه pillar تمرکز کنید.");
        else
            foreach (var a in actions)
                sb.AppendLine($"- [{a.Priority}] {a.Title} — {a.Description}");
        sb.AppendLine();
        sb.AppendLine("## نکات محتوایی");
        sb.AppendLine("- Auto-Fix گروهی را از مرکز SEO اجرا کنید اگر محصول جدید اضافه شده.");
        sb.AppendLine("- دسته‌های بدون تصویر شاخص را در ویرایش دسته تکمیل کنید.");
        sb.AppendLine("- چک‌لیست E-E-A-T را در پنل اعتبار بررسی کنید.");

        var metrics = JsonSerializer.Serialize(new
        {
            avgSeo = avg,
            publishReady = ready,
            blocked = blocked + noProfile,
            top10 = radar.Top10Count,
            top3 = radar.Top3Count,
            authorityScore = authScore,
            totalProducts = totalProducts
        });

        var report = new GrowthReport
        {
            WeekStart = weekStart,
            Title = $"گزارش هفتگی {weekStart:yyyy/MM/dd}",
            BodyMarkdown = sb.ToString(),
            MetricsJson = metrics,
            CreatedAt = DateTime.UtcNow
        };
        db.GrowthReports.Add(report);
        await db.SaveChangesAsync(ct);
        return report;
    }

    public async Task<List<GrowthReport>> GetReportsAsync(int take = 12, CancellationToken ct = default) =>
        await db.GrowthReports.OrderByDescending(r => r.CreatedAt).Take(take).ToListAsync(ct);

    public async Task<GrowthReport?> GetReportAsync(int id, CancellationToken ct = default) =>
        await db.GrowthReports.FindAsync([id], ct);
}
