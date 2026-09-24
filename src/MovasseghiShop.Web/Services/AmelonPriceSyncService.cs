using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;

namespace MovasseghiShop.Web.Services;

public interface IAmelonPriceSyncService
{
    Task<PriceSyncResult> SyncFromPriceListsAsync(CancellationToken ct = default);
    Task<int> EnrichSeoFromAmelonAsync(CancellationToken ct = default);
}

public record PriceSyncResult(
    int ProductsMatched,
    int VariantsUpdated,
    int VariantsCreated,
    int ProductsCreated,
    int Skipped,
    int ProductsDeactivated,
    PriceSyncAuditReport? Audit = null);

public record PriceSyncAuditReport(
    int PdfWholesaleCodes,
    int MappedToSite,
    int MatchedProducts,
    IReadOnlyList<string> PdfCodesWithoutSiteMatch,
    IReadOnlyList<string> SiteProductsNotInPdf,
    IReadOnlyList<string> DeactivatedProducts);

public record PriceListEntry(string Code, string Name, decimal Price, int Cartons, int Pack, string Type);

public record WholesaleMapEntry(string Web, string? Slug, string? Name);

public class AmelonPriceSyncService(
    ApplicationDbContext db,
    IWebHostEnvironment env,
    IHttpClientFactory httpClientFactory,
    ILogger<AmelonPriceSyncService> logger) : IAmelonPriceSyncService
{
    const string PriceJson = "App_Data/amelon-prices.json";
    const string MapJson = "App_Data/amelon-wholesale-map.json";

    static readonly Dictionary<string, string> ManualWebCodes = new(StringComparer.Ordinal)
    {
        ["201009"] = "000606",
        ["201050"] = "008912",
        ["201059"] = "009208",
        ["201121"] = "009312",
        ["201076"] = "008912",
        ["201074"] = "008814",
        ["201077"] = "008924",
        ["401122"] = "008815",
        ["000702"] = "008612",
        ["201003"] = "008612",
        ["201001"] = "000532",
        ["201105"] = "000530",
        // کاسه معمولی — نقشه خودکار اشتباه به 000434 می‌رفت
        ["201032"] = "000407",
        ["201033"] = "000412",
        ["201034"] = "000406",
        ["201035"] = "000406",
        ["201036"] = "000405",
        // کاسه صدفی
        ["201037"] = "008706",
        ["201038"] = "000404",
        ["201039"] = "000434",
        ["201080"] = "008706",
        ["201081"] = "000404",
        ["201082"] = "000210",
    };

    public static readonly string[] MissingWebCodes =
        ["000606", "008912", "009208", "009312", "008814", "008815"];

    public async Task<PriceSyncResult> SyncFromPriceListsAsync(CancellationToken ct = default)
    {
        var path = Path.Combine(env.ContentRootPath, PriceJson);
        if (!File.Exists(path))
            throw new FileNotFoundException("Price list JSON not found.", path);

        await using var stream = File.OpenRead(path);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var bulk = ParseEntries(doc.RootElement.GetProperty("bulk"));
        var shrink = ParseEntries(doc.RootElement.GetProperty("shrink"));
        var byWholesale = bulk.Concat(shrink).GroupBy(x => x.Code).ToDictionary(g => g.Key, g => g.ToList());

        var wholesaleMap = LoadWholesaleMap();
        foreach (var (k, v) in ManualWebCodes)
        {
            if (wholesaleMap.TryGetValue(k, out var existing))
                wholesaleMap[k] = existing with { Web = v };
            else
                wholesaleMap[k] = new WholesaleMapEntry(v, null, null);
        }

        var products = await db.Products.Include(p => p.Variants).ToListAsync(ct);
        var bySlug = products
            .GroupBy(p => p.Slug, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var matched = 0;
        var updated = 0;
        var created = 0;
        var skipped = 0;
        var pdfCodesWithoutSite = new List<string>();
        var coveredProductIds = new HashSet<int>();
        var packagingTouched = new Dictionary<int, HashSet<PackagingType>>();

        foreach (var (wholesaleCode, entries) in byWholesale)
        {
            if (!wholesaleMap.TryGetValue(wholesaleCode, out var mapEntry))
                mapEntry = new WholesaleMapEntry(wholesaleCode, null, entries[0].Name);

            var webCode = mapEntry.Web;
            if (ManualWebCodes.TryGetValue(wholesaleCode, out var manualWeb))
                webCode = manualWeb;

            var product = ResolveProduct(products, bySlug, mapEntry, webCode, entries[0].Name);
            if (product == null)
            {
                logger.LogWarning("No product for wholesale {Wholesale} (web {Web}, slug {Slug})",
                    wholesaleCode, webCode, mapEntry.Slug);
                pdfCodesWithoutSite.Add($"{wholesaleCode} — {entries[0].Name} (کد سایت: {webCode})");
                skipped++;
                continue;
            }

            var skuPrefix = GetSkuPrefix(product);
            matched++;
            coveredProductIds.Add(product.Id);
            foreach (var entry in entries)
                UpsertVariantsFromEntry(product, skuPrefix, entry, ref created, ref updated);

            if (!packagingTouched.TryGetValue(product.Id, out var touched))
            {
                touched = [];
                packagingTouched[product.Id] = touched;
            }
            foreach (var entry in entries)
                touched.Add(entry.Type == "bulk" ? PackagingType.Bulk : PackagingType.Shrink);

            product.IsActive = true;
            product.UpdatedAt = DateTime.UtcNow;
            ApplySeo(product, wholesaleCode);
            DeactivateLegacyVariants(product, skuPrefix);
        }

        foreach (var (productId, touched) in packagingTouched)
        {
            var product = products.First(p => p.Id == productId);
            if (!touched.Contains(PackagingType.Bulk))
                DeactivatePackagingVariants(product, PackagingType.Bulk);
            if (!touched.Contains(PackagingType.Shrink))
                DeactivatePackagingVariants(product, PackagingType.Shrink);
        }

        var propagated = PropagateCanonicalVariants(products, ref created, ref updated);
        var unitAsNylonFixes = FixBulkNylonPricesStoredAsUnit(bulk, products);

        var deactivated = new List<string>();
        foreach (var p in products)
        {
            if (coveredProductIds.Contains(p.Id) || !p.IsActive)
                continue;

            p.IsActive = false;
            p.UpdatedAt = DateTime.UtcNow;
            deactivated.Add($"{p.ProductCode ?? "—"} | {p.Name} (Id {p.Id})");
        }

        var onSiteNotInPdf = products
            .Where(p => !coveredProductIds.Contains(p.Id))
            .Select(p => $"{p.ProductCode ?? "—"} | {p.Name}")
            .OrderBy(x => x)
            .ToList();

        var audit = new PriceSyncAuditReport(
            byWholesale.Count,
            wholesaleMap.Count,
            matched,
            pdfCodesWithoutSite,
            onSiteNotInPdf,
            deactivated);

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Price sync: matched={Matched}, variants updated={Updated}, created={Created}, propagated={Prop}, unitFix={UnitFix}, skipped={Skipped}, deactivated={Off}",
            matched, updated, created, propagated, unitAsNylonFixes, skipped, deactivated.Count);

        WriteAuditReport(audit);

        return new PriceSyncResult(matched, updated, created, 0, skipped, deactivated.Count, audit);
    }

    void WriteAuditReport(PriceSyncAuditReport audit)
    {
        try
        {
            var path = Path.Combine(env.ContentRootPath, "App_Data", "price-sync-audit.json");
            var json = JsonSerializer.Serialize(audit, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not write price sync audit");
        }
    }

    static string GetSkuPrefix(Product product) =>
        !string.IsNullOrWhiteSpace(product.ProductCode) ? product.ProductCode.Trim() : $"P{product.Id}";

    void UpsertVariantsFromEntry(Product product, string skuPrefix, PriceListEntry entry, ref int created, ref int updated)
    {
        var computed = WholesalePriceCalculator.Compute(entry);
        if (computed.PackPriceToman <= 0) return;

        if (entry.Type == "bulk")
        {
            UpsertVariant(product, skuPrefix, "BN", SellUnitType.BulkNylon, PackagingType.Bulk,
                computed.PackPriceToman, computed.UnitsPerPack, computed.UnitsPerPack, 0,
                $"نایلون {computed.UnitsPerPack:N0} عددی", ref created, ref updated);

            if (computed.CartonPriceToman is > 0 && computed.PacksPerCarton > 0)
            {
                UpsertVariant(product, skuPrefix, "BC", SellUnitType.BulkCarton, PackagingType.Bulk,
                    computed.CartonPriceToman.Value, computed.UnitsPerPack, computed.UnitsPerCarton,
                    computed.PacksPerCarton,
                    $"کارتن {computed.UnitsPerCarton:N0} عددی ({computed.PacksPerCarton} نایلون)",
                    ref created, ref updated);
            }

            return;
        }

        UpsertVariant(product, skuPrefix, "SP", SellUnitType.ShrinkPack, PackagingType.Shrink,
            computed.PackPriceToman, computed.UnitsPerPack, computed.UnitsPerPack, 0,
            $"بسته {computed.UnitsPerPack} عددی", ref created, ref updated);

        if (computed.CartonPriceToman is > 0 && computed.PacksPerCarton > 0)
        {
            UpsertVariant(product, skuPrefix, "SC", SellUnitType.ShrinkCarton, PackagingType.Shrink,
                computed.CartonPriceToman.Value, computed.UnitsPerPack, computed.UnitsPerCarton,
                computed.PacksPerCarton,
                $"کارتن {computed.PacksPerCarton} بسته ({computed.UnitsPerCarton:N0} عدد)",
                ref created, ref updated);
        }
    }

    void UpsertVariant(Product product, string skuPrefix, string suffix, SellUnitType sellUnit, PackagingType packaging,
        decimal price, int unitsPerPack, int unitsPerCarton, int packsPerCarton, string packLabel,
        ref int created, ref int updated)
    {
        var sku = $"AM-{skuPrefix}-{suffix}";
        var foreign = db.ProductVariants
            .FirstOrDefault(v => v.Sku == sku && v.ProductId != product.Id);
        if (foreign != null)
        {
            foreign.IsActive = false;
            foreign.Sku = $"AM-P{foreign.ProductId}-{suffix}-dup";
        }

        var variant = product.Variants.FirstOrDefault(v => v.Sku == sku)
            ?? product.Variants.FirstOrDefault(v => v.IsActive && v.SellUnit == sellUnit)
            ?? FindLegacyVariant(product, skuPrefix, suffix, sellUnit, packaging);

        if (variant == null)
        {
            variant = new ProductVariant { ProductId = product.Id, Sku = sku, IsActive = true };
            product.Variants.Add(variant);
            db.ProductVariants.Add(variant);
            created++;
        }
        else
        {
            if (variant.Sku != sku)
            {
                variant.Sku = sku;
                updated++;
            }
            else updated++;
        }

        variant.IsActive = true;
        variant.SellUnit = sellUnit;
        variant.PackagingType = packaging;
        variant.Price = price;
        variant.ShowPrice = price > 0;
        variant.UnitsPerPack = unitsPerPack;
        variant.UnitsPerCarton = unitsPerCarton;
        variant.PacksPerCarton = packsPerCarton;
        variant.PackLabel = packLabel;
    }

    static ProductVariant? FindLegacyVariant(Product product, string skuPrefix, string suffix, SellUnitType sellUnit, PackagingType packaging)
    {
        var legacySku = suffix is "BN" or "BC" ? $"AM-{skuPrefix}-B" : $"AM-{skuPrefix}-S";
        var legacy = product.Variants.FirstOrDefault(v => v.Sku == legacySku);
        if (legacy != null) return legacy;

        return product.Variants.FirstOrDefault(v => v.IsActive
            && (v.PackagingType == packaging || v.PackagingType == default)
            && v.SellUnit == sellUnit
            && !IsCanonicalVariant(v.Sku, skuPrefix))
            ?? product.Variants.FirstOrDefault(v => v.IsActive
            && (v.PackagingType == packaging || v.PackagingType == default)
            && !IsCanonicalVariant(v.Sku, skuPrefix)
            && (v.Price <= 0 || !v.ShowPrice));
    }

    static bool IsCanonicalVariant(string sku, string skuPrefix)
        => sku.StartsWith($"AM-{skuPrefix}-", StringComparison.Ordinal)
           && (sku.EndsWith("BN", StringComparison.Ordinal) || sku.EndsWith("BC", StringComparison.Ordinal)
               || sku.EndsWith("SP", StringComparison.Ordinal) || sku.EndsWith("SC", StringComparison.Ordinal));

    static void DeactivateLegacyVariants(Product product, string skuPrefix)
    {
        foreach (var v in product.Variants)
        {
            if (IsCanonicalVariant(v.Sku, skuPrefix))
                continue;
            if (v.Sku.StartsWith("AM-", StringComparison.Ordinal))
                v.IsActive = false;
        }
    }

    /// <summary>
    /// وقتی در PDF ستون کارتن خالی است، بعضی واریانت‌ها قیمت واحد (تومان) را به‌جای نایلون ذخیره کرده‌اند.
    /// </summary>
    static int FixBulkNylonPricesStoredAsUnit(List<PriceListEntry> bulkEntries, List<Product> products)
    {
        var unitByPack = bulkEntries
            .Where(e => e is { Type: "bulk", Pack: > 1 })
            .GroupBy(e => e.Pack)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => (long)Math.Round(e.Price / 10m)).ToHashSet());

        var fixes = 0;
        for (var round = 0; round < 40; round++)
        {
            var roundFixes = 0;
            foreach (var v in products.SelectMany(p => p.Variants)
                         .Where(v => v.IsActive
                                     && v.SellUnit == SellUnitType.BulkNylon
                                     && v.UnitsPerPack > 1))
            {
                if (!unitByPack.TryGetValue(v.UnitsPerPack, out var units)) continue;
                var price = (long)Math.Round(v.Price);
                if (!units.Contains(price)) continue;
                v.Price = price * v.UnitsPerPack;
                roundFixes++;
            }

            fixes += roundFixes;
            if (roundFixes == 0) break;
        }

        return fixes;
    }

    static void DeactivatePackagingVariants(Product product, PackagingType packaging)
    {
        foreach (var v in product.Variants)
        {
            if (v.PackagingType != packaging) continue;
            if (!v.Sku.StartsWith("AM-", StringComparison.Ordinal)) continue;
            v.IsActive = false;
        }
    }

    int PropagateCanonicalVariants(List<Product> products, ref int created, ref int updated)
    {
        var propagated = 0;
        var groups = products
            .Where(p => p.IsActive && !string.IsNullOrEmpty(p.ProductCode))
            .GroupBy(p => p.ProductCode!, StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var list = group.ToList();
            if (list.Count < 2) continue;

            var canonical = list
                .OrderByDescending(p => p.Variants.Count(v => v.IsActive && v.ShowPrice && v.Price > 0))
                .ThenBy(p => p.Id)
                .First();

            if (!canonical.Variants.Any(v => v.IsActive && v.ShowPrice && v.Price > 0))
                continue;

            foreach (var alias in list.Where(p => p.Id != canonical.Id))
            {
                if (NameMatchScore(alias.Name, canonical.Name) < 8)
                    continue;
                if (alias.Variants.Any(v => v.IsActive && v.ShowPrice && v.Price > 0))
                    continue;

                var prefix = $"P{alias.Id}";
                foreach (var src in canonical.Variants.Where(v => v.IsActive && v.ShowPrice && v.Price > 0))
                {
                    var suffix = src.Sku.Split('-').LastOrDefault() ?? "X";
                    if (suffix is not ("BN" or "BC" or "SP" or "SC")) continue;
                    CloneVariant(alias, src, prefix, suffix, ref created, ref updated);
                    propagated++;
                }
            }
        }

        return propagated;
    }

    void CloneVariant(Product target, ProductVariant src, string skuPrefix, string suffix,
        ref int created, ref int updated)
    {
        var sku = $"AM-{skuPrefix}-{suffix}";
        var variant = target.Variants.FirstOrDefault(v => v.Sku == sku)
            ?? target.Variants.FirstOrDefault(v => v.IsActive && v.SellUnit == src.SellUnit);

        if (variant == null)
        {
            variant = new ProductVariant { ProductId = target.Id, Sku = sku, IsActive = true };
            target.Variants.Add(variant);
            db.ProductVariants.Add(variant);
            created++;
        }
        else
        {
            variant.Sku = sku;
            updated++;
        }

        variant.IsActive = true;
        variant.SellUnit = src.SellUnit;
        variant.PackagingType = src.PackagingType;
        variant.Price = src.Price;
        variant.ShowPrice = src.ShowPrice;
        variant.UnitsPerPack = src.UnitsPerPack;
        variant.UnitsPerCarton = src.UnitsPerCarton;
        variant.PacksPerCarton = src.PacksPerCarton;
        variant.PackLabel = src.PackLabel;
    }

    static Product? ResolveProduct(
        List<Product> products,
        Dictionary<string, Product> bySlug,
        WholesaleMapEntry mapEntry,
        string webCode,
        string priceListName)
    {
        if (!string.IsNullOrEmpty(mapEntry.Slug) && bySlug.TryGetValue(mapEntry.Slug, out var bySlugProduct))
            return bySlugProduct;

        var candidates = products
            .Where(p => string.Equals(p.ProductCode, webCode, StringComparison.Ordinal))
            .ToList();
        if (candidates.Count == 1)
            return candidates[0];
        if (candidates.Count > 1)
            return candidates.OrderByDescending(p => NameMatchScore(p.Name, mapEntry.Name ?? priceListName)).First();

        return products.FirstOrDefault(p => string.Equals(p.ProductCode, webCode, StringComparison.Ordinal));
    }

    static int NameMatchScore(string productName, string priceName)
    {
        var a = NormalizeNameTokens(productName);
        var b = NormalizeNameTokens(priceName);
        if (a.Count == 0 || b.Count == 0) return 0;
        return b.Count(t => a.Contains(t));
    }

    static HashSet<string> NormalizeNameTokens(string name)
    {
        var cleaned = Regex.Replace(name, @"[^\p{L}\p{Nd}]+", " ");
        return cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1 && !int.TryParse(t, out _))
            .Select(t => t.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<int> EnrichSeoFromAmelonAsync(CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("amelon");
        var cacheDir = Path.Combine(env.ContentRootPath, "App_Data", "amelon-cache");
        var products = await db.Products
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.AmelonSourceUrl != null)
            .ToListAsync(ct);

        var count = 0;
        foreach (var product in products)
        {
            try
            {
                var url = product.AmelonSourceUrl!;
                var slug = Uri.UnescapeDataString(url.TrimEnd('/').Split('/').Last());
                var cachePath = Path.Combine(cacheDir, "products", Uri.EscapeDataString(slug) + ".html");
                string? html;
                if (File.Exists(cachePath))
                    html = await File.ReadAllTextAsync(cachePath, ct);
                else
                {
                    html = await client.GetStringAsync(url, ct);
                    Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
                    await File.WriteAllTextAsync(cachePath, html, ct);
                }

                if (string.IsNullOrWhiteSpace(html)) continue;

                EnrichFromHtml(product, html);
                product.UpdatedAt = DateTime.UtcNow;
                count++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Enrich failed for {Code}", product.ProductCode);
            }
        }

        await db.SaveChangesAsync(ct);

        return count;
    }

    static void EnrichFromHtml(Product product, string html)
    {
        var descMatch = Regex.Match(html, @"<meta name=""description"" content=""(.*?)""", RegexOptions.Singleline);
        if (descMatch.Success)
        {
            var desc = WebUtilityHtmlDecode(descMatch.Groups[1].Value);
            if (desc.Length > 40)
            {
                product.Description = BuildRichDescription(product, desc, html);
                product.ShortDescription = desc.Length > 180 ? desc[..180] + "…" : desc;
            }
        }

        var imgs = AmelonProductImageParser.ExtractImageUrls(html);
        if (imgs.Count > 0)
        {
            if (!product.Images.Any())
            {
                for (var i = 0; i < imgs.Count; i++)
                {
                    product.Images.Add(new ProductImage
                    {
                        Url = imgs[i],
                        AltText = $"{product.Name} — ظرف گیاهی آملون، تصویر {(i + 1)}",
                        IsPrimary = i == 0,
                        SortOrder = i
                    });
                }
            }
            else
            {
                foreach (var img in product.Images.Where(i => i.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
                {
                    var match = imgs.FirstOrDefault(u => u.Contains(Path.GetFileName(new Uri(u).LocalPath), StringComparison.OrdinalIgnoreCase));
                    if (match != null) img.Url = match;
                }
            }
        }

        ApplySeo(product, null);
    }

    static string BuildRichDescription(Product p, string metaDesc, string html)
    {
        var bodyMatch = Regex.Match(html, @"<div class=""the_content_wrapper[^""]*"">(.*?)</div>\s*</div>\s*</div>\s*<section",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var body = bodyMatch.Success ? bodyMatch.Groups[1].Value : "";
        body = Regex.Replace(body, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (string.IsNullOrWhiteSpace(body) || body.Length < 30)
            body = $"<p>{metaDesc}</p>";

        var faq = new StringBuilder();
        faq.Append("<section class=\"product-specs\">");
        faq.Append("<h2>مشخصات فنی</h2><ul>");
        if (!string.IsNullOrWhiteSpace(p.ProductCode))
            faq.Append($"<li><strong>کد محصول آملون:</strong> {p.ProductCode}</li>");
        if (!string.IsNullOrWhiteSpace(p.Dimensions))
            faq.Append($"<li><strong>ابعاد:</strong> {p.Dimensions}</li>");
        if (!string.IsNullOrWhiteSpace(p.Material))
            faq.Append($"<li><strong>جنس:</strong> {p.Material}</li>");
        if (p.MicrowaveSafe)
            faq.Append("<li><strong>مایکروویو:</strong> قابل استفاده</li>");
        faq.Append("</ul></section>");

        return body + faq;
    }

    static void ApplySeo(Product p, string? wholesaleCode)
    {
        var type = string.IsNullOrWhiteSpace(p.ProductType) ? "ظروف گیاهی" : p.ProductType;
        var code = p.ProductCode ?? "";
        var shortDesc = p.ShortDescription ?? p.Description?.StripHtml()?.Trim();
        if (string.IsNullOrWhiteSpace(shortDesc))
            shortDesc = $"{p.Name} — ظرف یکبار مصرف گیاهی (نشاسته) اصل کارخانه آملون، مناسب پخش عمده و کترینگ.";

        var wholesalePart = wholesaleCode != null ? $" | کد لیست عمده {wholesaleCode}" : "";
        p.MetaTitle = $"{p.Name} | خرید عمده {type} آملون{wholesalePart} — موثقی";
        p.MetaDescription =
            $"{p.Name} (کد {code}) — {shortDesc} خرید عمده از فروشگاه موثقی، نمایندگی رسمی آملون در تهران. حداقل سفارش ۱۰ میلیون تومان، تخفیف پلکانی تا ۱۲٪.";
        p.MetaKeywords =
            $"{p.Name}, {type}, آملون, ظروف گیاهی, خرید عمده, کد {code}, فروشگاه موثقی, تهران, نشاسته, biodegradable"
            + (wholesaleCode != null ? $", کد عمده {wholesaleCode}" : "");

        if (string.IsNullOrWhiteSpace(p.ShortDescription))
            p.ShortDescription = shortDesc.Length > 200 ? shortDesc[..200] : shortDesc;
    }

    Dictionary<string, WholesaleMapEntry> LoadWholesaleMap()
    {
        var map = new Dictionary<string, WholesaleMapEntry>(StringComparer.Ordinal);
        var path = Path.Combine(env.ContentRootPath, MapJson);
        if (!File.Exists(path)) return map;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("map", out var mapEl)) return map;
        foreach (var prop in mapEl.EnumerateObject())
        {
            var web = prop.Value.TryGetProperty("web", out var webEl) ? webEl.GetString()! : prop.Name;
            var slug = prop.Value.TryGetProperty("slug", out var slugEl) ? slugEl.GetString() : null;
            var name = prop.Value.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            map[prop.Name] = new WholesaleMapEntry(web, slug, name);
        }

        return map;
    }

    static List<PriceListEntry> ParseEntries(JsonElement arr)
        => arr.EnumerateArray().Select(e => new PriceListEntry(
            e.GetProperty("code").GetString()!,
            e.GetProperty("name").GetString()!,
            e.GetProperty("price").GetDecimal(),
            e.GetProperty("cartons").GetInt32(),
            e.GetProperty("pack").GetInt32(),
            e.GetProperty("type").GetString()!)).ToList();

    static string WebUtilityHtmlDecode(string s)
        => s.Replace("&raquo;", "»").Replace("&nbsp;", " ").Replace("&#8211;", "–").Replace("&amp;", "&");
}

internal static class StringHtmlExt
{
    public static string? StripHtml(this string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;
        return Regex.Replace(html, "<[^>]+>", " ").Trim();
    }
}
