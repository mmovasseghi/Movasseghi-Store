using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Models.Entities;

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public ProductImageRole Role { get; set; } = ProductImageRole.Studio;
}
