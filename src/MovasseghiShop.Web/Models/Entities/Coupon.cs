using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Models.Entities;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? MinCartons { get; set; }
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
