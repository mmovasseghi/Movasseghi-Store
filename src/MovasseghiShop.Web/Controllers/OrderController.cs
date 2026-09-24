using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Controllers;

public class OrderController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Success(string number)
    {
        var order = await LoadOrderByNumberAsync(number);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Track(string? orderNumber, string? phone)
    {
        ViewBag.TrackOrderNumber = orderNumber?.Trim();
        ViewBag.TrackPhone = phone;

        if (!string.IsNullOrWhiteSpace(orderNumber) && !string.IsNullOrWhiteSpace(phone))
        {
            var order = await FindTrackableOrderAsync(phone, orderNumber);
            if (order != null)
                return View("TrackResult", order);
            ViewBag.TrackError = "سفارشی با این شماره سفارش و موبایل پیدا نشد. اعداد را دوباره بررسی کنید.";
        }
        else if (TempData["TrackError"] is string err)
        {
            ViewBag.TrackError = err;
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TrackPost(string phone, string orderNumber)
    {
        ViewBag.TrackOrderNumber = orderNumber?.Trim();
        ViewBag.TrackPhone = phone;

        var order = await FindTrackableOrderAsync(phone, orderNumber);
        if (order == null)
        {
            ViewBag.TrackError = "سفارشی با این شماره سفارش و موبایل پیدا نشد. اعداد را دوباره بررسی کنید.";
            return View("Track");
        }

        return View("TrackResult", order);
    }

    [Authorize]
    public async Task<IActionResult> Details(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var number = (id ?? "").Trim();
        if (string.IsNullOrEmpty(number)) return NotFound();

        var userPhone = MockOtpService.NormalizePhone(User.Identity?.Name ?? "");
        var order = await db.Orders
            .Include(o => o.Items)
            .Include(o => o.VehicleInfo)
            .Include(o => o.DeliveryAddress)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == number);
        if (order == null) return NotFound();
        if (!OrderBelongsToUser(order, userId, userPhone)) return NotFound();
        ViewBag.IsAccountOrderView = true;
        return View("TrackResult", order);
    }

    /// <summary>پیش‌فاکتور برای مهمان — نیاز به شماره سفارش + موبایل ثبت‌شده.</summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Invoice(string number, string phone)
    {
        var order = await FindTrackableOrderAsync(phone, number);
        if (order == null) return NotFound();
        return View(await BuildProformaViewModelAsync(order));
    }

    /// <summary>پیش‌فاکتور برای کاربر واردشده — بدون ارسال موبایل در URL.</summary>
    [Authorize]
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> MyInvoice(string number)
    {
        var order = await LoadOrderByNumberAsync(number);
        if (order == null) return NotFound();
        if (!UserOwnsOrder(order)) return Forbid();
        return View("Invoice", await BuildProformaViewModelAsync(order));
    }

    async Task<ProformaInvoiceViewModel> BuildProformaViewModelAsync(Order order) =>
        new()
        {
            Order = order,
            Analytics = await CartAnalyticsHelper.TryBuildSummaryFromOrderAsync(db, order)
        };

    bool UserOwnsOrder(Order order)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return false;
        var userPhone = MockOtpService.NormalizePhone(User.Identity?.Name ?? "");
        return OrderBelongsToUser(order, userId, userPhone);
    }

    static bool OrderBelongsToUser(Order order, string userId, string normalizedUserPhone)
    {
        if (!string.IsNullOrEmpty(order.UserId) && order.UserId == userId)
            return true;
        if (string.IsNullOrEmpty(normalizedUserPhone)) return false;
        return MockOtpService.NormalizePhone(order.Phone) == normalizedUserPhone;
    }

    async Task<Order?> LoadOrderByNumberAsync(string? orderNumber)
    {
        var number = (orderNumber ?? "").Trim();
        if (string.IsNullOrEmpty(number)) return null;
        return await db.Orders
            .Include(o => o.Items)
            .Include(o => o.VehicleInfo)
            .Include(o => o.DeliveryAddress)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == number);
    }

    async Task<Order?> FindTrackableOrderAsync(string? phone, string? orderNumber)
    {
        var normalizedPhone = MockOtpService.NormalizePhone(phone ?? "");
        var number = (orderNumber ?? "").Trim();
        if (string.IsNullOrEmpty(normalizedPhone) || string.IsNullOrEmpty(number))
            return null;

        var order = await LoadOrderByNumberAsync(number);
        if (order == null) return null;

        var orderPhone = MockOtpService.NormalizePhone(order.Phone);
        return orderPhone == normalizedPhone ? order : null;
    }
}
