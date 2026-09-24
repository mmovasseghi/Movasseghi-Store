using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Models;

public class AccountDashboardViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public IReadOnlyList<Order> Orders { get; set; } = [];
    public IReadOnlyList<OrderDeliveryAddress> SavedAddresses { get; set; } = [];
    public IReadOnlyList<UserAddress> UserAddresses { get; set; } = [];
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Initials { get; set; } = "م";
    public string RoleBadge { get; set; } = "مشتری";
    public bool HasPassword { get; set; }
}

public static class OrderStatusLabels
{
    public static string Fa(MovasseghiShop.Web.Models.Enums.OrderStatus status) => status switch
    {
        MovasseghiShop.Web.Models.Enums.OrderStatus.PendingPayment => "در انتظار پرداخت",
        MovasseghiShop.Web.Models.Enums.OrderStatus.PaymentReceived => "پرداخت شده",
        MovasseghiShop.Web.Models.Enums.OrderStatus.Confirmed => "تأیید شده",
        MovasseghiShop.Web.Models.Enums.OrderStatus.PreparingForPickup => "در حال آماده‌سازی",
        MovasseghiShop.Web.Models.Enums.OrderStatus.ReadyForPickup => "آماده تحویل",
        MovasseghiShop.Web.Models.Enums.OrderStatus.OutForDelivery => "ارسال شده",
        MovasseghiShop.Web.Models.Enums.OrderStatus.Delivered => "تحویل داده شد",
        MovasseghiShop.Web.Models.Enums.OrderStatus.Completed => "تکمیل شده",
        MovasseghiShop.Web.Models.Enums.OrderStatus.Cancelled => "لغو شده",
        _ => status.ToString()
    };

    /// <summary>سفارش «باز» = هنوز در جریان پردازش؛ لغو، تکمیل و تحویل‌شده حساب نمی‌شود.</summary>
    public static bool IsOpen(MovasseghiShop.Web.Models.Enums.OrderStatus status) => status switch
    {
        MovasseghiShop.Web.Models.Enums.OrderStatus.Cancelled => false,
        MovasseghiShop.Web.Models.Enums.OrderStatus.Completed => false,
        MovasseghiShop.Web.Models.Enums.OrderStatus.Delivered => false,
        _ => true
    };

    public static int TimelineIndex(MovasseghiShop.Web.Models.Enums.OrderStatus status) => status switch
    {
        MovasseghiShop.Web.Models.Enums.OrderStatus.PendingPayment => 0,
        MovasseghiShop.Web.Models.Enums.OrderStatus.PaymentReceived => 1,
        MovasseghiShop.Web.Models.Enums.OrderStatus.Confirmed => 2,
        MovasseghiShop.Web.Models.Enums.OrderStatus.PreparingForPickup => 3,
        MovasseghiShop.Web.Models.Enums.OrderStatus.ReadyForPickup => 4,
        MovasseghiShop.Web.Models.Enums.OrderStatus.OutForDelivery => 4,
        MovasseghiShop.Web.Models.Enums.OrderStatus.Delivered => 5,
        MovasseghiShop.Web.Models.Enums.OrderStatus.Completed => 6,
        MovasseghiShop.Web.Models.Enums.OrderStatus.Cancelled => -1,
        _ => 0
    };

    /// <summary>سفارش در صف پیگیری اپراتور است تا «پیگیری شد» ثبت شود (لغو‌شده‌ها خارج).</summary>
    public static bool NeedsOperatorFollowUp(Order order) =>
        order.Status != OrderStatus.Cancelled && order.OperatorFollowedUpAt is null;

    public static readonly string[] CustomerTrackSteps =
    [
        "ثبت سفارش",
        "تأیید فروش",
        "آماده‌سازی",
        "تحویل / ارسال",
        "تحویل نهایی"
    ];

    /// <summary>ایندکس گام تایم‌لاین پیگیری مشتری (۰–۴)؛ لغو = -۱.</summary>
    public static int CustomerTrackStepIndex(OrderStatus status) => status switch
    {
        OrderStatus.Cancelled => -1,
        OrderStatus.PendingPayment => 0,
        OrderStatus.PaymentReceived => 1,
        OrderStatus.Confirmed => 1,
        OrderStatus.PreparingForPickup => 2,
        OrderStatus.ReadyForPickup => 3,
        OrderStatus.OutForDelivery => 3,
        OrderStatus.Delivered => 4,
        OrderStatus.Completed => 4,
        _ => 0
    };

    public static string CustomerTrackHint(OrderStatus status) => status switch
    {
        OrderStatus.Cancelled => "این سفارش لغو شده است. برای سفارش جدید با واحد فروش تماس بگیرید.",
        OrderStatus.PendingPayment => "سفارش ثبت شده؛ در صورت پرداخت آنلاین، منتظر تأیید پرداخت باشید.",
        OrderStatus.PaymentReceived or OrderStatus.Confirmed =>
            "سفارش تأیید شد. واحد فروش برای هماهنگی موجودی و زمان بارگیری با شما تماس می‌گیرد.",
        OrderStatus.PreparingForPickup => "سفارش در انبار در حال آماده‌سازی است.",
        OrderStatus.ReadyForPickup => "سفارش آماده بارگیری است؛ زمان تحویل را با فروش هماهنگ کنید.",
        OrderStatus.OutForDelivery => "سفارش در مسیر تحویل است.",
        OrderStatus.Delivered or OrderStatus.Completed => "سفارش تحویل داده شد. از خرید شما سپاسگزاریم.",
        _ => "وضعیت سفارش به‌روزرسانی می‌شود."
    };
}
