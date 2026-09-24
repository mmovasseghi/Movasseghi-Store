using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

/// <summary>
/// تبدیل قیمت لیست آملون (ریال) به تومان و قیمت نایلون/بسته/کارتن برای فروشگاه.
/// </summary>
public static class WholesalePriceCalculator
{
    public sealed record ComputedPrices(
        decimal UnitToman,
        decimal PackPriceToman,
        decimal? CartonPriceToman,
        int UnitsPerPack,
        int UnitsPerCarton,
        int PacksPerCarton);

    public static ComputedPrices Compute(PriceListEntry entry)
    {
        var packUnits = Math.Max(1, entry.Pack);
        var cartonField = entry.Cartons;

        if (entry.Type == "bulk")
            return ComputeBulk(entry.Price, packUnits, cartonField);

        return ComputeShrink(entry.Price, packUnits, cartonField);
    }

    /// <summary>فله: قیمت ردیف همیشه ریال هر عدد — نایلون = واحد × تعداد نایلون؛ کارتن = واحد × تعداد کارتن (اگر در PDF آمده).</summary>
    static ComputedPrices ComputeBulk(decimal priceRial, int packUnits, int totalUnitsInCarton)
    {
        var unitToman = Math.Round(priceRial / 10m, 0);
        var packPrice = unitToman * packUnits;

        if (totalUnitsInCarton <= 0)
            return new ComputedPrices(unitToman, packPrice, null, packUnits, packUnits, 0);

        var cartonPrice = unitToman * totalUnitsInCarton;
        var nylons = packUnits > 0 ? totalUnitsInCarton / packUnits : 0;
        return new ComputedPrices(unitToman, packPrice, cartonPrice, packUnits, totalUnitsInCarton,
            nylons > 0 ? nylons : 0);
    }

    /// <summary>
    /// شیرینگ (لیست PDF): قیمت ردیف = ریال کل هر بسته شیرینگ؛ کارتن = قیمت بسته × تعداد بسته در کارتن.
    /// فله: قیمت ردیف = ریال هر عدد (مثل درب ۱۰۵).
    /// </summary>
    static ComputedPrices ComputeShrink(decimal priceRial, int unitsPerPack, int packsPerCarton)
    {
        var packPrice = Math.Round(priceRial / 10m, 0);
        var unit = unitsPerPack > 0 ? Math.Round(packPrice / unitsPerPack, 0) : packPrice;

        if (packsPerCarton <= 0)
            return new ComputedPrices(unit, packPrice, null, unitsPerPack, unitsPerPack, 0);

        var totalUnits = packsPerCarton * unitsPerPack;
        var cartonPrice = packPrice * packsPerCarton;
        return new ComputedPrices(unit, packPrice, cartonPrice, unitsPerPack, totalUnits, packsPerCarton);
    }
}
