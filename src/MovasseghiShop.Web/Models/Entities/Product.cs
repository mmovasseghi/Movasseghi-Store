namespace MovasseghiShop.Web.Models.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? ProductCode { get; set; }
    /// <summary>بارکد جهانی (EAN-13 / UPC) — برای Google Shopping و Product Schema.</summary>
    public string? Gtin { get; set; }
    /// <summary>کد قطعه سازنده — جایگزین GTIN وقتی بارکد جهانی ثبت نشده.</summary>
    public string? Mpn { get; set; }
    public string? Dimensions { get; set; }
    public string? Applications { get; set; }
    public string? Material { get; set; }
    public string? ProductType { get; set; }
    public int? CapacityCc { get; set; }
    public int? CompartmentCount { get; set; }
    public bool MicrowaveSafe { get; set; }
    /// <summary>Normalized use-case tags derived from Applications (comma-separated).</summary>
    public string? UseCaseTags { get; set; }
    public string? AmelonSourceUrl { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    /// <summary>ستاره طلایی — بلوک «پیشنهاد ویژه موثقی» صفحه اصلی.</summary>
    public bool IsHomeSpecialOffer { get; set; }
    /// <summary>ستاره الماسی — بلوک «محصولات برتر موثقی» صفحه اصلی.</summary>
    public bool IsHomeFeatured { get; set; }
    public int SortOrder { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    /// <summary>Primary target keyword for this product (admin).</summary>
    public string? KeywordPrimary { get; set; }
    /// <summary>Secondary target keyword for this product (admin).</summary>
    public string? KeywordSecondary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
}
