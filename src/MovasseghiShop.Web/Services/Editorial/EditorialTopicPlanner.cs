using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services.Editorial;

public sealed record EditorialTopicCandidate(
    string FocusKeyword,
    string SecondaryKeyword,
    string ProposedTitle,
    string Angle,
    int PillarPriority,
    double OpportunityScore,
    string Source);

/// <summary>
/// اولویت‌بندی کلمات ستون اصلی + فرصت‌های GSC با امتیاز «مانورپذیری»
/// (نیت اطلاعاتی/تجاری، نبود مقاله نزدیک، اولویت پیلار).
/// </summary>
public static class EditorialTopicPlanner
{
    static readonly string[] TitleAngles =
    [
        "guide", "compare", "buying", "usecase", "faq"
    ];

    public static async Task<List<EditorialTopicCandidate>> PlanAsync(
        ApplicationDbContext db,
        ISeoSerpIntelligence serp,
        int maxTopics,
        CancellationToken ct = default)
    {
        var gsc = await serp.GetSearchConsoleQueriesAsync(ct);
        var publishedTitles = await db.BlogPosts
            .Where(b => b.IsPublished)
            .Select(b => b.Title)
            .ToListAsync(ct);
        var profiles = await db.SeoProfiles
            .Where(p => p.EntityType == "blog")
            .Select(p => new { p.FocusKeyword, p.EntityId })
            .ToListAsync(ct);
        var coveredFocus = profiles
            .Where(p => !string.IsNullOrWhiteSpace(p.FocusKeyword))
            .Select(p => p.FocusKeyword!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var list = new List<EditorialTopicCandidate>();

        foreach (var pillar in SiteKeywordStrategy.Pillars.OrderBy(p => p.Priority))
        {
            if (HasCannibalization(pillar.Phrase, publishedTitles, coveredFocus))
                continue;

            var secondary = SiteKeywordStrategy.Secondary;
            var angle = TitleAngles[pillar.Priority % TitleAngles.Length];
            var title = ProposeTitle(pillar.Phrase, angle, pillar.Priority);
            var score = ScorePillar(pillar, publishedTitles.Count, gsc);

            list.Add(new EditorialTopicCandidate(
                pillar.Phrase, secondary, title, angle, pillar.Priority, score, "pillar"));
        }

        foreach (var row in gsc.OrderByDescending(r => r.Impressions).Take(25))
        {
            if (!SeoText.IsKeywordish(row.Query, SiteKeywordStrategy.Primary, 0.45))
                continue;
            if (HasCannibalization(row.Query, publishedTitles, coveredFocus))
                continue;

            var focus = SeoText.HeadPhrase(row.Query);
            var title = ProposeTitle(focus, "gsc", row.Impressions.GetHashCode());
            var score = ScoreGsc(row, publishedTitles.Count);
            list.Add(new EditorialTopicCandidate(
                focus, SiteKeywordStrategy.Secondary, title, "gsc", 50, score, "gsc"));
        }

        ApplyStrikingDistanceBoost(list, await db.RankSnapshots
            .OrderByDescending(s => s.CheckedAt)
            .Take(200)
            .ToListAsync(ct));

        return list
            .GroupBy(c => SeoText.Normalize(c.FocusKeyword))
            .Select(g => g.OrderByDescending(x => x.OpportunityScore).First())
            .OrderByDescending(c => c.OpportunityScore)
            .ThenBy(c => c.PillarPriority)
            .Take(maxTopics)
            .ToList();
    }

    /// <summary>رتبه ۴–۲۰: نزدیک‌ترین فرصت برای رسیدن به Top 3 با مقاله هدفمند.</summary>
    static void ApplyStrikingDistanceBoost(List<EditorialTopicCandidate> list, List<RankSnapshot> snapshots)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var c = list[i];
            var snap = snapshots.FirstOrDefault(s => SeoText.IsKeywordish(s.Keyword, c.FocusKeyword, 0.85));
            if (snap?.Position is >= 4 and <= 20)
            {
                list[i] = c with
                {
                    OpportunityScore = Math.Min(99, c.OpportunityScore + (21 - snap.Position) * 1.2)
                };
            }
        }
    }

    static bool HasCannibalization(string focus, List<string> titles, HashSet<string> coveredFocus)
    {
        if (coveredFocus.Contains(focus)) return true;
        return titles.Any(t => SeoText.IsKeywordish(t, focus, 0.82));
    }

    static double ScorePillar(PillarKeywordDef pillar, int publishedCount, IReadOnlyList<GscQueryRow> gsc)
    {
        var baseScore = 100 - pillar.Priority * 3.5;
        if (pillar.SearchIntent == "informational") baseScore += 8;
        if (pillar.Cluster is "product" or "usecase") baseScore += 6;

        var gscBoost = gsc
            .Where(r => SeoText.IsKeywordish(r.Query, pillar.Phrase, 0.55))
            .Sum(r => Math.Min(12, Math.Log10(r.Impressions + 1) * 4));
        baseScore += gscBoost;

        baseScore -= Math.Min(15, publishedCount * 0.3);
        return Math.Round(Math.Clamp(baseScore, 20, 98), 1);
    }

    static double ScoreGsc(GscQueryRow row, int publishedCount)
    {
        var imp = Math.Min(40, Math.Log10(row.Impressions + 1) * 12);
        var pos = row.Position is > 0 and < 30 ? (30 - row.Position) * 0.8 : 0;
        var ctrGap = row.Impressions > 50 && row.Clicks < row.Impressions * 0.02 ? 10 : 0;
        return Math.Round(Math.Clamp(45 + imp + pos + ctrGap - publishedCount * 0.2, 25, 95), 1);
    }

    public static string ProposeTitle(string focus, string angle, int seed)
    {
        var head = SeoText.HeadPhrase(focus);
        return angle switch
        {
            "compare" => $"مقایسه {head} با ظروف پلاستیکی — راهنمای خریدار عمده",
            "buying" => $"خرید عمده {head}: چک‌لیست قبل از سفارش",
            "usecase" => $"کاربرد {head} در رستوران، کیترینگ و فست‌فود",
            "faq" => $"سوالات پرتکرار درباره {head}",
            "gsc" => $"راهنمای عملی {head} برای تأمین ماهانه",
            _ => $"راهنمای جامع {head} برای کسب‌وکارهای غذایی"
        };
    }
}
