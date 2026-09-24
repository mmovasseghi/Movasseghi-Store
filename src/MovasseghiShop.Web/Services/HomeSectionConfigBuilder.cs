using System.Text.Json;
using MovasseghiShop.Web.Models;

namespace MovasseghiShop.Web.Services;

public static class HomeSectionConfigBuilder
{
    public static string BuildHeroJson(IEnumerable<HeroSlideConfig> slides, IEnumerable<string> trustItems) =>
        JsonSerializer.Serialize(new { slides, trustItems });

    public static string BuildFeaturesJson(string bandText, IEnumerable<FeatureCardConfig> cards) =>
        JsonSerializer.Serialize(new { bandText, cards });

    public static string BuildWholesaleJson(string badge, IEnumerable<WholesaleSegmentConfig> segments) =>
        JsonSerializer.Serialize(new { badge, segments });

    public static string BuildBadgeJson(string badge, string? subtitle = null) =>
        subtitle != null
            ? JsonSerializer.Serialize(new { badge, subtitle })
            : JsonSerializer.Serialize(new { badge });
}
