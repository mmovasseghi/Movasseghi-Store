namespace MovasseghiShop.Web.Services;

/// <summary>اگر GSC پیکربندی شده باشد، یک بار پس از استارت همگام‌سازی می‌کند.</summary>
public class GscStartupSyncService(
    IServiceProvider services,
    ILogger<GscStartupSyncService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var gsc = scope.ServiceProvider.GetRequiredService<IGoogleSearchConsoleService>();
            if (!gsc.IsConfigured)
            {
                log.LogInformation("GSC: {Hint}", gsc.SetupHint);
                return;
            }

            var result = await gsc.SyncTopQueriesAsync(cancellationToken);
            log.LogInformation("GSC startup sync: {Message}", result.Message);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "GSC startup sync failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
