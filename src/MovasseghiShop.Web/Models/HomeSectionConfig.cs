using System.Text.Json;

namespace MovasseghiShop.Web.Models;

public record HeroSlideConfig(string Kicker, string Title, string? TitleEm, string Desc, string Cta1Text, string Cta1Url, string? Cta2Text, string? Cta2Url);
public record FeatureCardConfig(string Icon, string Title, string Desc);
public record WholesaleSegmentConfig(string Title, string Desc, string Icon, string Url);
public record PromoBarConfig(string SloganFull, string SloganShort, string[] Chips);

public static class HomeSectionConfigParser
{
    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static List<HeroSlideConfig> ParseHeroSlides(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("slides", out var slides)) return [];
            return slides.EnumerateArray().Select(s => new HeroSlideConfig(
                s.GetProperty("kicker").GetString() ?? "",
                s.GetProperty("title").GetString() ?? "",
                s.TryGetProperty("titleEm", out var em) ? em.GetString() : null,
                s.GetProperty("desc").GetString() ?? "",
                s.GetProperty("cta1Text").GetString() ?? "",
                s.GetProperty("cta1Url").GetString() ?? "/Catalog",
                s.TryGetProperty("cta2Text", out var c2t) ? c2t.GetString() : null,
                s.TryGetProperty("cta2Url", out var c2u) ? c2u.GetString() : null
            )).ToList();
        }
        catch { return []; }
    }

    public static string[] ParseTrustItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("trustItems", out var items)) return [];
            return items.EnumerateArray().Select(i => i.GetString() ?? "").Where(s => s != "").ToArray();
        }
        catch { return []; }
    }

    public static List<FeatureCardConfig> ParseFeatureCards(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("cards", out var cards)) return [];
            return cards.EnumerateArray().Select(c => new FeatureCardConfig(
                c.GetProperty("icon").GetString() ?? "shield",
                c.GetProperty("title").GetString() ?? "",
                c.GetProperty("desc").GetString() ?? ""
            )).ToList();
        }
        catch { return []; }
    }

    public static List<WholesaleSegmentConfig> ParseWholesaleSegments(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("segments", out var segs)) return [];
            return segs.EnumerateArray().Select(s => new WholesaleSegmentConfig(
                s.GetProperty("title").GetString() ?? "",
                s.GetProperty("desc").GetString() ?? "",
                s.GetProperty("icon").GetString() ?? "store",
                s.GetProperty("url").GetString() ?? "/Catalog"
            )).ToList();
        }
        catch { return []; }
    }

    public static PromoBarConfig? ParsePromoBar(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonSerializer.Deserialize<PromoBarConfig>(json, JsonOpts); }
        catch { return null; }
    }
}
