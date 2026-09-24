using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovasseghiShop.Web;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;
using MovasseghiShop.Web.Services.Editorial;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StudioController(
    ApplicationDbContext db,
    IGrowthEngineService growth,
    ISeoEngineService seo,
    IRankTrackerService rankTracker,
    IGoogleSearchConsoleService gsc,
    ISeoMaintenanceCoordinator seoMaintenance,
    IEditorialGrowthService editorialGrowth,
    IBlogEditorialAdminService blogEditorial,
    IOptions<EditorialGrowthOptions> editorialOptions) : Controller
{
    readonly EditorialGrowthOptions _editorialOpt = editorialOptions.Value;

    public Task<IActionResult> Index() => CommandCenter();

    /// <summary>نام view «CommandCenter» است؛ این alias برای لینک‌های قدیمی /Admin/Studio/CommandCenter.</summary>
    [ActionName("CommandCenter")]
    public async Task<IActionResult> CommandCenter()
    {
        var snap = seoMaintenance.GetSnapshot();
        if (!snap.IsRunning)
            await growth.RefreshActionQueueAsync();
        var model = await growth.GetDashboardAsync();
        ViewBag.SeoMaintenance = snap;
        return View("CommandCenter", model);
    }

    [HttpGet]
    public IActionResult MaintenanceStatus() => Json(seoMaintenance.GetSnapshot());

    [HttpGet]
    public async Task<IActionResult> DashboardLive(bool refreshQueue = false)
    {
        if (refreshQueue && !seoMaintenance.GetSnapshot().IsRunning)
            await growth.RefreshActionQueueAsync();
        var model = await growth.GetDashboardAsync();
        var maintenance = seoMaintenance.GetSnapshot();
        return Json(new
        {
            maintenance,
            stats = new
            {
                model.AverageReadiness,
                model.AverageScore,
                model.AverageCompetitive,
                model.PublishReadyCount,
                model.BlockedCount,
                model.OpenActions,
                model.AuditedCount,
                model.HasSerpData
            },
            topActions = model.TopActions.Select(a => new
            {
                a.Priority,
                a.Title,
                a.Description,
                a.EntityType,
                a.EntityId
            })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshQueue()
    {
        await growth.RefreshActionQueueAsync();
        TempData["Success"] = "صف کار به‌روز شد.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunEditorialGrowth()
    {
        var result = await editorialGrowth.RunDailyCycleAsync();
        TempData["Success"] =
            $"موتور رشد بلاگ: {result.DraftsCreated} پیش‌نویس، {result.PostsPublished} انتشار، {result.TopicsQueued} موضوع در صف. "
            + string.Join(" · ", result.Log.Take(3));
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditorialGrowthStatus() =>
        Json(await editorialGrowth.GetStatusAsync());

    /// <summary>پنل مدیریت موتور خودکار رشد بلاگ (SEO/GEO/AEO).</summary>
    public async Task<IActionResult> EditorialHub()
    {
        var status = await editorialGrowth.GetStatusAsync();
        var queue = await db.EditorialTopicQueue.AsNoTracking()
            .OrderByDescending(t => t.OpportunityScore)
            .ThenBy(t => t.Status)
            .Take(50)
            .ToListAsync();
        var pillars = SiteKeywordStrategy.Pillars.OrderBy(p => p.Priority).Take(12).ToList();
        var blogStats = new
        {
            Total = await db.BlogPosts.CountAsync(),
            Published = await db.BlogPosts.CountAsync(b => b.IsPublished),
            MissingKeywords = await db.BlogPosts.CountAsync(b => string.IsNullOrEmpty(b.MetaKeywords))
        };
        var inventory = await editorialGrowth.GetBlogInventoryAsync();
        var publishedToday = await EditorialHumanSchedule.CountPublishedTodayTehranAsync(db);
        var timeline = EditorialHumanSchedule.BuildPublishTimeline(
            _editorialOpt, status.NextScheduledUtc, publishedToday);

        ViewBag.Status = status;
        ViewBag.Queue = queue;
        ViewBag.Pillars = pillars;
        ViewBag.Options = _editorialOpt;
        ViewBag.BlogStats = blogStats;
        ViewBag.Inventory = inventory;
        ViewBag.Timeline = timeline;
        ViewBag.PublishedToday = publishedToday;
        ViewData["HeroTitle"] = "موتور رشد خودکار بلاگ";
        ViewData["HeroSubtitle"] = "زمان‌بندی انتشار، کیفیت SEO و تولید از pillarهای سایت";
        ViewData["HeroBadge"] = _editorialOpt.Enabled ? "فعال" : "غیرفعال";
        return View("EditorialHub");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditorialQualitySweep()
    {
        var result = await editorialGrowth.SweepLowQualityAndProduceAsync();
        TempData["Success"] =
            $"پاکسازی: {result.Purged} مقاله زیر {_editorialOpt.MinPublishDisplayScore} · "
            + $"تولید آماده: {result.ProducedReady}/{result.TargetCount}. "
            + string.Join(" · ", result.Log.Take(6));
        return RedirectToAction(nameof(EditorialHub));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BackfillBlogKeywords()
    {
        var n = await blogEditorial.BackfillAllBlogKeywordsAsync();
        TempData["Success"] = $"کلیدواژه {n} مقاله از pillar + GSC پر شد.";
        return RedirectToAction(nameof(EditorialHub));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunEditorialGrowthFromHub()
    {
        var result = await editorialGrowth.RunDailyCycleAsync();
        TempData["Success"] =
            $"چرخه رشد: {result.DraftsCreated} پیش‌نویس، {result.PostsPublished} انتشار، {result.TopicsQueued} موضوع جدید در صف. "
            + string.Join(" · ", result.Log);
        return RedirectToAction(nameof(EditorialHub));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshEditorialQueueFromHub()
    {
        var added = await editorialGrowth.RefreshTopicQueueAsync();
        TempData["Success"] = $"صف موضوعات: {added} مورد جدید (pillar + GSC + رتبه ۴–۲۰).";
        return RedirectToAction(nameof(EditorialHub));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RunEditorialFullMaint()
    {
        var sweep = await editorialGrowth.SweepLowQualityAndProduceAsync();
        var queued = await editorialGrowth.RefreshTopicQueueAsync();
        var cycle = await editorialGrowth.RunDailyCycleAsync();
        TempData["Success"] =
            $"نگهداری کامل: حذف {sweep.Purged} · تولید {sweep.ProducedReady}/{sweep.TargetCount} · صف +{queued} · "
            + $"چرخه: {cycle.DraftsCreated} پیش‌نویس، {cycle.PostsPublished} انتشار.";
        return RedirectToAction(nameof(EditorialHub));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoFixProduct(int id, string? returnUrl = null)
    {
        var wasActive = await db.Products.Where(p => p.Id == id).Select(p => p.IsActive).FirstOrDefaultAsync();

        var score = await seo.AutoFixProductAsync(id);

        var product = await db.Products.FindAsync(id);
        if (product != null)
        {
            if (wasActive || score.IsPublishReady)
                product.IsActive = true;
            await db.SaveChangesAsync();
        }

        var manual = score.Ultimate?.ManualTasks.Count ?? 0;
        TempData["Success"] = score.IsPublishReady
            ? $"Auto-Fix Ultimate اعمال شد — امتیاز {score.DisplayTotal}/100 · قدرت رقابتی {score.CompetitiveStrength} · "
              + $"{SeoPublishGateLabels.ReadinessLabel} {score.RankingReadiness}/100 ({SeoPublishGateLabels.ReadinessVerdict(score.RankingReadiness)})."
              + (manual > 0 ? $" {manual} کار باقی‌مانده نیازمند شما است — پنل SEO را ببینید." : "")
            : $"Auto-Fix Ultimate اعمال شد — امتیاز نمایشی {score.DisplayTotal}/100 · {SeoPublishGateLabels.ReadinessLabel} {score.RankingReadiness}/100 · "
              + $"قدرت رقابتی {score.CompetitiveStrength}/100. "
              + SeoPublishGateLabels.AutoFixFollowUpMessage(score);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(AppPath.H(returnUrl, HttpContext));
        return RedirectToAction("Edit", "Products", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoFixBlog(int id, string? returnUrl = null)
    {
        await ClearEditorialLocksAsync("blog", id);
        var score = await seo.AutoFixBlogAsync(id);
        TempData["Success"] = score.IsPublishReady
            ? $"درست‌سازی SEO مقاله انجام شد — امتیاز نمایشی {score.DisplayTotal}/100 · {SeoPublishGateLabels.Verified}"
            : $"درست‌سازی SEO انجام شد — امتیاز نمایشی {score.DisplayTotal}/100. {SeoPublishGateLabels.AutoFixFollowUpMessage(score)}";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(AppPath.H(returnUrl, HttpContext));
        return RedirectToAction("Edit", "BlogAdmin", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoFixNews(int id, string? returnUrl = null)
    {
        await ClearEditorialLocksAsync("news", id);
        var score = await seo.AutoFixNewsAsync(id);
        TempData["Success"] = score.IsPublishReady
            ? $"درست‌سازی SEO خبر انجام شد — امتیاز نمایشی {score.DisplayTotal}/100 · {SeoPublishGateLabels.Verified}"
            : $"درست‌سازی SEO انجام شد — امتیاز نمایشی {score.DisplayTotal}/100. {SeoPublishGateLabels.AutoFixFollowUpMessage(score)}";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(AppPath.H(returnUrl, HttpContext));
        return RedirectToAction("Edit", "NewsAdmin", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoFixCategory(int id, string? returnUrl = null)
    {
        var profile = await db.SeoProfiles
            .FirstOrDefaultAsync(s => s.EntityType == "category" && s.EntityId == id);
        if (profile != null)
        {
            profile.LockCategoryLead = false;
            await db.SaveChangesAsync();
        }

        var score = await seo.AutoFixCategoryAsync(id);
        TempData["Success"] = score.IsPublishReady
            ? $"SEO دسته اعمال شد — امتیاز {score.TotalScore}/100 · {SeoPublishGateLabels.ReadinessLabel} {score.RankingReadiness}/100 "
              + $"({SeoPublishGateLabels.ReadinessVerdict(score.RankingReadiness)})"
            : $"SEO دسته اعمال شد — امتیاز نمایشی {score.DisplayTotal}/100 · {SeoPublishGateLabels.ReadinessLabel} {score.RankingReadiness}/100. "
              + SeoPublishGateLabels.AutoFixFollowUpMessage(score);
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(AppPath.H(returnUrl, HttpContext));
        return RedirectToAction("Edit", "Categories", new { id });
    }

    async Task ClearEditorialLocksAsync(string entityType, int entityId)
    {
        var profile = await db.SeoProfiles
            .FirstOrDefaultAsync(s => s.EntityType == entityType && s.EntityId == entityId);
        if (profile is null) return;
        if (entityType is "blog" or "news")
        {
            profile.LockArticleExcerpt = false;
            profile.LockArticleContent = false;
        }
        await db.SaveChangesAsync();
    }

    [HttpGet]
    public async Task<IActionResult> ProductScore(int id)
    {
        var result = await seo.AuditProductAsync(id);
        return Json(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAutoFix()
    {
        var snap = seoMaintenance.GetSnapshot();
        if (snap.IsRunning)
        {
            TempData["Success"] = "نگهداری SEO هم‌اکنون در حال اجراست — لطفاً چند دقیقه صبر کنید.";
            return RedirectToAction(nameof(Index));
        }

        await seoMaintenance.RequestFullCatalogAsync();
        TempData["Success"] =
            "نگهداری SEO در پس‌زمینه شروع شد (محصولات + دسته‌ها — حالت امن: توضیحات قفل‌شده بازنویسی نمی‌شود). "
            + "پیشرفت و آمار همین صفحه خودکار به‌روز می‌شود.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAutoFixCategories()
    {
        var snap = seoMaintenance.GetSnapshot();
        if (snap.IsRunning)
        {
            TempData["Success"] = "نگهداری SEO هم‌اکنون در حال اجراست — پیشرفت را در همین صفحه می‌بینید.";
            return RedirectToAction(nameof(Index));
        }

        await seoMaintenance.RequestAllCategoriesAsync();
        TempData["Success"] = "Auto-Fix دسته‌ها در پس‌زمینه شروع شد — پیشرفت به‌صورت خودکار به‌روز می‌شود.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAutoFixBlogs()
    {
        var snap = seoMaintenance.GetSnapshot();
        if (snap.IsRunning)
        {
            TempData["Success"] = "نگهداری SEO هم‌اکنون در حال اجراست — پیشرفت را در همین صفحه می‌بینید.";
            return RedirectToAction(nameof(Index));
        }

        await seoMaintenance.RequestAllPublishedBlogsAsync();
        TempData["Success"] =
            "Auto-Fix مقالات منتشر‌شده در پس‌زمینه شروع شد (SERP Gap + AEO/GEO). پیشرفت خودکار به‌روز می‌شود.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshEditorialQueue()
    {
        var added = await editorialGrowth.RefreshTopicQueueAsync();
        TempData["Success"] = $"صف موضوعات بلاگ به‌روز شد — {added} موضوع جدید.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> RankRadar()
    {
        var model = await rankTracker.GetRadarAsync();
        ViewBag.Gsc = await gsc.GetTopQueriesAsync();
        ViewBag.GscConfigured = gsc.IsConfigured;
        ViewBag.GscProperty = gsc.PropertyUrl;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordRank(string keyword, int position, string keywordType = "pillar", int? entityId = null, string? targetUrl = null)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            TempData["Success"] = "کلمه کلیدی الزامی است.";
            return RedirectToAction(nameof(RankRadar));
        }
        await rankTracker.RecordSnapshotAsync(keyword.Trim(), position, keywordType, entityId, targetUrl);
        TempData["Success"] = $"رتبه «{keyword}» ثبت شد.";
        return RedirectToAction(nameof(RankRadar));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncGsc()
    {
        var result = await gsc.SyncTopQueriesAsync();
        TempData[result.Success ? "Success" : "Error"] = result.Message ?? "GSC";
        return RedirectToAction(nameof(RankRadar));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncRankApi()
    {
        var result = await rankTracker.SyncFromApiAsync();
        TempData["Success"] = result.Message ?? "همگام‌سازی انجام شد.";
        return RedirectToAction(nameof(RankRadar));
    }
}
