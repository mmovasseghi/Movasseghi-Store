using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Controllers;

public class CartController(
    ICartService cartService,
    IOrderService orderService,
    IStudioContentService studio,
    ICheckoutHumanVerificationService humanVerification,
    IAnalyticsTracker analytics) : Controller
{
    private async Task<CartSummary> LoadCartAsync()
    {
        ViewBag.CartEmpty = await studio.GetAtomAsync("cart-empty");
        var cart = await cartService.GetCartAsync(HttpContext);
        var couponCode = await cartService.GetCouponCodeAsync(HttpContext);
        if (!string.IsNullOrEmpty(couponCode))
        {
            var coupon = await orderService.ValidateCouponAsync(couponCode, cart);
            ViewBag.Coupon = coupon;
            ViewBag.CouponDiscount = coupon != null ? orderService.CalculateDiscount(coupon, cart.TotalAfterVolumeDiscount) : 0m;
        }
        return cart;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.HumanVerified = humanVerification.IsVerified(HttpContext);
        ViewBag.HumanChallenge = ViewBag.HumanVerified is true
            ? null
            : humanVerification.CreateChallenge(HttpContext);
        ViewBag.HumanCheckFail = TempData["HumanCheckFail"] as bool? == true;
        ViewBag.HumanCheckOk = TempData["HumanCheckOk"] as bool? == true;
        return View(await LoadCartAsync());
    }

    [HttpGet]
    [Route("Cart/HumanCheck")]
    [Route("Cart/VerifyHuman")]
    [Route("Cart/SubmitHumanCheck")]
    public IActionResult HumanCheckRedirect() => RedirectToAction(nameof(Index));

    [HttpPost]
    [Route("Cart/HumanCheck")]
    [Route("Cart/VerifyHuman")]
    [Route("Cart/SubmitHumanCheck")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("checkout-human")]
    public IActionResult HumanCheck(string token, string optionId)
    {
        var ok = humanVerification.ValidateAndGrant(HttpContext, token, optionId);
        if (WantsHumanCheckJson())
            return Json(new { ok, verified = ok });

        if (ok)
            TempData["HumanCheckOk"] = true;
        else
            TempData["HumanCheckFail"] = true;
        return RedirectToAction(nameof(Index));
    }

    bool WantsHumanCheckJson()
    {
        if (Request.Headers.TryGetValue("Accept", out var accept)
            && accept.Any(v => v.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
            return true;
        return string.Equals(Request.Query["ajax"], "true", StringComparison.OrdinalIgnoreCase);
    }

    [HttpPost]
    [Route("Cart/RefreshHumanChallenge")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("checkout-human")]
    public IActionResult RefreshHumanChallenge()
    {
        var challenge = humanVerification.CreateChallenge(HttpContext);
        return Json(new
        {
            token = challenge.Token,
            prompt = challenge.Prompt,
            options = challenge.Options.Select(o => new { o.Id, o.Label, o.Emoji })
        });
    }

    [HttpGet]
    public IActionResult Count() => Json(new { count = cartService.GetItemCount(HttpContext) });

    [HttpGet]
    public async Task<IActionResult> Panel()
    {
        return PartialView("_CartPanel", await LoadCartAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int variantId, int cartons = 1, bool ajax = false, bool confirm = false)
    {
        await cartService.AddItemAsync(HttpContext, variantId, cartons);
        var cart = await LoadCartAsync();
        var item = cart.Items.FirstOrDefault(x => x.VariantId == variantId);

        if (item != null)
            analytics.TrackEvent("add_to_cart", HttpContext,
                label: item.ProductName,
                entityId: item.ProductId,
                entityType: "product",
                value: item.UnitPrice * cartons,
                quantity: cartons);

        if (ajax)
        {
            if (item == null) return BadRequest();
            if (confirm) return PartialView("_CartAddConfirm", BuildAddResult(item, cartons, cart));
            return PartialView("_CartPanel", cart);
        }

        if (item == null) return RedirectToAction(nameof(Index));
        return RedirectToAction(nameof(Index));
    }

    static CartAddResult BuildAddResult(CartItem item, int addedQty, CartSummary cart) => new()
    {
        ProductName = item.ProductName,
        VariantLabel = item.VariantLabel,
        AddedQuantity = addedQty,
        UnitName = item.UnitName,
        AddedLineTotal = item.UnitPrice * addedQty,
        CartItemCount = cart.Items.Count,
        CartSubTotal = cart.SubTotal,
        MeetsMinimum = cart.MeetsMinimum,
        AmountToMinimum = cart.AmountToMinimum,
        VolumeDiscountPercent = cart.VolumeDiscountPercent,
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int variantId, int cartons, bool ajax = false)
    {
        await cartService.UpdateItemAsync(HttpContext, variantId, cartons);
        if (ajax) return PartialView("_CartPanel", await LoadCartAsync());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int variantId, bool ajax = false)
    {
        await cartService.RemoveItemAsync(HttpContext, variantId);
        if (ajax) return PartialView("_CartPanel", await LoadCartAsync());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCoupon(string code, bool ajax = false)
    {
        await cartService.ApplyCouponAsync(HttpContext, code);
        if (ajax) return PartialView("_CartPanel", await LoadCartAsync());
        return RedirectToAction(nameof(Index));
    }
}
