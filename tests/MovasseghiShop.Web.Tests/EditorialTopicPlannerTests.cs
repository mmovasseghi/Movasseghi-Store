using MovasseghiShop.Web.Services;
using Xunit;
using MovasseghiShop.Web.Services.Editorial;

namespace MovasseghiShop.Web.Tests;

public class EditorialTopicPlannerTests
{
    [Fact]
    public void ProposeTitle_includes_focus_phrase()
    {
        var title = EditorialTopicPlanner.ProposeTitle("لیوان یکبار مصرف گیاهی", "guide", 1);
        Assert.Contains("لیوان", title);
    }

    [Fact]
    public void Higher_priority_pillar_scores_above_low_priority_in_formula()
    {
        var high = SiteKeywordStrategy.Pillars.First(p => p.Priority == 1);
        var low = SiteKeywordStrategy.Pillars.First(p => p.Priority == 20);
        var scoreHigh = InvokeScorePillar(high);
        var scoreLow = InvokeScorePillar(low);
        Assert.True(scoreHigh > scoreLow);
    }

    static double InvokeScorePillar(PillarKeywordDef pillar)
    {
        var method = typeof(EditorialTopicPlanner).GetMethod(
            "ScorePillar",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (double)method!.Invoke(null, [pillar, 0, Array.Empty<GscQueryRow>()])!;
    }
}
