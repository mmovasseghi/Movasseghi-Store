using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface ICompetitorAnalysisService
{
    Task<int> SyncKeywordAsync(string keyword, CancellationToken ct = default);
    Task<int> SyncAllPillarsAsync(CancellationToken ct = default);
    Task<List<CompetitorSnapshot>> GetLatestForKeywordAsync(string keyword, int take = 10, CancellationToken ct = default);
}

public class CompetitorAnalysisService(ApplicationDbContext db, ISerpRankProvider serp, IConfiguration config) : ICompetitorAnalysisService
{
    public async Task<int> SyncKeywordAsync(string keyword, CancellationToken ct = default)
    {
        var domain = GetTargetDomain();
        var result = await serp.GetRankAsync(keyword, domain, ct);
        if (result == null) return 0;

        foreach (var organic in result.TopResults.Take(10))
        {
            db.CompetitorSnapshots.Add(new CompetitorSnapshot
            {
                Keyword = keyword,
                RankPosition = organic.Position,
                Domain = organic.Domain,
                Title = organic.Title,
                Url = organic.Url,
                CheckedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
        return result.TopResults.Count;
    }

    public async Task<int> SyncAllPillarsAsync(CancellationToken ct = default)
    {
        if (!serp.IsConfigured) return 0;
        var keywords = await db.PillarKeywords.Where(p => p.IsActive).Select(p => p.Phrase).ToListAsync(ct);
        var total = 0;
        foreach (var kw in keywords)
            total += await SyncKeywordAsync(kw, ct);
        return total;
    }

    public async Task<List<CompetitorSnapshot>> GetLatestForKeywordAsync(string keyword, int take = 10, CancellationToken ct = default)
    {
        var latest = await db.CompetitorSnapshots.Where(s => s.Keyword == keyword).MaxAsync(s => (DateTime?)s.CheckedAt, ct);
        if (latest == null) return [];
        return await db.CompetitorSnapshots
            .Where(s => s.Keyword == keyword && s.CheckedAt == latest)
            .OrderBy(s => s.RankPosition)
            .Take(take)
            .ToListAsync(ct);
    }

    string GetTargetDomain()
    {
        var baseUrl = config["SiteSettings:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            return uri.Host;
        return "movasseghi";
    }
}
