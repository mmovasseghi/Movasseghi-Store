using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>بلوک «محصولات برتر موثقی» — فعال + ستاره الماسی در ادمین.</summary>
public static class HomeFeaturedProductsSelector
{
    public const int MaxCount = HomeProductHighlights.MaxPerSlot;

    public static IQueryable<Product> Filter(IQueryable<Product> query) =>
        query.Where(p => p.IsActive && p.IsHomeFeatured);

    public static async Task<List<Product>> LoadAsync(IQueryable<Product> products, CancellationToken ct = default) =>
        await Filter(products)
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.UpdatedAt)
            .Take(MaxCount)
            .ToListAsync(ct);
}
