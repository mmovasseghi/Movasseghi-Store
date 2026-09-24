using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services.ContentPipeline;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class SeoContentRelevanceModuleTests
{
    [Fact]
    public void Rejects_irrelevant_competitor_pages()
    {
        var product = new Product
        {
            Name = "بشقاب بسته‌بندی ۶ عددی آملون",
            ProductCode = "201088",
            Material = "گیاهی"
        };
        var bundle = new SeoContentResearchBundle
        {
            Competitors =
            [
                new CompetitorPageResearch
                {
                    Title = "خرید لپ‌تاپ گیمینگ",
                    Domain = "example.com",
                    MetaDescription = "لپ تاپ ارزان"
                },
                new CompetitorPageResearch
                {
                    Title = "بشقاب ۶ عددی آملون بسته شیرینگ",
                    Domain = "shop.ir",
                    MetaDescription = "ظروف گیاهی آملون ۶ عددی"
                }
            ]
        };

        SeoContentRelevanceModule.ScoreAndFilter(product, bundle, new SeoContentPipelineConfig());
        Assert.False(bundle.Competitors.Single(c => c.Domain == "example.com").Accepted);
        Assert.True(bundle.Competitors.Single(c => c.Domain == "shop.ir").Accepted);
    }
}
