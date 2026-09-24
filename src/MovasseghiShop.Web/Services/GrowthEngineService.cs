using Microsoft.EntityFrameworkCore;

using MovasseghiShop.Web.Data;

using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;



public interface IGrowthEngineService

{

    Task EnsurePillarKeywordsAsync(CancellationToken ct = default);

    Task RefreshActionQueueAsync(CancellationToken ct = default);

    Task<GrowthDashboardModel> GetDashboardAsync(CancellationToken ct = default);

}



public class GrowthDashboardModel

{

    public int TotalProducts { get; set; }

    public int PublishReadyCount { get; set; }

    public int BlockedCount { get; set; }

    public int OpenActions { get; set; }

    public double AverageScore { get; set; }

    /// <summary>میانگین آمادگی رتبه‌گیری — به‌جای وعده «رتبه ۱».</summary>

    public double AverageReadiness { get; set; }

    public double AverageCompetitive { get; set; }

    public int AuditedCount { get; set; }

    public int TotalBlogPosts { get; set; }

    public int BlogPublishReadyCount { get; set; }

    public int BlogBlockedCount { get; set; }

    public bool HasSerpData { get; set; }

    public List<GrowthAction> TopActions { get; set; } = [];

    public List<PillarKeyword> Pillars { get; set; } = [];

}



public class GrowthEngineService(ApplicationDbContext db, ISeoEngineService seo) : IGrowthEngineService

{

    public async Task EnsurePillarKeywordsAsync(CancellationToken ct = default)

    {

        var existing = await db.PillarKeywords.ToListAsync(ct);

        var strategyPhrases = SiteKeywordStrategy.Pillars.Select(p => p.Phrase).ToHashSet();



        foreach (var def in SiteKeywordStrategy.Pillars)

        {

            var row = existing.FirstOrDefault(p => p.Phrase == def.Phrase);

            if (row == null)

            {

                db.PillarKeywords.Add(new PillarKeyword

                {

                    Phrase = def.Phrase,

                    Cluster = def.Cluster,

                    Priority = def.Priority,

                    OwnerPageKey = def.OwnerPageKey,

                    SearchIntent = def.SearchIntent,

                    IsActive = true

                });

            }

            else

            {

                row.Priority = def.Priority;

                row.Cluster = def.Cluster;

                row.OwnerPageKey = def.OwnerPageKey;

                row.SearchIntent = def.SearchIntent;

                row.IsActive = true;

            }

        }



        foreach (var old in existing.Where(p => !strategyPhrases.Contains(p.Phrase)))

            old.IsActive = false;



        await db.SaveChangesAsync(ct);

    }



    public async Task RefreshActionQueueAsync(CancellationToken ct = default)

    {

        var products = await db.Products

            .Include(p => p.Images)

            .Include(p => p.Category)

            .Where(p => p.IsActive)

            .ToListAsync(ct);



        var openProductActions = await db.GrowthActions

            .Where(a => a.Status == "open" && a.ActionType == "seo_fix" && a.EntityType == "product")

            .ToListAsync(ct);



        foreach (var product in products)

        {

            var result = await seo.AuditProductAsync(product.Id, ct);

            var open = openProductActions.FirstOrDefault(a => a.EntityId == product.Id);



            if (result.IsPublishReady || result.Issues.Count == 0)

            {

                if (open is not null)

                {

                    open.Status = "closed";

                    open.CompletedAt = DateTime.UtcNow;

                    open.Description = "مشکل برطرف شد";

                }

                continue;

            }



            var description = result.Issues.FirstOrDefault()
                ?? $"{SeoPublishGateLabels.ReadinessLabel} {result.RankingReadiness}/100 · امتیاز نمایشی {result.DisplayTotal}/100";

            var priority = result.RankingReadiness < 60 || result.DisplayTotal < 70 ? "high" : "medium";



            if (open is not null)

            {

                open.Description = description;

                open.Priority = priority;

                open.Title = $"SEO محصول: {product.Name}";

                continue;

            }



            db.GrowthActions.Add(new GrowthAction

            {

                ActionType = "seo_fix",

                Priority = priority,

                Title = $"SEO محصول: {product.Name}",

                Description = description,

                EntityType = "product",

                EntityId = product.Id,

                Status = "open"

            });

        }



        // بستن کارهای محصولاتی که غیرفعال شده‌اند

        var activeIds = products.Select(p => p.Id).ToHashSet();

        foreach (var stale in openProductActions.Where(a => a.EntityId is int id && !activeIds.Contains(id)))

        {

            stale.Status = "closed";

            stale.CompletedAt = DateTime.UtcNow;

            stale.Description = "محصول غیرفعال شد";

        }



        var missingPages = new[] { ("wholesale", "صفحه پخش عمده"), ("pricing", "صفحه قیمت"), ("amelon", "صفحه آملون") };

        var openPageActions = await db.GrowthActions

            .Where(a => a.Status == "open" && a.ActionType == "create_page")

            .ToListAsync(ct);



        foreach (var (key, title) in missingPages)

        {

            var exists = await db.CmsPages.AnyAsync(p => p.Key == key, ct);

            var open = openPageActions.FirstOrDefault(a => a.Title.Contains(title, StringComparison.Ordinal));



            if (exists)

            {

                if (open is not null)

                {

                    open.Status = "closed";

                    open.CompletedAt = DateTime.UtcNow;

                }

                continue;

            }



            if (open is not null) continue;



            db.GrowthActions.Add(new GrowthAction

            {

                ActionType = "create_page",

                Priority = "high",

                Title = $"بساز: {title}",

                Description = $"برای پوشش keyword pillar — key={key}",

                Status = "open"

            });

        }



        var blogPosts = await db.BlogPosts.AsNoTracking().ToListAsync(ct);
        var openBlogActions = await db.GrowthActions
            .Where(a => a.Status == "open" && a.ActionType == "seo_fix" && a.EntityType == "blog")
            .ToListAsync(ct);

        foreach (var post in blogPosts)
        {
            var result = await seo.AuditBlogAsync(post.Id, ct);
            var open = openBlogActions.FirstOrDefault(a => a.EntityId == post.Id);

            if (result.IsPublishReady)
            {
                if (open is not null)
                {
                    open.Status = "closed";
                    open.CompletedAt = DateTime.UtcNow;
                    open.Description = "مشکل برطرف شد";
                }
                continue;
            }

            var description = result.Issues.FirstOrDefault()
                ?? $"{SeoPublishGateLabels.ReadinessLabel} {result.RankingReadiness}/100 · امتیاز نمایشی {result.DisplayTotal}/100";
            var priority = result.RankingReadiness < 60 || result.DisplayTotal < 70 ? "high" : "medium";

            if (open is not null)
            {
                open.Description = description;
                open.Priority = priority;
                open.Title = $"SEO مقاله: {post.Title}";
                continue;
            }

            db.GrowthActions.Add(new GrowthAction
            {
                ActionType = "seo_fix",
                Priority = priority,
                Title = $"SEO مقاله: {Truncate(post.Title, 60)}",
                Description = description,
                EntityType = "blog",
                EntityId = post.Id,
                Status = "open"
            });
        }

        var blogIds = blogPosts.Select(b => b.Id).ToHashSet();
        foreach (var stale in openBlogActions.Where(a => a.EntityId is int id && !blogIds.Contains(id)))
        {
            stale.Status = "closed";
            stale.CompletedAt = DateTime.UtcNow;
            stale.Description = "مقاله حذف شد";
        }

        await db.SaveChangesAsync(ct);

    }

    static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    public async Task<GrowthDashboardModel> GetDashboardAsync(CancellationToken ct = default)

    {

        await EnsurePillarKeywordsAsync(ct);



        var profiles = await db.SeoProfiles.Where(p => p.EntityType == "product").ToListAsync(ct);

        var total = await db.Products.CountAsync(ct);

        var ready = profiles.Count(p => p.IsPublishReady);

        var blocked = profiles.Count(p => !p.IsPublishReady && p.TotalScore > 0);

        var noProfile = total - profiles.Count;



        var actions = await db.GrowthActions

            .Where(a => a.Status == "open")

            .OrderByDescending(a => a.Priority == "high")

            .ThenBy(a => a.CreatedAt)

            .Take(12)

            .ToListAsync(ct);



        var pillars = await db.PillarKeywords

            .Where(p => p.IsActive)

            .OrderBy(p => p.Priority)

            .ToListAsync(ct);

        var blogProfiles = await db.SeoProfiles.Where(p => p.EntityType == "blog").ToListAsync(ct);
        var totalBlogs = await db.BlogPosts.CountAsync(ct);
        var blogReady = blogProfiles.Count(p => p.IsPublishReady);
        var blogBlocked = blogProfiles.Count(p => !p.IsPublishReady && p.TotalScore > 0);



        return new GrowthDashboardModel

        {

            TotalProducts = total,

            PublishReadyCount = ready,

            BlockedCount = blocked + noProfile,

            TotalBlogPosts = totalBlogs,

            BlogPublishReadyCount = blogReady,

            BlogBlockedCount = blogBlocked + Math.Max(0, totalBlogs - blogProfiles.Count),

            OpenActions = actions.Count,

            AverageScore = profiles.Count > 0
                ? profiles.Average(p => SeoUltimateAnalyzer.ResolveDisplayOverallFromProfile(p))
                : 0,

            AverageReadiness = profiles.Count > 0 ? profiles.Average(p => p.RankingReadiness) : 0,

            AverageCompetitive = profiles.Count > 0 ? profiles.Average(p => p.CompetitiveStrength) : 0,

            AuditedCount = profiles.Count(p => p.LastAuditedAt != null),

            HasSerpData = await db.CompetitorSnapshots.AnyAsync(ct),

            TopActions = actions,

            Pillars = pillars

        };

    }

}


