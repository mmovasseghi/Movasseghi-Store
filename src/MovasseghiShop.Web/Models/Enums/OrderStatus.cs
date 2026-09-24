namespace MovasseghiShop.Web.Models.Enums;

public enum OrderStatus
{
    PendingPayment = 1,
    PaymentReceived = 2,
    Confirmed = 3,
    PreparingForPickup = 4,
    ReadyForPickup = 5,
    OutForDelivery = 6,
    Delivered = 7,
    Completed = 8,
    Cancelled = 9
}
