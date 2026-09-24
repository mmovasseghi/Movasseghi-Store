using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Areas.Admin;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

public interface IOrderService
{
    Task<(Order? Order, string? Error)> CreateOrderAsync(HttpContext httpContext, CheckoutInput input);
    Task<Coupon?> ValidateCouponAsync(string code, CartSummary cart);
    decimal CalculateDiscount(Coupon coupon, decimal subTotal);
}

public record CheckoutInput(
    string CustomerName,
    string Phone,
    PaymentMethod PaymentMethod,
    DeliveryMethod DeliveryMethod,
    bool WantsOfficialInvoice,
    string? CompanyName,
    string? NationalId,
    string? EconomicCode,
    string? RegistrationNumber,
    string? LegalAddress,
    string? Notes,
    string? DriverName,
    string? DriverPhone,
    string? PlateNumber,
    string? VehicleType,
    string? VehicleAdditionalInfo,
    string? Province,
    string? City,
    string? FullAddress,
    string? PostalCode,
    string? RecipientName,
    string? RecipientPhone,
    string? CouponCode);

public class OrderService(
    ApplicationDbContext db,
    ICartService cartService,
    ICheckoutHumanVerificationService humanVerification,
    IAdminNavBadgeService adminNavBadges) : IOrderService
{
    public async Task<(Order? Order, string? Error)> CreateOrderAsync(HttpContext httpContext, CheckoutInput input)
    {
        if (!humanVerification.IsVerified(httpContext))
            return (null, "لطفاً در صفحه سبد خرید تأیید «ربات نیستم» را انجام دهید.");

        if (input.PaymentMethod != PaymentMethod.CashOnPickup)
            return (null, "در حال حاضر فقط «نقدی هنگام بارگیری» فعال است. پرداخت آنلاین به‌زودی اضافه می‌شود.");

        if (string.IsNullOrWhiteSpace(input.CustomerName) || string.IsNullOrWhiteSpace(input.Phone))
            return (null, "نام و شماره موبایل الزامی است.");

        var cart = await cartService.GetCartAsync(httpContext);
        if (cart.Items.Count == 0) return (null, "سبد خرید خالی است.");

        if (!cart.MeetsMinimum)
            return (null, $"حداقل سفارش {WholesaleRules.FormatMinAmount()} تومان است. برای هماهنگی با {WholesaleRules.CoordinationPhone} تماس بگیرید.");

        var volumeDiscount = cart.VolumeDiscountAmount;
        var afterVolume = cart.SubTotal - volumeDiscount;

        if (input.DeliveryMethod == DeliveryMethod.FreeKhavar && !cart.QualifiesForFreeTehranDelivery)
        {
            return (null,
                $"ارسال رایگان خاور فقط در سطح ویژه فعال است ({WholesaleRules.PremiumCartons} کارتن یا {WholesaleRules.FormatPremiumAmount()} تومان). لطفاً بارگیری با وسیله مشتری را انتخاب کنید.");
        }

        var deliveryMethod = cart.QualifiesForFreeTehranDelivery && input.DeliveryMethod == DeliveryMethod.FreeKhavar
            ? DeliveryMethod.FreeKhavar
            : DeliveryMethod.CustomerVehicle;

        if (deliveryMethod == DeliveryMethod.FreeKhavar)
        {
            if (string.IsNullOrWhiteSpace(input.Province) || string.IsNullOrWhiteSpace(input.City) || string.IsNullOrWhiteSpace(input.FullAddress))
                return (null, "آدرس تحویل داخل تهران برای ارسال رایگان الزامی است.");
        }
        else if (string.IsNullOrWhiteSpace(input.DriverName) || string.IsNullOrWhiteSpace(input.DriverPhone) ||
                 string.IsNullOrWhiteSpace(input.PlateNumber) || string.IsNullOrWhiteSpace(input.VehicleType))
        {
            return (null, "اطلاعات وسیله نقلیه و راننده الزامی است.");
        }

        if (input.WantsOfficialInvoice &&
            (string.IsNullOrWhiteSpace(input.CompanyName) || string.IsNullOrWhiteSpace(input.NationalId) || string.IsNullOrWhiteSpace(input.EconomicCode)))
            return (null, "برای فاکتور رسمی، اطلاعات حقوقی الزامی است.");

        var coupon = !string.IsNullOrWhiteSpace(input.CouponCode)
            ? await ValidateCouponAsync(input.CouponCode, cart)
            : null;
        if (!string.IsNullOrWhiteSpace(input.CouponCode) && coupon == null)
            return (null, "کد تخفیف نامعتبر است.");

        var couponDiscount = coupon != null ? CalculateDiscount(coupon, afterVolume) : 0;
        var totalDiscount = volumeDiscount + couponDiscount;
        var total = afterVolume - couponDiscount;

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            UserId = httpContext.User.Identity?.IsAuthenticated == true ? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null,
            CustomerName = input.CustomerName.Trim(),
            Phone = MockOtpService.NormalizePhone(input.Phone),
            Status = input.PaymentMethod == PaymentMethod.CashOnPickup ? OrderStatus.Confirmed : OrderStatus.PendingPayment,
            PaymentMethod = input.PaymentMethod,
            DeliveryMethod = deliveryMethod,
            SubTotal = cart.SubTotal,
            DiscountAmount = totalDiscount,
            TotalAmount = total,
            TotalCartons = cart.TotalCartons,
            CouponCode = coupon?.Code,
            WantsOfficialInvoice = input.WantsOfficialInvoice,
            CompanyName = input.CompanyName,
            NationalId = input.NationalId,
            EconomicCode = input.EconomicCode,
            RegistrationNumber = input.RegistrationNumber,
            LegalAddress = input.LegalAddress,
            Notes = input.Notes
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductVariantId = item.VariantId,
                ProductName = item.ProductName,
                VariantLabel = item.VariantLabel,
                CartonQuantity = item.CartonQuantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.UnitPrice * item.CartonQuantity
            });
        }

        if (deliveryMethod == DeliveryMethod.CustomerVehicle)
        {
            order.VehicleInfo = new OrderVehicleInfo
            {
                DriverName = input.DriverName!.Trim(),
                DriverPhone = MockOtpService.NormalizePhone(input.DriverPhone!),
                PlateNumber = input.PlateNumber!.Trim(),
                VehicleType = input.VehicleType!.Trim(),
                AdditionalInfo = input.VehicleAdditionalInfo
            };
        }
        else
        {
            order.DeliveryAddress = new OrderDeliveryAddress
            {
                Province = input.Province!.Trim(),
                City = input.City!.Trim(),
                FullAddress = input.FullAddress!.Trim(),
                PostalCode = input.PostalCode,
                RecipientName = input.RecipientName ?? input.CustomerName,
                RecipientPhone = input.RecipientPhone ?? MockOtpService.NormalizePhone(input.Phone)
            };
        }

        db.Orders.Add(order);
        if (coupon != null) coupon.UsedCount++;
        await db.SaveChangesAsync();
        adminNavBadges.Invalidate();
        await cartService.ClearAsync(httpContext);
        humanVerification.Revoke(httpContext);
        return (order, null);
    }

    public async Task<Coupon?> ValidateCouponAsync(string code, CartSummary cart)
    {
        code = code.Trim().ToUpperInvariant();
        var coupon = await db.Coupons.FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
        if (coupon == null) return null;
        var now = DateTime.UtcNow;
        if (coupon.StartsAt.HasValue && coupon.StartsAt > now) return null;
        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt < now) return null;
        if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses) return null;
        if (coupon.MinOrderAmount.HasValue && cart.SubTotal < coupon.MinOrderAmount) return null;
        if (coupon.MinCartons.HasValue && cart.TotalCartons < coupon.MinCartons) return null;
        return coupon;
    }

    public decimal CalculateDiscount(Coupon coupon, decimal subTotal)
        => coupon.Type == CouponType.Percentage
            ? Math.Round(subTotal * coupon.Value / 100m, 0)
            : Math.Min(coupon.Value, subTotal);

    private static string GenerateOrderNumber()
        => $"MS{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
}
