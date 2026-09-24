using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Models.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public PaymentMethod PaymentMethod { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalCartons { get; set; }
    public string? CouponCode { get; set; }
    public bool WantsOfficialInvoice { get; set; }
    public string? CompanyName { get; set; }
    public string? NationalId { get; set; }
    public string? EconomicCode { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? LegalAddress { get; set; }
    public string? Notes { get; set; }
    public string? PaymentReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>زمان ثبت «پیگیری شد» توسط اپراتور در پنل — null یعنی هنوز در صف پیگیری است.</summary>
    public DateTime? OperatorFollowedUpAt { get; set; }
    public string? OperatorFollowedUpBy { get; set; }

    public bool NeedsOperatorFollowUp =>
        MovasseghiShop.Web.Models.OrderStatusLabels.NeedsOperatorFollowUp(this);

    public OrderVehicleInfo? VehicleInfo { get; set; }
    public OrderDeliveryAddress? DeliveryAddress { get; set; }
    public ICollection<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public int CartonQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderVehicleInfo
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string DriverName { get; set; } = string.Empty;
    public string DriverPhone { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string? AdditionalInfo { get; set; }
}

public class OrderDeliveryAddress
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
}
