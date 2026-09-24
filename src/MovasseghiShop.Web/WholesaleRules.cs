namespace MovasseghiShop.Web;

public readonly record struct VolumeDiscountTier(decimal MinAmountToman, int DiscountPercent, string LabelFa);

public static class WholesaleRules
{
    public const decimal MinAmountToman = 10_000_000m;
    public const int PremiumCartons = 180;
    public const decimal PremiumAmountToman = 700_000_000m;
    public const int MaxAutoDiscountPercent = 12;
    public const string CoordinationPhone = "09125199105";
    public const string StoreNameFa = "فروشگاه موثقی | نمایندگی رسمی آملون";
    public const string StoreNameEn = "Movasseghi Store";

    public static readonly VolumeDiscountTier[] VolumeTiers =
    [
        new(10_000_000m, 3, "۱۰ میلیون"),
        new(30_000_000m, 5, "۳۰ میلیون"),
        new(50_000_000m, 7, "۵۰ میلیون"),
        new(80_000_000m, 9, "۸۰ میلیون"),
        new(100_000_000m, 10, "۱۰۰ میلیون"),
        new(150_000_000m, 12, "۱۵۰ میلیون"),
    ];

    public static bool MeetsMinimum(decimal subTotal) => subTotal >= MinAmountToman;

    public static bool RequiresPremiumCoordination(decimal subTotal, int physicalCartons)
        => subTotal >= PremiumAmountToman || physicalCartons >= PremiumCartons;

    public static bool QualifiesForFreeTehranDelivery(decimal subTotal, int cartons)
        => RequiresPremiumCoordination(subTotal, cartons);

    /// <summary>تخفیف پلکانی فقط بر اساس جمع فاکتور — نه تعداد کارتن.</summary>
    public static int GetVolumeDiscountPercent(decimal subTotal)
    {
        var percent = 0;
        foreach (var tier in VolumeTiers)
        {
            if (subTotal >= tier.MinAmountToman)
                percent = tier.DiscountPercent;
        }
        return percent;
    }

    public static decimal CalculateVolumeDiscount(decimal subTotal)
        => Math.Round(subTotal * GetVolumeDiscountPercent(subTotal) / 100m, 0);

    public static VolumeDiscountTier? GetActiveTier(decimal subTotal)
    {
        VolumeDiscountTier? active = null;
        foreach (var tier in VolumeTiers)
        {
            if (subTotal >= tier.MinAmountToman)
                active = tier;
        }
        return active;
    }

    public static VolumeDiscountTier? GetNextTier(decimal subTotal)
    {
        foreach (var tier in VolumeTiers)
        {
            if (subTotal < tier.MinAmountToman)
                return tier;
        }
        return null;
    }

    public static string FormatMinAmount() => SlugHelper.FormatToman(MinAmountToman);

    public static string FormatPremiumAmount() => SlugHelper.FormatToman(PremiumAmountToman);
}
