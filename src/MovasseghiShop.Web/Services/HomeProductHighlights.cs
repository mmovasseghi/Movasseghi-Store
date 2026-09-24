using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

public static class HomeProductHighlights
{
    public const int MaxPerSlot = 8;

    public static async Task<int> CountSpecialAsync(IQueryable<Product> products, CancellationToken ct = default) =>
        await products.CountAsync(p => p.IsActive && p.IsHomeSpecialOffer, ct);

    public static async Task<int> CountFeaturedAsync(IQueryable<Product> products, CancellationToken ct = default) =>
        await products.CountAsync(p => p.IsActive && p.IsHomeFeatured, ct);

    /// <summary>آیا می‌توان این نقش را برای محصول فعال کرد؟ (سقف ۸ برای هر بلوک)</summary>
    public static async Task<(bool Allowed, string? Message)> CanEnableAsync(
        IQueryable<Product> products,
        int productId,
        string role,
        CancellationToken ct = default)
    {
        if (role == "special")
        {
            var already = await products.AnyAsync(p => p.Id == productId && p.IsHomeSpecialOffer, ct);
            if (already) return (true, null);
            var count = await CountSpecialAsync(products, ct);
            if (count >= MaxPerSlot)
                return (false, $"حداکثر {MaxPerSlot} محصول فعال در «پیشنهاد ویژه» مجاز است. یکی را بردارید و دوباره تلاش کنید.");
            return (true, null);
        }

        if (role == "featured")
        {
            var already = await products.AnyAsync(p => p.Id == productId && p.IsHomeFeatured, ct);
            if (already) return (true, null);
            var count = await CountFeaturedAsync(products, ct);
            if (count >= MaxPerSlot)
                return (false, $"حداکثر {MaxPerSlot} محصول فعال در «محصولات برتر» مجاز است. یکی را بردارید و دوباره تلاش کنید.");
            return (true, null);
        }

        return (false, "نقش نامعتبر است.");
    }
}
