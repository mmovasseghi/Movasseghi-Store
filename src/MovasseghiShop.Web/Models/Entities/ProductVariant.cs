using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Models.Entities;

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public PackagingType PackagingType { get; set; }
    public SellUnitType SellUnit { get; set; }
    public int PacksPerCarton { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool ShowPrice { get; set; }
    public int UnitsPerPack { get; set; } = 1;
    public int UnitsPerCarton { get; set; } = 500;
    public string? PackLabel { get; set; }
    public bool IsActive { get; set; } = true;
}
