using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Services.Editorial;

public sealed record BlogRecoveryResult(int Total, int NowReady, int StillStuck);

public interface IBlogEditorialAdminService
{
    Task EnsureKeywordsAsync(BlogPost post, CancellationToken ct = default);
    Task<int> BackfillAllBlogKeywordsAsync(CancellationToken ct = default);
    Task<BlogRecoveryResult> RecoverStuckBlogsAsync(CancellationToken ct = default);
}

public sealed class BlogEditorialAdminService(
    ApplicationDbContext db,
    ISeoSerpIntelligence serp,
    ISeoAutoFixEngine autoFix) : IBlogEditorialAdminService
{
    public async Task EnsureKeywordsAsync(BlogPost post, CancellationToken ct = default)
    {
        var profile = await SeoProfileEditorialLocks.GetOrCreateAsync(db, "blog", post.Id);
        var gsc = await serp.GetSearchConsoleQueriesAsync(ct);

        if (string.IsNullOrWhiteSpace(profile.FocusKeyword))
        {
            var (primary, secondary) = SeoKeywordResearch.ResolveEditorialKeywords(post.Title, gsc);
            profile.FocusKeyword = primary;
            profile.SecondaryKeyword = secondary;
        }

        EditorialKeywordSync.ApplyToPost(post, profile);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> BackfillAllBlogKeywordsAsync(CancellationToken ct = default)
    {
        var posts = await db.BlogPosts.OrderBy(b => b.Id).ToListAsync(ct);
        var gsc = await serp.GetSearchConsoleQueriesAsync(ct);
        var n = 0;

        foreach (var post in posts)
        {
            var profile = await SeoProfileEditorialLocks.GetOrCreateAsync(db, "blog", post.Id);
            if (string.IsNullOrWhiteSpace(profile.FocusKeyword))
            {
                var (primary, secondary) = SeoKeywordResearch.ResolveEditorialKeywords(post.Title, gsc);
                profile.FocusKeyword = primary;
                profile.SecondaryKeyword = secondary;
            }

            EditorialKeywordSync.ApplyToPost(post, profile);
            n++;
        }

        await db.SaveChangesAsync(ct);
        return n;
    }

    public async Task<BlogRecoveryResult> RecoverStuckBlogsAsync(CancellationToken ct = default)
    {
        await BackfillAllBlogKeywordsAsync(ct);
        var ids = await db.BlogPosts.OrderBy(b => b.Id).Select(b => b.Id).ToListAsync(ct);
        foreach (var id in ids)
        {
            var profile = await SeoProfileEditorialLocks.GetOrCreateAsync(db, "blog", id, ct);
            profile.LockArticleContent = false;
            profile.LockArticleExcerpt = false;
        }

        await db.SaveChangesAsync(ct);
        var ready = 0;

        for (var pass = 0; pass < 2; pass++)
        {
            ready = 0;
            foreach (var id in ids)
            {
                var before = await autoFix.AuditBlogAsync(id, ct);
                var force = !before.IsPublishReady;
                var report = await autoFix.AutoFixBlogAsync(id, 8, forceRegenerate: true, ct);
                if (report.IsPublishReady) ready++;
            }

            if (ready >= ids.Count) break;
        }

        return new BlogRecoveryResult(ids.Count, ready, ids.Count - ready);
    }
}
