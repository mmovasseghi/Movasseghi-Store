namespace MovasseghiShop.Web.Models;

using MovasseghiShop.Web;
using MovasseghiShop.Web.Models.Enums;

public class CartItem
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public SellUnitType SellUnit { get; set; }
    public int CartonQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int UnitsPerCarton { get; set; }
    public int UnitsPerPack { get; set; } = 1;
    public int PacksPerCarton { get; set; }
    public string UnitName => VariantLabelHelper.GetSellUnitName(SellUnit);
    public decimal LineTotal => UnitPrice * CartonQuantity;
    public CartItemStats Stats => CartAnalyticsHelper.ComputeItem(this);
}

public class CartSummary
{
    public List<CartItem> Items { get; set; } = [];
    private CartAggregateStats? _analytics;

    public int TotalCartons => Analytics.TotalCartons;
    public int TotalUnits => Items.Sum(x => x.CartonQuantity);
    public decimal SubTotal => Items.Sum(x => x.LineTotal);
    public decimal AmountToMinimum => Math.Max(0, WholesaleRules.MinAmountToman - SubTotal);
    public bool MeetsMinimum => WholesaleRules.MeetsMinimum(SubTotal);
    public int VolumeDiscountPercent => WholesaleRules.GetVolumeDiscountPercent(SubTotal);
    public decimal VolumeDiscountAmount => WholesaleRules.CalculateVolumeDiscount(SubTotal);
    public decimal TotalAfterVolumeDiscount => SubTotal - VolumeDiscountAmount;
    public bool RequiresPremiumCoordination => WholesaleRules.RequiresPremiumCoordination(SubTotal, TotalCartons);
    public bool QualifiesForFreeTehranDelivery => WholesaleRules.QualifiesForFreeTehranDelivery(SubTotal, TotalCartons);
    public VolumeDiscountTier? NextDiscountTier => WholesaleRules.GetNextTier(SubTotal);
    public CartAggregateStats Analytics => _analytics ??= CartAnalyticsHelper.ComputeAggregate(this);
}

public class CartAddResult
{
    public string ProductName { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public int AddedQuantity { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal AddedLineTotal { get; set; }
    public int CartItemCount { get; set; }
    public decimal CartSubTotal { get; set; }
    public bool MeetsMinimum { get; set; }
    public decimal AmountToMinimum { get; set; }
    public int VolumeDiscountPercent { get; set; }
}
