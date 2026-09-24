using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReportsController(IAnalyticsReportService reports) : Controller
{
    // بازه‌های مجاز — جلوگیری از پرس‌وجوی سنگین با عدد دلخواه در URL
    static readonly int[] AllowedRanges = [1, 7, 14, 30, 90, 180, 365];

    static int Normalize(int? days) =>
        days is int d && AllowedRanges.Contains(d) ? d : 30;

    public async Task<IActionResult> Index(int? days)
    {
        var range = Normalize(days);
        ViewData["Title"] = "گزارش‌ها و تحلیل";
        ViewBag.Days = range;
        ViewBag.Ranges = AllowedRanges;
        return View(await reports.GetOverviewAsync(range, HttpContext.RequestAborted));
    }

    public async Task<IActionResult> Traffic(int? days)
    {
        var range = Normalize(days);
        ViewData["Title"] = "ترافیک و صفحات";
        ViewBag.Days = range;
        ViewBag.Ranges = AllowedRanges;
        return View(await reports.GetTrafficAsync(range, HttpContext.RequestAborted));
    }

    public async Task<IActionResult> Sales(int? days)
    {
        var range = Normalize(days);
        ViewData["Title"] = "فروش و درآمد";
        ViewBag.Days = range;
        ViewBag.Ranges = AllowedRanges;
        return View(await reports.GetSalesAsync(range, HttpContext.RequestAborted));
    }

    /// <summary>خروجی CSV صفحات پربازدید — برای تحلیل در اکسل.</summary>
    public async Task<IActionResult> ExportPages(int? days)
    {
        var report = await reports.GetTrafficAsync(Normalize(days), HttpContext.RequestAborted);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("مسیر,نوع صفحه,بازدید,نشست,میانگین پاسخ سرور (ms)");
        foreach (var p in report.TopPages)
            csv.AppendLine($"\"{p.Path}\",{p.FaPageType},{p.Views},{p.Sessions},{p.AvgServerMs}");

        // BOM لازم است وگرنه اکسل فارسی را خراب نشان می‌دهد
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
            .ToArray();

        return File(bytes, "text/csv", $"pages-{DateTime.Now:yyyy-MM-dd}.csv");
    }
}
