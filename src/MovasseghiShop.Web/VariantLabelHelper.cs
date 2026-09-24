using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web;

public static class VariantLabelHelper
{
    public static string GetSellUnitName(SellUnitType unit) => unit switch
    {
        SellUnitType.BulkNylon => "نایلون",
        SellUnitType.BulkCarton => "کارتن",
        SellUnitType.ShrinkPack => "بسته",
        SellUnitType.ShrinkCarton => "کارتن",
        _ => "واحد"
    };

    public static string GetQuantityLabel(SellUnitType unit) => $"تعداد {GetSellUnitName(unit)}";

    public static string GetPriceSuffix(SellUnitType unit) => $"تومان / {GetSellUnitName(unit)}";

    public static string GetVariantLabel(ProductVariant v)
    {
        if (!string.IsNullOrWhiteSpace(v.PackLabel))
            return v.PackLabel;
        return v.SellUnit switch
        {
            SellUnitType.BulkNylon => "فله — نایلون",
            SellUnitType.BulkCarton => "فله — کارتن",
            SellUnitType.ShrinkPack => "شیرینگ — بسته",
            SellUnitType.ShrinkCarton => "شیرینگ — کارتن",
            _ => v.PackagingType == PackagingType.Bulk ? "فله" : "شیرینگ"
        };
    }

    public static bool CountsAsCarton(SellUnitType unit)
        => unit is SellUnitType.BulkCarton or SellUnitType.ShrinkCarton;
}
