using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface IRankTrackerService
{
    Task<RankRadarModel> GetRadarAsync(CancellationToken ct = default);
    Task RecordSnapshotAsync(string keyword, int position, string keywordType, int? entityId = null, string? targetUrl = null, CancellationToken ct = default);
    Task<RankSyncResult> SyncFromApiAsync(CancellationToken ct = default);
    /// <summary>یک کلیدواژه مقاله — بدون اسکن کامل pillarها (بعد از انتشار).</summary>
    Task<RankSyncResult> SyncBlogKeywordAsync(int blogPostId, CancellationToken ct = default);
}

public class RankSyncResult
{
    public int KeywordsChecked { get; set; }
    public int PositionsFound { get; set; }
    public bool ApiConfigured { get; set; }
    public string? Message { get; set; }
}

public class RankRadarRow
{
    public string Keyword { get; set; } = "";
    public string KeywordType { get; set; } = "pillar";
    public int? EntityId { get; set; }
    public int? LastPosition { get; set; }
    public DateTime? LastChecked { get; set; }
    public string? TargetUrl { get; set; }
    public string GoogleSearchUrl { get; set; } = "";
}

public class RankRadarModel
{
    public List<RankRadarRow> Rows { get; set; } = [];
    public int TrackedCount { get; set; }
    public int Top3Count { get; set; }
    public int Top10Count { get; set; }
}

public class RankTrackerService(ApplicationDbContext db, IConfiguration config, ISerpRankProvider serp) : IRankTrackerService
{
    public async Task<RankRadarModel> GetRadarAsync(CancellationToken ct = default)
    {
        var pillars = await db.PillarKeywords.Where(p => p.IsActive).OrderBy(p => p.Priority).ToListAsync(ct);
        var productKeywords = await db.Products
            .Where(p => p.KeywordPrimary != null && p.KeywordPrimary != "" && p.Slug != "")
            .Select(p => new { p.Id, p.KeywordPrimary, p.Slug })
            .Take(50)
            .ToListAsync(ct);

        var blogKeywords = await db.BlogPosts
            .Where(b => b.IsPublished && b.Slug != "")
            .Join(db.SeoProfiles.Where(s => s.EntityType == "blog"),
                b => b.Id, s => s.EntityId, (b, s) => new { b.Id, b.Slug, s.FocusKeyword })
            .Where(x => x.FocusKeyword != null && x.FocusKeyword != "")
            .Take(40)
            .ToListAsync(ct);

        var allKeywords = pillars.Select(p => (p.Phrase, "pillar", (int?)null, TargetUrl: (string?)null))
            .Concat(productKeywords.Select(p => (p.KeywordPrimary!, "product", (int?)p.Id, $"/Shop/Product/{p.Slug}")))
            .Concat(blogKeywords.Select(b => (b.FocusKeyword!, "blog", (int?)b.Id, $"/Blog/Post/{b.Slug}")))
            .ToList();

        var snapshots = await db.RankSnapshots
            .OrderByDescending(s => s.CheckedAt)
            .ToListAsync(ct);

        var rows = new List<RankRadarRow>();
        foreach (var (kw, type, entityId, _) in allKeywords)
        {
            var latest = snapshots.FirstOrDefault(s =>
                s.Keyword == kw && s.KeywordType == type && s.EntityId == entityId);

            var siteBase = config["SiteSettings:PublicBaseUrl"]?.TrimEnd('/') ?? "";
            rows.Add(new RankRadarRow
            {
                Keyword = kw,
                KeywordType = type,
                EntityId = entityId,
                LastPosition = latest?.Position > 0 ? latest.Position : null,
                LastChecked = latest?.CheckedAt,
                TargetUrl = latest?.TargetUrl,
                GoogleSearchUrl = "https://www.google.com/search?q=" + Uri.EscapeDataString(kw)
            });
        }

        return new RankRadarModel
        {
            Rows = rows,
            TrackedCount = rows.Count(r => r.LastChecked.HasValue),
            Top3Count = rows.Count(r => r.LastPosition is >= 1 and <= 3),
            Top10Count = rows.Count(r => r.LastPosition is >= 1 and <= 10)
        };
    }

    public async Task RecordSnapshotAsync(string keyword, int position, string keywordType, int? entityId = null, string? targetUrl = null, CancellationToken ct = default)
    {
        await RecordSnapshotAsync(keyword, position, keywordType, "manual", entityId, targetUrl, ct);
    }

    async Task RecordSnapshotAsync(string keyword, int position, string keywordType, string source, int? entityId, string? targetUrl, CancellationToken ct)
    {
        db.RankSnapshots.Add(new RankSnapshot
        {
            Keyword = keyword,
            Position = position,
            KeywordType = keywordType,
            EntityId = entityId,
            TargetUrl = targetUrl,
            Source = source,
            CheckedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<RankSyncResult> SyncFromApiAsync(CancellationToken ct = default)
    {
        var result = new RankSyncResult { ApiConfigured = serp.IsConfigured };
        if (!serp.IsConfigured)
        {
            result.Message = "SerpAPI پیکربندی نشده — SerpApi:ApiKey در appsettings";
            return result;
        }

        var domain = GetTargetDomain();
        var pillarKeywords = await db.PillarKeywords.Where(p => p.IsActive).Select(p => p.Phrase).ToListAsync(ct);
        var blogRows = await db.BlogPosts
            .Where(b => b.IsPublished)
            .Join(db.SeoProfiles.Where(s => s.EntityType == "blog"),
                b => b.Id, s => s.EntityId, (b, s) => new { b.Id, b.Slug, Kw = s.FocusKeyword })
            .Where(x => x.Kw != null && x.Kw != "")
            .Take(25)
            .ToListAsync(ct);

        foreach (var kw in pillarKeywords)
        {
            result.KeywordsChecked++;
            var rank = await serp.GetRankAsync(kw, domain, ct);
            if (rank == null) continue;

            var position = rank.Position > 0 ? rank.Position : 0;
            if (rank.Position > 0) result.PositionsFound++;

            await RecordSnapshotAsync(kw, position, "pillar", "api", null, rank.MatchedUrl, ct);
        }

        foreach (var row in blogRows)
        {
            result.KeywordsChecked++;
            var rank = await serp.GetRankAsync(row.Kw!, domain, ct);
            if (rank == null) continue;
            var position = rank.Position > 0 ? rank.Position : 0;
            if (rank.Position > 0) result.PositionsFound++;
            await RecordSnapshotAsync(
                row.Kw!, position, "blog", "api", row.Id, rank.MatchedUrl ?? $"/Blog/Post/{row.Slug}", ct);
        }

        result.Message = $"همگام‌سازی: {result.PositionsFound}/{result.KeywordsChecked} کلیدواژه با رتبه (pillar+blog)";
        return result;
    }

    public async Task<RankSyncResult> SyncBlogKeywordAsync(int blogPostId, CancellationToken ct = default)
    {
        var result = new RankSyncResult { ApiConfigured = serp.IsConfigured };
        if (!serp.IsConfigured)
        {
            result.Message = "SerpAPI پیکربندی نشده";
            return result;
        }

        var row = await db.BlogPosts
            .Where(b => b.Id == blogPostId)
            .Join(db.SeoProfiles.Where(s => s.EntityType == "blog"),
                b => b.Id, s => s.EntityId, (b, s) => new { b.Id, b.Slug, Kw = s.FocusKeyword })
            .FirstOrDefaultAsync(ct);

        if (row == null || string.IsNullOrWhiteSpace(row.Kw))
        {
            result.Message = "کلیدواژه مقاله خالی است";
            return result;
        }

        var domain = GetTargetDomain();
        result.KeywordsChecked = 1;
        var rank = await serp.GetRankAsync(row.Kw, domain, ct);
        if (rank == null)
        {
            result.Message = "پاسخ SerpAPI دریافت نشد";
            return result;
        }

        var position = rank.Position > 0 ? rank.Position : 0;
        if (rank.Position > 0) result.PositionsFound = 1;
        await RecordSnapshotAsync(
            row.Kw, position, "blog", "api", row.Id, rank.MatchedUrl ?? $"/Blog/Post/{row.Slug}", ct);
        result.Message = position > 0
            ? $"رتبه {position} برای «{row.Kw}»"
            : $"رتبه‌ای در ۱۰۰ اول برای «{row.Kw}» ثبت نشد";
        return result;
    }

    string GetTargetDomain()
    {
        var baseUrl = config["SiteSettings:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return uri.Host;
        return "movasseghi";
    }
}
