using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovasseghiShop.Web.Data;

namespace MovasseghiShop.Web.Services;

public sealed class SeoMaintenanceOptions
{
    public const string SectionName = "Seo:Maintenance";

    /// <summary>با تغییر این مقدار در appsettings بعد از دیپلوی، یک بار Full Catalog Fix اجرا می‌شود.</summary>
    public string CatalogVersion { get; set; } = "1";

    public bool RunFullCatalogOnStartup { get; set; } = true;

    public bool AutoFixProductOnSave { get; set; } = false;

    public bool AutoFixCategoryOnSave { get; set; } = true;

    public int BulkFixMaxIterations { get; set; } = 5;

    public bool AutoFixBlogOnSave { get; set; } = false;
}

public sealed record SeoMaintenanceSnapshot
{
    public bool IsRunning { get; init; }
    public string? CurrentTask { get; init; }
    public int Done { get; init; }
    public int Total { get; init; }
    public string? LastSummary { get; init; }
    public DateTimeOffset? LastCompletedAt { get; init; }
}

public enum SeoMaintenanceJobKind
{
    FullCatalog,
    AllCategories,
    AllPublishedBlogs,
    Product,
    Category,
    Blog
}

public sealed record SeoMaintenanceJob(SeoMaintenanceJobKind Kind, int? EntityId = null);

public interface ISeoMaintenanceCoordinator
{
    SeoMaintenanceSnapshot GetSnapshot();
    ValueTask RequestFullCatalogAsync(CancellationToken ct = default);
    ValueTask RequestAllCategoriesAsync(CancellationToken ct = default);
    ValueTask RequestProductFixAsync(int productId, CancellationToken ct = default);
    ValueTask RequestCategoryFixAsync(int categoryId, CancellationToken ct = default);
    ValueTask RequestBlogFixAsync(int blogId, CancellationToken ct = default);
    ValueTask RequestAllPublishedBlogsAsync(CancellationToken ct = default);
}

public sealed class SeoMaintenanceCoordinator : ISeoMaintenanceCoordinator
{
    readonly Channel<SeoMaintenanceJob> _queue = Channel.CreateUnbounded<SeoMaintenanceJob>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    volatile SeoMaintenanceSnapshot _snapshot = new();

    public ChannelReader<SeoMaintenanceJob> Reader => _queue.Reader;

    public SeoMaintenanceSnapshot GetSnapshot() => _snapshot;

    internal void UpdateSnapshot(SeoMaintenanceSnapshot snapshot) => _snapshot = snapshot;

    public ValueTask RequestFullCatalogAsync(CancellationToken ct = default) =>
        _queue.Writer.WriteAsync(new SeoMaintenanceJob(SeoMaintenanceJobKind.FullCatalog), ct);

    public ValueTask RequestAllCategoriesAsync(CancellationToken ct = default) =>
        _queue.Writer.WriteAsync(new SeoMaintenanceJob(SeoMaintenanceJobKind.AllCategories), ct);

    public ValueTask RequestProductFixAsync(int productId, CancellationToken ct = default) =>
        _queue.Writer.WriteAsync(new SeoMaintenanceJob(SeoMaintenanceJobKind.Product, productId), ct);

    public ValueTask RequestCategoryFixAsync(int categoryId, CancellationToken ct = default) =>
        _queue.Writer.WriteAsync(new SeoMaintenanceJob(SeoMaintenanceJobKind.Category, categoryId), ct);

    public ValueTask RequestBlogFixAsync(int blogId, CancellationToken ct = default) =>
        _queue.Writer.WriteAsync(new SeoMaintenanceJob(SeoMaintenanceJobKind.Blog, blogId), ct);

    public ValueTask RequestAllPublishedBlogsAsync(CancellationToken ct = default) =>
        _queue.Writer.WriteAsync(new SeoMaintenanceJob(SeoMaintenanceJobKind.AllPublishedBlogs), ct);
}

public sealed class SeoMaintenanceBackgroundService(
    SeoMaintenanceCoordinator coordinator,
    IServiceProvider services,
    IOptions<SeoMaintenanceOptions> options,
    IWebHostEnvironment env,
    ILogger<SeoMaintenanceBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in coordinator.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                switch (job.Kind)
                {
                    case SeoMaintenanceJobKind.FullCatalog:
                        await RunFullCatalogAsync(stoppingToken);
                        break;
                    case SeoMaintenanceJobKind.AllCategories:
                        await RunAllCategoriesAsync(stoppingToken);
                        break;
                    case SeoMaintenanceJobKind.Product when job.EntityId is > 0:
                        await RunProductAsync(job.EntityId.Value, stoppingToken);
                        break;
                    case SeoMaintenanceJobKind.Category when job.EntityId is > 0:
                        await RunCategoryAsync(job.EntityId.Value, stoppingToken);
                        break;
                    case SeoMaintenanceJobKind.Blog when job.EntityId is > 0:
                        await RunBlogAsync(job.EntityId.Value, stoppingToken);
                        break;
                    case SeoMaintenanceJobKind.AllPublishedBlogs:
                        await RunAllPublishedBlogsAsync(stoppingToken);
                        break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "SEO maintenance job {Kind} failed", job.Kind);
                coordinator.UpdateSnapshot(coordinator.GetSnapshot() with
                {
                    IsRunning = false,
                    LastSummary = $"خطا در نگهداری SEO: {ex.Message}"
                });
            }
        }
    }

    async Task RunProductAsync(int id, CancellationToken ct)
    {
        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = true,
            CurrentTask = $"Auto-Fix محصول #{id}",
            Done = 0,
            Total = 1
        });

        await using var scope = services.CreateAsyncScope();
        var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
        var iterations = Math.Clamp(options.Value.BulkFixMaxIterations, 3, 8);
        await engine.AutoFixProductAsync(id, iterations, SeoProductContentMode.PreserveLocked, ct);

        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = false,
            Done = 1,
            Total = 1,
            LastSummary = $"محصول #{id} — Auto-Fix کامل شد.",
            LastCompletedAt = DateTimeOffset.UtcNow
        });
    }

    async Task RunBlogAsync(int id, CancellationToken ct)
    {
        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = true,
            CurrentTask = $"Auto-Fix مقاله #{id}",
            Done = 0,
            Total = 1
        });

        await using var scope = services.CreateAsyncScope();
        var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
        var iterations = Math.Clamp(options.Value.BulkFixMaxIterations, 3, 8);
        await engine.AutoFixBlogAsync(id, iterations, forceRegenerate: false, ct);

        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = false,
            Done = 1,
            Total = 1,
            LastSummary = $"مقاله #{id} — Auto-Fix کامل شد.",
            LastCompletedAt = DateTimeOffset.UtcNow
        });
    }

    async Task RunAllPublishedBlogsAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
        var iterations = Math.Clamp(options.Value.BulkFixMaxIterations, 3, 8);

        var blogIds = await db.BlogPosts.Where(b => b.IsPublished).OrderBy(b => b.Id).Select(b => b.Id).ToListAsync(ct);
        var total = blogIds.Count;
        var done = 0;
        var failed = 0;

        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = true,
            CurrentTask = "Auto-Fix مقالات منتشر‌شده",
            Done = 0,
            Total = total
        });

        foreach (var id in blogIds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await engine.AutoFixBlogAsync(id, iterations, forceRegenerate: false, ct);
            }
            catch (Exception ex)
            {
                failed++;
                log.LogWarning(ex, "SEO maintenance blog {Id} failed", id);
            }

            done++;
            coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
            {
                IsRunning = true,
                CurrentTask = $"مقاله {done}/{total}",
                Done = done,
                Total = total
            });
        }

        var summary = $"Auto-Fix مقالات: {total}" + (failed > 0 ? $" · {failed} خطا" : "");
        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = false,
            Done = total,
            Total = total,
            LastSummary = summary,
            LastCompletedAt = DateTimeOffset.UtcNow
        });
    }

    async Task RunCategoryAsync(int id, CancellationToken ct)
    {
        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = true,
            CurrentTask = $"Auto-Fix دسته #{id}",
            Done = 0,
            Total = 1
        });

        await using var scope = services.CreateAsyncScope();
        var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
        var iterations = Math.Clamp(options.Value.BulkFixMaxIterations, 3, 8);
        await engine.AutoFixCategoryAsync(id, iterations, ct);

        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = false,
            Done = 1,
            Total = 1,
            LastSummary = $"دسته #{id} — Auto-Fix کامل شد.",
            LastCompletedAt = DateTimeOffset.UtcNow
        });
    }

    async Task RunAllCategoriesAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
        var growth = scope.ServiceProvider.GetRequiredService<IGrowthEngineService>();
        var iterations = Math.Clamp(options.Value.BulkFixMaxIterations, 3, 8);

        var categoryIds = await db.Categories.Select(c => c.Id).ToListAsync(ct);
        var total = categoryIds.Count;
        var done = 0;
        var failed = 0;

        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = true,
            CurrentTask = "Auto-Fix همه دسته‌ها",
            Done = 0,
            Total = total
        });

        foreach (var id in categoryIds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await engine.AutoFixCategoryAsync(id, iterations, ct);
            }
            catch (Exception ex)
            {
                failed++;
                log.LogWarning(ex, "SEO maintenance category {Id} failed", id);
            }

            done++;
            coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
            {
                IsRunning = true,
                CurrentTask = $"دسته {done}/{total}",
                Done = done,
                Total = total
            });
        }

        try { await growth.RefreshActionQueueAsync(ct); }
        catch (Exception ex) { log.LogWarning(ex, "Growth queue refresh after category maintenance failed"); }

        var summary = $"Auto-Fix دسته‌ها: {total} دسته" + (failed > 0 ? $" · {failed} خطا" : "");
        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = false,
            Done = total,
            Total = total,
            LastSummary = summary,
            LastCompletedAt = DateTimeOffset.UtcNow
        });
    }

    async Task RunFullCatalogAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var engine = scope.ServiceProvider.GetRequiredService<ISeoAutoFixEngine>();
        var growth = scope.ServiceProvider.GetRequiredService<IGrowthEngineService>();
        var iterations = Math.Clamp(options.Value.BulkFixMaxIterations, 3, 8);

        var productIds = await db.Products.Select(p => p.Id).ToListAsync(ct);
        var categoryIds = await db.Categories.Select(c => c.Id).ToListAsync(ct);
        var total = productIds.Count + categoryIds.Count;
        var done = 0;
        var ready = 0;
        var failed = 0;

        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = true,
            CurrentTask = "Auto-Fix همه محصولات و دسته‌ها",
            Done = 0,
            Total = total
        });

        log.LogInformation("SEO full catalog maintenance started — {Products} products, {Categories} categories",
            productIds.Count, categoryIds.Count);

        foreach (var id in productIds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var report = await engine.AutoFixProductAsync(id, iterations, SeoProductContentMode.PreserveLocked, ct);
                if (report.IsPublishReady) ready++;
            }
            catch (Exception ex)
            {
                failed++;
                log.LogWarning(ex, "SEO maintenance product {Id} failed", id);
            }

            done++;
            coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
            {
                IsRunning = true,
                CurrentTask = $"محصول {done}/{total}",
                Done = done,
                Total = total
            });
        }

        foreach (var id in categoryIds)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await engine.AutoFixCategoryAsync(id, iterations, ct);
            }
            catch (Exception ex)
            {
                failed++;
                log.LogWarning(ex, "SEO maintenance category {Id} failed", id);
            }

            done++;
            coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
            {
                IsRunning = true,
                CurrentTask = $"دسته {done}/{total}",
                Done = done,
                Total = total
            });
        }

        try { await growth.RefreshActionQueueAsync(ct); }
        catch (Exception ex) { log.LogWarning(ex, "Growth queue refresh after SEO maintenance failed"); }

        var summary =
            $"نگهداری SEO: {productIds.Count} محصول + {categoryIds.Count} دسته · {ready} آماده انتشار"
            + (failed > 0 ? $" · {failed} خطا" : "");

        await WriteCatalogVersionMarkerAsync();

        coordinator.UpdateSnapshot(new SeoMaintenanceSnapshot
        {
            IsRunning = false,
            Done = total,
            Total = total,
            LastSummary = summary,
            LastCompletedAt = DateTimeOffset.UtcNow
        });

        log.LogInformation("SEO full catalog maintenance completed — {Summary}", summary);
    }

    async Task WriteCatalogVersionMarkerAsync()
    {
        var dir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "seo-catalog-version.txt");
        await File.WriteAllTextAsync(path, options.Value.CatalogVersion.Trim());
    }
}

/// <summary>پس از استارت، در صورت نیاز Full Catalog را در صف می‌گذارد (بدون بلاک کردن HTTP).</summary>
public sealed class SeoStartupMaintenanceService(
    ISeoMaintenanceCoordinator coordinator,
    IOptions<SeoMaintenanceOptions> options,
    IWebHostEnvironment env,
    ILogger<SeoStartupMaintenanceService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var opt = options.Value;
        if (!opt.RunFullCatalogOnStartup)
        {
            log.LogInformation("SEO startup maintenance disabled (Seo:Maintenance:RunFullCatalogOnStartup=false)");
            return;
        }

        var marker = Path.Combine(env.ContentRootPath, "App_Data", "seo-catalog-version.txt");
        var expected = opt.CatalogVersion.Trim();
        if (File.Exists(marker))
        {
            var current = (await File.ReadAllTextAsync(marker, cancellationToken)).Trim();
            if (string.Equals(current, expected, StringComparison.Ordinal))
            {
                log.LogInformation("SEO catalog already at version {Version} — skip startup maintenance", expected);
                return;
            }
        }

        log.LogInformation("SEO catalog version {Version} — queueing full maintenance", expected);
        await coordinator.RequestFullCatalogAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
