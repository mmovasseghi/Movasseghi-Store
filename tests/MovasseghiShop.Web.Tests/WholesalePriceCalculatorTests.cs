using MovasseghiShop.Web.Services;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class WholesalePriceCalculatorTests
{
    [Fact]
    public void Bulk_darb_105_unit_nylon_carton()
    {
        var entry = new PriceListEntry("201009", "درب 105", 21600, 3000, 300, "bulk");
        var c = WholesalePriceCalculator.Compute(entry);
        Assert.Equal(2160, c.UnitToman);
        Assert.Equal(2160 * 300, c.PackPriceToman);
        Assert.Equal(2160 * 3000, c.CartonPriceToman);
    }

    [Fact]
    public void Bulk_kik_khori_nylon_and_carton()
    {
        var entry = new PriceListEntry("201058", "کیک خوری", 44700, 1400, 200, "bulk");
        var c = WholesalePriceCalculator.Compute(entry);
        Assert.Equal(4470, c.UnitToman);
        Assert.Equal(4470 * 200, c.PackPriceToman);
        Assert.Equal(4470 * 1400, c.CartonPriceToman);
        Assert.Equal(7, c.PacksPerCarton);
    }

    [Fact]
    public void Shrink_kik_khori_pack_and_carton()
    {
        var entry = new PriceListEntry("201085", "کیک خوری 12", 822000, 51, 12, "shrink");
        var c = WholesalePriceCalculator.Compute(entry);
        Assert.Equal(82200, c.PackPriceToman);
        Assert.Equal(6850, c.UnitToman);
        Assert.Equal(82200 * 51, c.CartonPriceToman);
    }

    [Fact]
    public void Bulk_without_carton_column_still_unit_times_nylon()
    {
        var entry = new PriceListEntry("201001", "بشقاب", 72600, 0, 200, "bulk");
        var c = WholesalePriceCalculator.Compute(entry);
        Assert.Equal(7260, c.UnitToman);
        Assert.Equal(7260 * 200, c.PackPriceToman);
        Assert.Null(c.CartonPriceToman);
    }

    [Fact]
    public void Bulk_large_plate_201003_nylon_150()
    {
        var entry = new PriceListEntry("201003", "بشقاب بزرگ", 138700, 0, 150, "bulk");
        var c = WholesalePriceCalculator.Compute(entry);
        Assert.Equal(13870, c.UnitToman);
        Assert.Equal(13870 * 150, c.PackPriceToman);
    }
}
