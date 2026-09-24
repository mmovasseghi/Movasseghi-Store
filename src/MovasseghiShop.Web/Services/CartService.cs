using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

public interface ICartService
{
    Task<CartSummary> GetCartAsync(HttpContext httpContext);
    int GetItemCount(HttpContext httpContext);
    Task AddItemAsync(HttpContext httpContext, int variantId, int cartons);
    Task UpdateItemAsync(HttpContext httpContext, int variantId, int cartons);
    Task RemoveItemAsync(HttpContext httpContext, int variantId);
    Task ClearAsync(HttpContext httpContext);
    Task<string?> ApplyCouponAsync(HttpContext httpContext, string code);
    Task<string?> GetCouponCodeAsync(HttpContext httpContext);
}

public class CartService(ApplicationDbContext db) : ICartService
{
    private const string CartKey = "cart";
    private const string CouponKey = "coupon";

    public async Task<CartSummary> GetCartAsync(HttpContext httpContext)
    {
        var items = GetItems(httpContext);
        if (items.Count == 0) return new CartSummary();

        var variantIds = items.Select(x => x.VariantId).ToList();
        var variants = await db.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id) && v.IsActive)
            .ToListAsync();

        var summary = new CartSummary();
        foreach (var item in items)
        {
            var variant = variants.FirstOrDefault(v => v.Id == item.VariantId);
            if (variant == null) continue;

            summary.Items.Add(new CartItem
            {
                VariantId = variant.Id,
                ProductId = variant.ProductId,
                ProductName = variant.Product.Name,
                VariantLabel = VariantLabelHelper.GetVariantLabel(variant),
                SellUnit = variant.SellUnit,
                CartonQuantity = item.CartonQuantity,
                UnitPrice = variant.Price,
                UnitsPerCarton = variant.UnitsPerCarton,
                UnitsPerPack = variant.UnitsPerPack,
                PacksPerCarton = variant.PacksPerCarton
            });
        }

        return summary;
    }

    public int GetItemCount(HttpContext httpContext) => GetItems(httpContext).Count;

    public async Task AddItemAsync(HttpContext httpContext, int variantId, int cartons)
    {
        if (cartons < 1) return;
        var variant = await db.ProductVariants.Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.IsActive && v.Product.IsActive);
        if (variant == null) return;

        var items = GetItems(httpContext);
        var existing = items.FirstOrDefault(x => x.VariantId == variantId);
        if (existing != null) existing.CartonQuantity += cartons;
        else items.Add(new CartItem { VariantId = variantId, CartonQuantity = cartons });
        SaveItems(httpContext, items);
    }

    public async Task UpdateItemAsync(HttpContext httpContext, int variantId, int cartons)
    {
        if (cartons < 1) { await RemoveItemAsync(httpContext, variantId); return; }
        var items = GetItems(httpContext);
        var existing = items.FirstOrDefault(x => x.VariantId == variantId);
        if (existing != null) existing.CartonQuantity = cartons;
        SaveItems(httpContext, items);
    }

    public Task RemoveItemAsync(HttpContext httpContext, int variantId)
    {
        var items = GetItems(httpContext);
        items.RemoveAll(x => x.VariantId == variantId);
        SaveItems(httpContext, items);
        return Task.CompletedTask;
    }

    public Task ClearAsync(HttpContext httpContext)
    {
        httpContext.Session.Remove(CartKey);
        httpContext.Session.Remove(CouponKey);
        return Task.CompletedTask;
    }

    public Task<string?> ApplyCouponAsync(HttpContext httpContext, string code)
    {
        httpContext.Session.SetString(CouponKey, code.Trim().ToUpperInvariant());
        return Task.FromResult<string?>(null);
    }

    public Task<string?> GetCouponCodeAsync(HttpContext httpContext)
        => Task.FromResult(httpContext.Session.GetString(CouponKey));

    private static List<CartItem> GetItems(HttpContext httpContext)
    {
        var json = httpContext.Session.GetString(CartKey);
        return string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
    }

    private static void SaveItems(HttpContext httpContext, List<CartItem> items)
        => httpContext.Session.SetString(CartKey, JsonSerializer.Serialize(items));
}
