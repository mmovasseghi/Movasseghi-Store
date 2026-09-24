using System.Globalization;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web;

public record CartItemStats(
    int NylonCount,
    int CartonCount,
    int PackCount,
    int PieceCount,
    int PiecesPerSellUnit,
    int InnerNylonsPerCarton,
    int InnerPacksPerCarton,
    string SummaryLine,
    string DetailLine);

public record CartPackagingSlice(string Label, string Icon, int Count, int Pieces, int Percent);

public record CartAggregateStats(
    int TotalNylons,
    int TotalCartons,
    int TotalPacks,
    int TotalPieces,
    int ProductLines,
    decimal SubTotalWeightScore,
    IReadOnlyList<CartPackagingSlice> PackagingMix,
    IReadOnlyList<(CartItem Item, CartItemStats Stats)> ItemDetails,
    string SmartSummaryLine,
    IReadOnlyList<(string Label, string Value)> SpecRows);

public static class CartAnalyticsHelper
{
    public static string FormatCount(int n) => n.ToString("N0", CultureInfo.GetCultureInfo("fa-IR"));

    public static CartItemStats ComputeItem(CartItem item)
    {
        var q = item.CartonQuantity;
        return item.SellUnit switch
        {
            SellUnitType.BulkNylon => Build(
                q, 0, 0, q * Safe(item.UnitsPerPack),
                item.UnitsPerPack, 0, 0,
                $"{FormatCount(q)} نایلون × {FormatCount(Safe(item.UnitsPerPack))} عدد",
                $"مجموع {FormatCount(q * Safe(item.UnitsPerPack))} قلم (عدد)"),

            SellUnitType.BulkCarton => Build(
                q * Safe(item.PacksPerCarton), q, 0, q * Safe(item.UnitsPerCarton),
                item.UnitsPerCarton, item.PacksPerCarton, 0,
                $"{FormatCount(q)} کارتن × {FormatCount(Safe(item.PacksPerCarton))} نایلون × {FormatCount(Safe(item.UnitsPerPack))} عدد",
                $"= {FormatCount(q * Safe(item.PacksPerCarton))} نایلون · {FormatCount(q * Safe(item.UnitsPerCarton))} قلم"),

            SellUnitType.ShrinkPack => Build(
                0, 0, q, q * Safe(item.UnitsPerPack),
                item.UnitsPerPack, 0, 0,
                $"{FormatCount(q)} بسته × {FormatCount(Safe(item.UnitsPerPack))} عدد",
                $"مجموع {FormatCount(q * Safe(item.UnitsPerPack))} قلم (عدد)"),

            SellUnitType.ShrinkCarton => Build(
                0, q, q * Safe(item.PacksPerCarton), q * Safe(item.UnitsPerCarton),
                item.UnitsPerCarton, 0, item.PacksPerCarton,
                $"{FormatCount(q)} کارتن × {FormatCount(Safe(item.PacksPerCarton))} بسته × {FormatCount(Safe(item.UnitsPerPack))} عدد",
                $"= {FormatCount(q * Safe(item.PacksPerCarton))} بسته · {FormatCount(q * Safe(item.UnitsPerCarton))} قلم"),

            _ => Build(
                0, q, 0, q * Safe(item.UnitsPerCarton > 0 ? item.UnitsPerCarton : item.UnitsPerPack),
                item.UnitsPerCarton > 0 ? item.UnitsPerCarton : item.UnitsPerPack, 0, 0,
                $"{FormatCount(q)} {item.UnitName}",
                $"مجموع {FormatCount(q * Safe(item.UnitsPerCarton > 0 ? item.UnitsPerCarton : item.UnitsPerPack))} قلم")
        };
    }

    public static CartSummary BuildSummaryFromOrder(Order order, IReadOnlyDictionary<int, ProductVariant> variants)
    {
        var items = new List<CartItem>();
        foreach (var oi in order.Items)
        {
            if (!variants.TryGetValue(oi.ProductVariantId, out var v))
            {
                items.Add(new CartItem
                {
                    VariantId = oi.ProductVariantId,
                    ProductName = oi.ProductName,
                    VariantLabel = oi.VariantLabel,
                    SellUnit = SellUnitType.BulkCarton,
                    CartonQuantity = oi.CartonQuantity,
                    UnitPrice = oi.UnitPrice,
                    UnitsPerCarton = 1,
                    UnitsPerPack = 1,
                    PacksPerCarton = 1
                });
                continue;
            }

            items.Add(new CartItem
            {
                VariantId = v.Id,
                ProductId = v.ProductId,
                ProductName = oi.ProductName,
                VariantLabel = oi.VariantLabel,
                SellUnit = v.SellUnit,
                CartonQuantity = oi.CartonQuantity,
                UnitPrice = oi.UnitPrice,
                UnitsPerCarton = v.UnitsPerCarton,
                UnitsPerPack = v.UnitsPerPack,
                PacksPerCarton = v.PacksPerCarton
            });
        }

        return new CartSummary { Items = items };
    }

    public static async Task<CartSummary?> TryBuildSummaryFromOrderAsync(ApplicationDbContext db, Order order, CancellationToken ct = default)
    {
        if (order.Items.Count == 0) return null;
        var ids = order.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var variants = await db.ProductVariants.AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);
        return BuildSummaryFromOrder(order, variants);
    }

    public static CartAggregateStats ComputeAggregate(CartSummary cart)
    {
        var details = cart.Items.Select(i => (i, ComputeItem(i))).ToList();
        var totalNylons = details.Sum(x => x.Item2.NylonCount);
        var totalCartons = details.Sum(x => x.Item2.CartonCount);
        var totalPacks = details.Sum(x => x.Item2.PackCount);
        var totalPieces = details.Sum(x => x.Item2.PieceCount);

        var mix = new List<CartPackagingSlice>();
        if (totalNylons > 0)
            mix.Add(new("نایلون", "📦", totalNylons, details.Where(x => x.Item2.NylonCount > 0).Sum(x => x.Item2.PieceCount),
                Percent(totalNylons, totalNylons + totalCartons + totalPacks)));
        if (totalCartons > 0)
            mix.Add(new("کارتن", "🧱", totalCartons, details.Where(x => x.Item2.CartonCount > 0).Sum(x => x.Item2.PieceCount),
                Percent(totalCartons, totalNylons + totalCartons + totalPacks)));
        if (totalPacks > 0)
            mix.Add(new("بسته شیرینگ", "🎁", totalPacks, details.Where(x => x.Item2.PackCount > 0).Sum(x => x.Item2.PieceCount),
                Percent(totalPacks, totalNylons + totalCartons + totalPacks)));

        if (mix.Count == 0 && totalPieces > 0)
            mix.Add(new("قلم", "✨", cart.TotalUnits, totalPieces, 100));

        var smartSummary = BuildSmartSummary(totalNylons, totalCartons, totalPacks, totalPieces);
        var specRows = BuildSpecRows(details);

        return new(totalNylons, totalCartons, totalPacks, totalPieces, cart.Items.Count, totalPieces,
            mix, details, smartSummary, specRows);
    }

    public static IReadOnlyList<(string Label, string Value)> GetItemSpecRows(CartItem item, CartItemStats stats)
    {
        var rows = new List<(string, string)>();
        switch (item.SellUnit)
        {
            case SellUnitType.BulkNylon:
                rows.Add(("هر نایلون", $"{FormatCount(Safe(item.UnitsPerPack))} عدد (قلم)"));
                break;
            case SellUnitType.BulkCarton:
                rows.Add(("هر نایلون", $"{FormatCount(Safe(item.UnitsPerPack))} عدد"));
                rows.Add(("هر کارتن", $"{FormatCount(Safe(item.PacksPerCarton))} نایلون · {FormatCount(Safe(item.UnitsPerCarton))} عدد"));
                break;
            case SellUnitType.ShrinkPack:
                rows.Add(("هر بسته", $"{FormatCount(Safe(item.UnitsPerPack))} عدد (قلم)"));
                break;
            case SellUnitType.ShrinkCarton:
                rows.Add(("هر بسته", $"{FormatCount(Safe(item.UnitsPerPack))} عدد"));
                rows.Add(("هر کارتن", $"{FormatCount(Safe(item.PacksPerCarton))} بسته · {FormatCount(Safe(item.UnitsPerCarton))} عدد"));
                break;
        }
        return rows;
    }

    static string BuildSmartSummary(int nylons, int cartons, int packs, int pieces)
    {
        var parts = new List<string>();
        if (nylons > 0) parts.Add($"{FormatCount(nylons)} نایلون");
        if (cartons > 0) parts.Add($"{FormatCount(cartons)} کارتن");
        if (packs > 0) parts.Add($"{FormatCount(packs)} بسته");
        if (parts.Count == 0) return pieces > 0 ? $"مجموع {FormatCount(pieces)} قلم در سبد" : "سبد خالی";
        return $"جمع بسته‌بندی: {string.Join(" + ", parts)} = {FormatCount(pieces)} قلم (عدد)";
    }

    static List<(string Label, string Value)> BuildSpecRows(IReadOnlyList<(CartItem Item, CartItemStats Stats)> details)
    {
        var rows = new List<(string, string)>();
        var bulkNylonUnits = details
            .Where(x => x.Item.SellUnit is SellUnitType.BulkNylon or SellUnitType.BulkCarton)
            .Select(x => Safe(x.Item.UnitsPerPack))
            .Distinct().ToList();
        if (bulkNylonUnits.Count == 1)
            rows.Add(("ظرفیت هر نایلون (فله)", $"{FormatCount(bulkNylonUnits[0])} عدد"));
        else if (bulkNylonUnits.Count > 1)
            rows.Add(("ظرفیت هر نایلون (فله)", $"بین {FormatCount(bulkNylonUnits.Min())} تا {FormatCount(bulkNylonUnits.Max())} عدد"));

        var bulkCartonNylons = details
            .Where(x => x.Item.SellUnit == SellUnitType.BulkCarton)
            .Select(x => Safe(x.Item.PacksPerCarton))
            .Distinct().ToList();
        if (bulkCartonNylons.Count == 1)
            rows.Add(("نایلون در هر کارتن (فله)", $"{FormatCount(bulkCartonNylons[0])} نایلون"));

        var shrinkPackUnits = details
            .Where(x => x.Item.SellUnit is SellUnitType.ShrinkPack or SellUnitType.ShrinkCarton)
            .Select(x => Safe(x.Item.UnitsPerPack))
            .Distinct().ToList();
        if (shrinkPackUnits.Count == 1)
            rows.Add(("ظرفیت هر بسته (شیرینگ)", $"{FormatCount(shrinkPackUnits[0])} عدد"));

        var shrinkCartonPacks = details
            .Where(x => x.Item.SellUnit == SellUnitType.ShrinkCarton)
            .Select(x => Safe(x.Item.PacksPerCarton))
            .Distinct().ToList();
        if (shrinkCartonPacks.Count == 1)
            rows.Add(("بسته در هر کارتن (شیرینگ)", $"{FormatCount(shrinkCartonPacks[0])} بسته"));

        return rows;
    }

    public static string DonutGradient(IReadOnlyList<CartPackagingSlice> mix)
    {
        if (mix.Count == 0) return "conic-gradient(#e8ede9 0 100%)";
        var colors = new[] { "#3d5a4c", "#c9a84c", "#6b9080", "#a4c3b2" };
        var pos = 0;
        var parts = new List<string>();
        foreach (var (slice, i) in mix.Select((s, i) => (s, i)))
        {
            var end = pos + slice.Percent;
            parts.Add($"{colors[i % colors.Length]} {pos}% {end}%");
            pos = end;
        }
        return $"conic-gradient({string.Join(", ", parts)})";
    }

    static CartItemStats Build(int nylons, int cartons, int packs, int pieces, int perUnit, int innerNylons, int innerPacks, string summary, string detail)
        => new(nylons, cartons, packs, pieces, perUnit, innerNylons, innerPacks, summary, detail);

    static int Safe(int v) => v > 0 ? v : 1;

    static int Percent(int part, int whole)
        => whole <= 0 ? 0 : (int)Math.Round(part * 100m / whole);
}
