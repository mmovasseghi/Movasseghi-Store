using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>انتخاب محصولات بلوک «پیشنهاد ویژه موثقی» — فقط فعال + ستاره‌دار در ادمین.</summary>
public static class HomeSpecialOffersSelector
{
    public const int MaxCount = HomeProductHighlights.MaxPerSlot;

    public static IQueryable<Product> Filter(IQueryable<Product> query) =>
        query.Where(p => p.IsActive && p.IsHomeSpecialOffer);

    public static async Task<List<Product>> LoadAsync(IQueryable<Product> products, CancellationToken ct = default) =>
        await Filter(products)
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.UpdatedAt)
            .Take(MaxCount)
            .ToListAsync(ct);
}
