using MovasseghiShop.Web.Services;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class ProductDescriptionHelperTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p></p>")]
    [InlineData("<p><br></p>")]
    [InlineData("<p>کوتاه</p>")]
    public void HasSubstantiveProse_rejects_empty_or_stub_html(string html)
    {
        Assert.False(ProductDescriptionHelper.HasSubstantiveProse(html));
    }

    [Fact]
    public void HasSubstantiveProse_accepts_real_paragraph()
    {
        var html = "<p>" + string.Join(' ', Enumerable.Repeat("ظرف", 15)) + "</p>";
        Assert.True(ProductDescriptionHelper.HasSubstantiveProse(html));
    }
}
