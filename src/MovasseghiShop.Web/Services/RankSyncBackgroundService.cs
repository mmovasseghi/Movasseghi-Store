using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Services;

public class RankSyncBackgroundService(IServiceProvider services, IConfiguration config, ILogger<RankSyncBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("SerpApi:AutoSync", false))
        {
            log.LogInformation("Rank auto-sync disabled (SerpApi:AutoSync=false)");
            return;
        }

        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var rank = scope.ServiceProvider.GetRequiredService<IRankTrackerService>();
                var competitor = scope.ServiceProvider.GetRequiredService<ICompetitorAnalysisService>();
                var sync = await rank.SyncFromApiAsync(stoppingToken);
                log.LogInformation("Rank sync: {Message}", sync.Message);
                if (sync.ApiConfigured)
                    await competitor.SyncAllPillarsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Rank background sync failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}

public class WeeklyReportBackgroundService(IServiceProvider services, IConfiguration config, ILogger<WeeklyReportBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("GrowthEngine:AutoWeeklyReport", true))
            return;

        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now.DayOfWeek == DayOfWeek.Monday && now.Hour < 6)
                {
                    using var scope = services.CreateScope();
                    var reports = scope.ServiceProvider.GetRequiredService<IGrowthReportService>();
                    var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                    var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
                    var exists = await db.GrowthReports.AnyAsync(r => r.WeekStart == weekStart, stoppingToken);
                    if (!exists)
                    {
                        var report = await reports.GenerateWeeklyReportAsync(stoppingToken);
                        log.LogInformation("Generated weekly report {Id}", report.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Weekly report generation failed");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
