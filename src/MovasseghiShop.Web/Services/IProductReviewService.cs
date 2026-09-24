using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

public interface IProductReviewService
{
    Task<IReadOnlyList<ProductReview>> GetApprovedForProductAsync(int productId, CancellationToken ct = default);
    Task<(double Average, int Count)> GetApprovedRatingAsync(int productId, CancellationToken ct = default);
    Task<int> CountApprovedAsync(int productId, CancellationToken ct = default);
    Task EnsureSeoSuggestedReviewsAsync(Product product, string focus, CancellationToken ct = default);
    Task SubmitPublicReviewAsync(int productId, string authorName, int rating, string body, CancellationToken ct = default);
}

public class ProductReviewService(ApplicationDbContext db) : IProductReviewService
{
    public async Task<IReadOnlyList<ProductReview>> GetApprovedForProductAsync(int productId, CancellationToken ct = default)
    {
        var roots = await db.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ProductReviewStatus.Approved && r.ParentReviewId == null)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        if (roots.Count == 0) return roots;

        var rootIds = roots.Select(r => r.Id).ToList();
        var replies = await db.ProductReviews
            .AsNoTracking()
            .Where(r => r.ParentReviewId != null && rootIds.Contains(r.ParentReviewId.Value)
                        && r.Status == ProductReviewStatus.Approved)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

        foreach (var root in roots)
            root.Replies = replies.Where(x => x.ParentReviewId == root.Id).ToList();

        return roots;
    }

    public async Task SubmitPublicReviewAsync(int productId, string authorName, int rating, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(body)) return;
        var exists = await db.Products.AnyAsync(p => p.Id == productId && p.IsActive, ct);
        if (!exists) return;

        db.ProductReviews.Add(new ProductReview
        {
            ProductId = productId,
            AuthorDisplayName = authorName.Trim()[..Math.Min(80, authorName.Trim().Length)],
            Body = body.Trim()[..Math.Min(2000, body.Trim().Length)],
            Rating = Math.Clamp(rating, 1, 5),
            Status = ProductReviewStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<(double Average, int Count)> GetApprovedRatingAsync(int productId, CancellationToken ct = default)
    {
        var ratings = await db.ProductReviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ProductReviewStatus.Approved && r.Rating > 0)
            .Select(r => r.Rating)
            .ToListAsync(ct);
        if (ratings.Count == 0) return (0, 0);
        return (Math.Round(ratings.Average(), 1), ratings.Count);
    }

    public async Task<int> CountApprovedAsync(int productId, CancellationToken ct = default) =>
        await db.ProductReviews.CountAsync(
            r => r.ProductId == productId && r.Status == ProductReviewStatus.Approved && r.Rating > 0, ct);

    public async Task EnsureSeoSuggestedReviewsAsync(Product product, string focus, CancellationToken ct = default)
    {
        var hasAny = await db.ProductReviews.AnyAsync(r => r.ProductId == product.Id, ct);
        if (hasAny) return;

        var drafts = SeoProductReviewGenerator.BuildSuggestedThread(product, focus);
        var mains = drafts.Where(r => !r.IsStoreReply).ToList();
        var store = drafts.FirstOrDefault(r => r.IsStoreReply);
        db.ProductReviews.AddRange(mains);
        await db.SaveChangesAsync(ct);

        if (store is not null)
        {
            var question = mains.FirstOrDefault(r => r.Body.Contains('؟'));
            store.ParentReviewId = question?.Id;
            store.ProductId = product.Id;
            db.ProductReviews.Add(store);
            await db.SaveChangesAsync(ct);
        }
    }
}
