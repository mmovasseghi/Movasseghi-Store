using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MovasseghiShop.Web.Models.Enums;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Controllers;

public class CheckoutController(
    ICartService cartService,
    IOrderService orderService,
    ICheckoutHumanVerificationService humanVerification,
    IPaymentGateway paymentGateway,
    IAnalyticsTracker analytics) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (!humanVerification.IsVerified(HttpContext))
            return RedirectToAction(nameof(Index), "Cart");

        var cart = await cartService.GetCartAsync(HttpContext);
        if (cart.Items.Count == 0) return RedirectToAction("Index", "Cart");
        if (!cart.MeetsMinimum)
        {
            // ریزش زیر حداقل سفارش خودش یک سیگنال است — شرط عمده بازدارنده است؟
            analytics.TrackEvent("below_minimum", HttpContext,
                label: $"{cart.SubTotal:N0}", value: cart.SubTotal);
            ViewBag.BelowMinimum = true;
            return View("BelowMinimum", cart);
        }

        analytics.TrackEvent("begin_checkout", HttpContext,
            label: $"{cart.Items.Count} قلم",
            value: cart.TotalAfterVolumeDiscount,
            quantity: cart.Items.Count);
        ViewBag.CouponCode = await cartService.GetCouponCodeAsync(HttpContext);
        var couponCode = ViewBag.CouponCode as string;
        if (!string.IsNullOrEmpty(couponCode))
        {
            var coupon = await orderService.ValidateCouponAsync(couponCode, cart);
            ViewBag.CouponDiscount = coupon != null ? orderService.CalculateDiscount(coupon, cart.TotalAfterVolumeDiscount) : 0m;
        }
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("checkout-human")]
    public async Task<IActionResult> PlaceOrder(CheckoutInput input)
    {
        var (order, error) = await orderService.CreateOrderAsync(HttpContext, input);
        if (error != null)
        {
            // خطای ثبت سفارش ثبت می‌شود تا علت ریزش پله آخر قیف معلوم شود
            analytics.TrackEvent("checkout_error", HttpContext, label: error);
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        analytics.TrackEvent("purchase", HttpContext,
            label: order!.OrderNumber,
            entityId: order.Id,
            entityType: "order",
            value: order.TotalAmount,
            quantity: order.Items?.Count ?? 0);

        if (order.PaymentMethod == PaymentMethod.Online)
        {
            var result = await paymentGateway.InitiateAsync(order.TotalAmount, order.OrderNumber,
                Url.Action("Verify", "Payment", null, Request.Scheme)!);
            if (result.Success && result.PaymentUrl != null)
            {
                HttpContext.Session.SetString($"pay_{order.OrderNumber}", result.Authority ?? "");
                return Redirect(result.PaymentUrl);
            }
        }

        return RedirectToAction("Success", "Order", new { number = order.OrderNumber });
    }
}
