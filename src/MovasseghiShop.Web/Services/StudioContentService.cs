using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public interface IStudioContentService
{
    Task<Dictionary<string, HomeSection>> GetHomeSectionsAsync(CancellationToken ct = default);
    Task<List<NavItem>> GetNavItemsAsync(string zone, CancellationToken ct = default);
    Task<ContentAtom?> GetAtomAsync(string key, CancellationToken ct = default);
    Task<List<HomeSection>> GetAllHomeSectionsAsync(CancellationToken ct = default);
    Task<HomeSection?> GetHomeSectionAsync(int id, CancellationToken ct = default);
    Task SaveHomeSectionAsync(HomeSection section, CancellationToken ct = default);
    Task<List<NavItem>> GetAllNavItemsAsync(CancellationToken ct = default);
    Task SaveNavItemAsync(NavItem item, CancellationToken ct = default);
    Task DeleteNavItemAsync(int id, CancellationToken ct = default);
    Task<List<ContentAtom>> GetAllAtomsAsync(CancellationToken ct = default);
    Task<ContentAtom?> GetAtomByIdAsync(int id, CancellationToken ct = default);
    Task SaveAtomAsync(ContentAtom atom, CancellationToken ct = default);
    Task<List<CmsPage>> GetAllPagesAsync(CancellationToken ct = default);
    Task<CmsPage?> GetPageByKeyAsync(string key, CancellationToken ct = default);
    Task SavePageAsync(CmsPage page, CancellationToken ct = default);
}

public class StudioContentService(ApplicationDbContext db, IMemoryCache cache) : IStudioContentService
{
    static readonly TimeSpan PublicTtl = TimeSpan.FromMinutes(10);
    const string VersionKey = "studio:ver";

    string Stamp() => cache.Get<string>(VersionKey) ?? "0";
    void Bump() => cache.Set(VersionKey, Guid.NewGuid().ToString("N"));

    public async Task<Dictionary<string, HomeSection>> GetHomeSectionsAsync(CancellationToken ct = default) =>
        await cache.GetOrCreateAsync($"studio:{Stamp()}:home", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = PublicTtl;
            return await db.HomeSections.AsNoTracking()
                .Where(s => s.IsActive)
                .ToDictionaryAsync(s => s.Key, s => s, ct);
        }) ?? [];

    public async Task<List<NavItem>> GetNavItemsAsync(string zone, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync($"studio:{Stamp()}:nav:{zone}", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = PublicTtl;
            return await db.NavItems.AsNoTracking()
                .Where(n => n.IsActive && n.Zone == zone)
                .OrderBy(n => n.SortOrder)
                .ToListAsync(ct);
        }) ?? [];

    public Task<ContentAtom?> GetAtomAsync(string key, CancellationToken ct = default) =>
        cache.GetOrCreateAsync($"studio:{Stamp()}:atom:{key}", async e =>
        {
            e.AbsoluteExpirationRelativeToNow = PublicTtl;
            return await db.ContentAtoms.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Key == key && a.IsActive, ct);
        });

    public async Task<List<HomeSection>> GetAllHomeSectionsAsync(CancellationToken ct = default) =>
        await db.HomeSections.OrderBy(s => s.SortOrder).ToListAsync(ct);

    public async Task<HomeSection?> GetHomeSectionAsync(int id, CancellationToken ct = default) =>
        await db.HomeSections.FindAsync([id], ct);

    public async Task SaveHomeSectionAsync(HomeSection section, CancellationToken ct = default)
    {
        section.UpdatedAt = DateTime.UtcNow;
        if (section.Id == 0) db.HomeSections.Add(section);
        await db.SaveChangesAsync(ct);
        Bump();
    }

    public async Task<List<NavItem>> GetAllNavItemsAsync(CancellationToken ct = default) =>
        await db.NavItems.OrderBy(n => n.Zone).ThenBy(n => n.SortOrder).ToListAsync(ct);

    public async Task SaveNavItemAsync(NavItem item, CancellationToken ct = default)
    {
        if (item.Id == 0) db.NavItems.Add(item);
        await db.SaveChangesAsync(ct);
        Bump();
    }

    public async Task DeleteNavItemAsync(int id, CancellationToken ct = default)
    {
        var item = await db.NavItems.FindAsync([id], ct);
        if (item != null)
        {
            db.NavItems.Remove(item);
            await db.SaveChangesAsync(ct);
            Bump();
        }
    }

    public async Task<List<ContentAtom>> GetAllAtomsAsync(CancellationToken ct = default) =>
        await db.ContentAtoms.OrderBy(a => a.Key).ToListAsync(ct);

    public async Task<ContentAtom?> GetAtomByIdAsync(int id, CancellationToken ct = default) =>
        await db.ContentAtoms.FindAsync([id], ct);

    public async Task SaveAtomAsync(ContentAtom atom, CancellationToken ct = default)
    {
        atom.UpdatedAt = DateTime.UtcNow;
        if (atom.Id == 0) db.ContentAtoms.Add(atom);
        await db.SaveChangesAsync(ct);
        Bump();
    }

    public async Task<List<CmsPage>> GetAllPagesAsync(CancellationToken ct = default) =>
        await db.CmsPages.OrderBy(p => p.Key).ToListAsync(ct);

    public async Task<CmsPage?> GetPageByKeyAsync(string key, CancellationToken ct = default) =>
        await db.CmsPages.FirstOrDefaultAsync(p => p.Key == key, ct);

    public async Task SavePageAsync(CmsPage page, CancellationToken ct = default)
    {
        page.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
