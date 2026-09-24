using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;
using MovasseghiShop.Web.Services.Editorial;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class SeoEditorialPublishGateTests
{
    [Fact]
    public void Draft_blog_with_rich_content_can_pass_publish_gate()
    {
        var focus = "ظروف یکبار مصرف آملون";
        var title = "راهنمای خرید عمده ظروف آملون برای رستوران";
        var html = SeoEditorialWriter.EnsureCatalogImagesInHtml(
            SeoEditorialWriter.BuildContent(
                title, focus, "ظروف گیاهی", "buying", 42, []),
            "/uploads/sample-hero.jpg",
            focus,
            title,
            42,
            ["/uploads/sample-hero.jpg", "/uploads/sample-inline.jpg"]);

        var profile = new SeoProfile
        {
            FocusKeyword = focus,
            SecondaryKeyword = "ظروف گیاهی",
            MetaTitle = SeoArticleBuilder.BuildMetaTitle(focus, title),
            MetaDescription = SeoArticleBuilder.BuildMetaDescription(focus, "ظروف گیاهی", title, ""),
            AiSummary = SeoEditorialWriter.BuildGeoSummary(focus, "ظروف گیاهی", title, []),
            FaqJson = SeoFaqStore.Serialize(SeoEditorialWriter.BuildFaqs(focus, title, "ظروف گیاهی"))
        };

        var ctx = new SeoUltimateContext
        {
            Profile = profile,
            Prose = html,
            EditorialTitle = title,
            EditorialExcerpt = SeoEditorialWriter.BuildExcerpt(focus, title, "buying"),
            FeaturedImageUrl = "/uploads/sample.jpg",
            EditorialPublished = false,
            EditorialKind = "blog",
            Baseline = new SeoSerpBaseline { HasLiveData = false }
        };

        var report = SeoUltimateAnalyzer.AnalyzeEditorial(ctx);

        Assert.True(report.IsIndexable);
        Assert.True(report.TechnicalScore >= SeoContentRules.MinTechnicalScore);
        Assert.True(report.WordCount >= SeoEditorialWriter.MinWordCount);
        Assert.True(report.SeoScore >= SeoContentRules.MinOnPageScore);
        Assert.True(report.ImageScore >= SeoContentRules.MinImageScore);
    }
}
