using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Models.Admin;

/// <summary>فرم ویرایش بسته‌بندی فله / شیرینگ در پنل محصول.</summary>
public class ProductPackagingInput
{
    public bool BulkEnabled { get; set; }
    public int BulkNylonUnits { get; set; } = 200;
    public decimal BulkNylonPrice { get; set; }
    public bool BulkNylonShowPrice { get; set; }

    public bool BulkCartonEnabled { get; set; }
    public int BulkCartonNylons { get; set; }
    public int BulkCartonTotalUnits { get; set; }
    public decimal BulkCartonPrice { get; set; }
    public bool BulkCartonShowPrice { get; set; }

    public bool ShrinkEnabled { get; set; }
    public int ShrinkPackUnits { get; set; } = 12;
    public decimal ShrinkPackPrice { get; set; }
    public bool ShrinkPackShowPrice { get; set; }

    public bool ShrinkCartonEnabled { get; set; }
    public int ShrinkCartonPacks { get; set; }
    public int ShrinkCartonTotalUnits { get; set; }
    public decimal ShrinkCartonPrice { get; set; }
    public bool ShrinkCartonShowPrice { get; set; }
}

public sealed class ProductPackagingViewModel
{
    public ProductPackagingInput Form { get; init; } = new();
    public string SkuPrefix { get; init; } = "";
}
