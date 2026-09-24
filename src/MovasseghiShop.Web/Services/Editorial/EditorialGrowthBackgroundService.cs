using Microsoft.Extensions.Options;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Services.Editorial;

public sealed class EditorialGrowthBackgroundService(
    IServiceProvider services,
    IOptions<EditorialGrowthOptions> options,
    ILogger<EditorialGrowthBackgroundService> log) : BackgroundService
{
    DateTime? _lastPeriodicRankSyncUtc;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.Value;
        if (!opt.Enabled)
        {
            log.LogInformation("EditorialGrowthBackgroundService: غیرفعال در appsettings");
            return;
        }

        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<IEditorialGrowthService>();
                var result = await engine.RunDailyCycleAsync(stoppingToken);
                if (result.DraftsCreated > 0 || result.PostsPublished > 0)
                {
                    log.LogInformation(
                        "Editorial growth: drafts={Drafts} published={Pub} queued={Q} — {Log}",
                        result.DraftsCreated, result.PostsPublished, result.TopicsQueued,
                        string.Join(" | ", result.Log));
                }

                var hours = Math.Clamp(opt.PeriodicRankSyncHours, 0, 168);
                if (hours > 0 && (_lastPeriodicRankSyncUtc == null
                        || DateTime.UtcNow - _lastPeriodicRankSyncUtc >= TimeSpan.FromHours(hours)))
                {
                    var rank = scope.ServiceProvider.GetRequiredService<IRankTrackerService>();
                    var sync = await rank.SyncFromApiAsync(stoppingToken);
                    _lastPeriodicRankSyncUtc = DateTime.UtcNow;
                    if (sync.ApiConfigured)
                        log.LogInformation("Periodic rank sync: {Message}", sync.Message);
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Editorial growth cycle failed");
            }

            var delay = Math.Clamp(opt.CheckIntervalMinutes, 15, 180);
            await Task.Delay(TimeSpan.FromMinutes(delay), stoppingToken);
        }
    }
}
