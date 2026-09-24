using MovasseghiShop.Web;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class WholesaleRulesDeliveryTests
{
    [Theory]
    [InlineData(699_999_999, 179, false)]
    [InlineData(700_000_000, 0, true)]
    [InlineData(10_000_000, 180, true)]
    [InlineData(700_000_000, 180, true)]
    public void QualifiesForFreeTehranDelivery_uses_premium_threshold(decimal subTotal, int cartons, bool expected)
    {
        Assert.Equal(expected, WholesaleRules.QualifiesForFreeTehranDelivery(subTotal, cartons));
    }

    [Fact]
    public void QualifiesForFreeTehranDelivery_matches_requires_premium_coordination()
    {
        const decimal sub = 50_000_000m;
        const int cartons = 90;
        Assert.Equal(
            WholesaleRules.RequiresPremiumCoordination(sub, cartons),
            WholesaleRules.QualifiesForFreeTehranDelivery(sub, cartons));
    }
}
