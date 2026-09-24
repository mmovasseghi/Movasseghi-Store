using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Enums;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController(
    ApplicationDbContext db,
    IAmelonImportService importer,
    IProductImageLocalizerService imageLocalizer,
    IAnalyticsReportService reports) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.OrdersCount = await db.Orders.CountAsync(ct);
        ViewBag.ProductsCount = await db.Products.CountAsync(ct);
        ViewBag.InquiriesCount = await db.PurchaseInquiries.CountAsync(i => !i.IsHandled, ct);
        ViewBag.PendingOrders = await db.Orders.CountAsync(o =>
            o.Status != OrderStatus.Cancelled && o.OperatorFollowedUpAt == null, ct);
        ViewBag.RecentOrders = await db.Orders.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt).Take(8).ToListAsync(ct);
        ViewBag.OpenInquiries = await db.PurchaseInquiries.AsNoTracking()
            .Include(i => i.Product)
            .Where(i => !i.IsHandled)
            .OrderByDescending(i => i.CreatedAt)
            .Take(6)
            .ToListAsync(ct);
        ViewBag.UsersCount = await db.Users.CountAsync(ct);
        var seoProfiles = await db.SeoProfiles.AsNoTracking()
            .Where(s => s.EntityType == "product")
            .ToListAsync(ct);
        ViewBag.SeoReady = seoProfiles.Count(s => s.IsPublishReady);
        var blocked = seoProfiles.Count(s => !s.IsPublishReady && s.TotalScore > 0);
        var noProfile = (int)ViewBag.ProductsCount - seoProfiles.Count;
        ViewBag.SeoWeak = blocked + noProfile;
        ViewBag.TopKeywords = await db.RankSnapshots.AsNoTracking()
            .OrderByDescending(r => r.CheckedAt)
            .Take(10)
            .ToListAsync(ct);
        ViewBag.ActiveProducts = await db.Products.CountAsync(p => p.IsActive, ct);
        ViewBag.Overview = await reports.GetOverviewAsync(7, ct);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportAmelon(CancellationToken ct)
    {
        try
        {
            var count = await importer.ImportAllAsync(ct);
            if (count > 0)
            {
                var imgResult = await imageLocalizer.LocalizeAllAsync(ct);
                TempData["Success"] = $"{count} محصول وارد شد — {imgResult.ImagesDownloaded} تصویر محلی ذخیره شد.";
                if (imgResult.Failures.Count > 0)
                    TempData["Error"] = $"{imgResult.Failures.Count} تصویر دانلود نشد (گزارش: App_Data/image-localization-report.json)";
            }
            else
            {
                TempData["Success"] = "Import انجام نشد — اتصال شبکه یا فایل cache را بررسی کنید.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"خطا در import: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LocalizeImages(CancellationToken ct)
    {
        try
        {
            var result = await imageLocalizer.LocalizeAllAsync(ct);
            TempData["Success"] = $"{result.ImagesDownloaded} تصویر دانلود شد ({result.WebpGenerated} WebP).";
            if (result.Failures.Count > 0)
                TempData["Error"] = $"{result.Failures.Count} مورد ناموفق — گزارش در App_Data/image-localization-report.json";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"خطا در دانلود تصاویر: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
