using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Models.Entities;

namespace MovasseghiShop.Web.Services;

/// <summary>بازه‌های ظرفیت (سی‌سی) برای فیلتر کاتالوگ — کاربردی‌تر از «تعداد خانه».</summary>
public static class CatalogCapacityBands
{
    public sealed record Band(string Value, string Label, int? MinCc, int? MaxCc);

    public static readonly Band[] All =
    [
        new("lt300", "تا ۳۰۰ سی‌سی", null, 299),
        new("300-500", "۳۰۰ تا ۵۰۰ سی‌سی", 300, 500),
        new("500-800", "۵۰۰ تا ۸۰۰ سی‌سی", 500, 800),
        new("gt800", "۸۰۰ سی‌سی و بیشتر", 800, null)
    ];

    public static string? LabelFor(string? value) =>
        All.FirstOrDefault(b => b.Value == value)?.Label;

    public static IQueryable<Product> Apply(IQueryable<Product> query, string? bandValue)
    {
        if (string.IsNullOrWhiteSpace(bandValue)) return query;
        var band = All.FirstOrDefault(b => b.Value == bandValue);
        if (band is null) return query;

        query = query.Where(p => p.CapacityCc != null);
        if (band.MinCc is int min)
            query = query.Where(p => p.CapacityCc >= min);
        if (band.MaxCc is int max)
            query = query.Where(p => p.CapacityCc <= max);
        return query;
    }

    public static async Task<List<(Band Band, int Count)>> CountAsync(
        IQueryable<Product> scoped,
        CancellationToken ct = default)
    {
        var withCap = scoped.Where(p => p.CapacityCc != null);
        var list = new List<(Band, int)>();
        foreach (var band in All)
        {
            var q = withCap;
            if (band.MinCc is int min)
                q = q.Where(p => p.CapacityCc >= min);
            if (band.MaxCc is int max)
                q = q.Where(p => p.CapacityCc <= max);
            var count = await q.CountAsync(ct);
            if (count > 0)
                list.Add((band, count));
        }
        return list;
    }
}
