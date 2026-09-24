using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Models.Entities;
using MovasseghiShop.Web.Models.Enums;
using System.Text.RegularExpressions;

namespace MovasseghiShop.Web.Services;

public interface IProductCatalogService
{
    Task<(List<Product> Items, CatalogFilterModel Facets, int Total)> QueryAsync(ProductFilterQuery q, CancellationToken ct = default);
    Task<List<SearchSuggestionDto>> SearchSuggestionsAsync(string term, int limit = 8, CancellationToken ct = default);
    Task<List<Product>> FindSimilarProductsAsync(string term, int limit = 6, CancellationToken ct = default);
    Task<List<SearchSuggestionDto>> FindSimilarSuggestionsAsync(string term, int limit = 6, CancellationToken ct = default);
    Task<List<string>> GetUseCasesAsync(CancellationToken ct = default);
    Task<List<(string Type, int Count)>> GetProductTypesAsync(CancellationToken ct = default);
    Task<string?> TryResolveDirectProductSlugAsync(string? term, CancellationToken ct = default);
}

public class ProductCatalogService(
    ApplicationDbContext db,
    IWebHostEnvironment env,
    IMemoryCache cache,
    IHttpContextAccessor httpContextAccessor) : IProductCatalogService
{
    private const int PageSize = 24;
    const string TypesCacheKey = "catalog:product-types";

    public async Task<(List<Product> Items, CatalogFilterModel Facets, int Total)> QueryAsync(ProductFilterQuery q, CancellationToken ct = default)
    {
        var baseQuery = db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => p.IsActive);

        baseQuery = ApplyFilters(baseQuery, q);

        var total = await baseQuery.CountAsync(ct);
        var facets = await BuildFacetsAsync(db.Products.Where(p => p.IsActive), q, ct);

        var sort = q.Sort switch
        {
            "name" => baseQuery.OrderBy(p => p.Name),
            "code" => baseQuery.OrderBy(p => p.ProductCode),
            "newest" => baseQuery.OrderByDescending(p => p.CreatedAt),
            _ => baseQuery.OrderBy(p => p.Category.SortOrder).ThenBy(p => p.Name)
        };

        var items = await sort
            .Skip((Math.Max(1, q.Page) - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(ct);

        facets.TotalResults = total;
        return (items, facets, total);
    }

    public async Task<string?> TryResolveDirectProductSlugAsync(string? term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2) return null;
        term = NormalizeFa(term.Trim());

        var product = await ProductStorefrontResolver.ResolveActiveBySlugAsync(db, term, ct);
        if (product != null)
            return product.Slug;

        var suggestions = await SearchSuggestionsAsync(term, 3, ct);
        if (suggestions.Count == 0) return null;

        var top = suggestions[0];
        if (string.Equals(top.Slug, term, StringComparison.OrdinalIgnoreCase))
            return top.Slug;

        if (ProductStorefrontResolver.IsStrawLegacySlug(term)
            && string.Equals(top.ProductCode, ProductStorefrontResolver.StrawProductCode, StringComparison.Ordinal))
            return top.Slug;

        if (term.Length <= 4 && top.Name.StartsWith(term, StringComparison.OrdinalIgnoreCase))
            return top.Slug;

        return null;
    }

    public async Task<List<SearchSuggestionDto>> SearchSuggestionsAsync(string term, int limit = 8, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2) return [];
        term = NormalizeFa(term.Trim());
        var products = await db.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive && (
                p.Name.Contains(term) ||
                p.Slug.Contains(term) ||
                (p.ProductCode != null && p.ProductCode.Contains(term))))
            .Take(80)
            .ToListAsync(ct);

        var tokens = Tokenize(term).ToArray();
        return products
            .Select(p => (Product: p, Score: ScoreSuggestion(p, term, tokens)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Product.Name.Length)
            .ThenBy(x => x.Product.Name)
            .Take(limit)
            .Select(x => ToSuggestionDto(x.Product))
            .ToList();
    }

    public async Task<List<Product>> FindSimilarProductsAsync(string term, int limit = 6, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2) return [];
        term = NormalizeFa(term.Trim());
        var tokens = Tokenize(term).ToArray();
        if (tokens.Length == 0) return [];

        var candidateIds = new HashSet<int>();
        foreach (var token in tokens)
        {
            var ids = await db.Products.AsNoTracking()
                .Where(p => p.IsActive && (
                    p.Name.Contains(token) ||
                    (p.ProductCode != null && p.ProductCode.Contains(token)) ||
                    (p.ShortDescription != null && p.ShortDescription.Contains(token)) ||
                    (p.ProductType != null && p.ProductType.Contains(token)) ||
                    (p.UseCaseTags != null && p.UseCaseTags.Contains(token)) ||
                    (p.Material != null && p.Material.Contains(token)) ||
                    (p.Applications != null && p.Applications.Contains(token)) ||
                    p.Category.Name.Contains(token)))
                .OrderBy(p => p.Name)
                .Select(p => p.Id)
                .Take(40)
                .ToListAsync(ct);
            foreach (var id in ids) candidateIds.Add(id);
        }

        List<Product> candidates;
        if (candidateIds.Count == 0)
        {
            candidates = await db.Products.AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);
            return candidates;
        }

        candidates = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Where(p => candidateIds.Contains(p.Id))
            .ToListAsync(ct);

        return candidates
            .Select(p => (Product: p, Score: ScoreSuggestion(p, term, tokens)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Product.Name.Length)
            .ThenBy(x => x.Product.Name)
            .Take(limit)
            .Select(x => x.Product)
            .ToList();
    }

    public async Task<List<SearchSuggestionDto>> FindSimilarSuggestionsAsync(
        string term, int limit = 6, CancellationToken ct = default)
        => (await FindSimilarProductsAsync(term, limit, ct)).Select(ToSuggestionDto).ToList();

    // نمونه‌ای (نه static) تا بتواند WebRootPath را ببیند و نسخه WebP تصویر را برگرداند
    SearchSuggestionDto ToSuggestionDto(Product p)
    {
        var img = ProductImageHelper.GetDisplayUrl(ProductImageHelper.GetPrimary(p.Images), env);
        var ctx = httpContextAccessor.HttpContext;
        if (ctx != null && !string.IsNullOrEmpty(img))
            img = AppPath.H(img, ctx);
        return new SearchSuggestionDto(p.Name, p.Slug, img, p.ProductCode);
    }

    static int ScoreSuggestion(Product p, string fullTerm, string[] tokens)
    {
        var score = ScoreProduct(p, fullTerm, tokens);
        if (string.Equals(p.Slug, fullTerm, StringComparison.OrdinalIgnoreCase)) score += 100;
        if (string.Equals(NormalizeFa(p.Name), fullTerm, StringComparison.OrdinalIgnoreCase)) score += 90;
        if (p.Name.StartsWith(fullTerm, StringComparison.OrdinalIgnoreCase)) score += 40;
        if (fullTerm.Length <= 4 && NameContainsWholeToken(p.Name, fullTerm)) score += 35;
        return score;
    }

    static bool NameContainsWholeToken(string name, string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        return Regex.IsMatch(name, $@"(^|[\s،\-–—]){Regex.Escape(token)}([\s،\-–—]|$)",
            RegexOptions.CultureInvariant);
    }

    static int ScoreProduct(Product p, string fullTerm, string[] tokens)
    {
        var score = 0;
        if (p.Name.Contains(fullTerm, StringComparison.OrdinalIgnoreCase)) score += 20;
        if (p.ProductCode?.Contains(fullTerm, StringComparison.OrdinalIgnoreCase) == true) score += 15;

        foreach (var token in tokens)
        {
            if (p.Name.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 10;
            if (p.ProductCode?.Contains(token, StringComparison.OrdinalIgnoreCase) == true) score += 8;
            if (p.ProductType?.Contains(token, StringComparison.OrdinalIgnoreCase) == true) score += 6;
            if (p.Category.Name.Contains(token, StringComparison.OrdinalIgnoreCase)) score += 5;
            if (p.ShortDescription?.Contains(token, StringComparison.OrdinalIgnoreCase) == true) score += 4;
            if (p.UseCaseTags?.Contains(token, StringComparison.OrdinalIgnoreCase) == true) score += 3;
            if (p.Material?.Contains(token, StringComparison.OrdinalIgnoreCase) == true) score += 2;
            if (p.Applications?.Contains(token, StringComparison.OrdinalIgnoreCase) == true) score += 2;
            if (p.CapacityCc.HasValue && token.All(char.IsDigit) && p.CapacityCc.Value.ToString().Contains(token)) score += 4;
        }

        return score;
    }

    static string NormalizeFa(string s) =>
        s.Replace('ي', 'ی').Replace('ك', 'ک').Replace('ة', 'ه').Trim();

    static IEnumerable<string> Tokenize(string term)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in term.Split([' ', '،', ',', '-', '_', '/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length >= 2) tokens.Add(part);
            foreach (Match m in Regex.Matches(part, @"[\p{L}]{2,}|\d{2,}"))
                tokens.Add(m.Value);
        }
        if (tokens.Count == 0 && term.Length >= 2) tokens.Add(term);
        return tokens.Take(6);
    }

    public async Task<List<string>> GetUseCasesAsync(CancellationToken ct = default)
    {
        var tags = await db.Products
            .Where(p => p.IsActive && p.UseCaseTags != null)
            .Select(p => p.UseCaseTags!)
            .ToListAsync(ct);

        return tags
            .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => g.Key)
            .ToList();
    }

    public async Task<List<(string Type, int Count)>> GetProductTypesAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(TypesCacheKey, out List<(string Type, int Count)>? cached) && cached is not null)
            return cached;

        var rows = await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.ProductType != null)
            .GroupBy(p => p.ProductType!)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var result = rows.Select(x => (x.Type, x.Count)).ToList();
        cache.Set(TypesCacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        });
        return result;
    }

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, ProductFilterQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var term = q.Q.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.ProductCode != null && p.ProductCode.Contains(term)) ||
                (p.ShortDescription != null && p.ShortDescription.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(q.Category))
            query = query.Where(p => p.Category.Slug == q.Category || p.Category.Name == q.Category);

        if (!string.IsNullOrWhiteSpace(q.ProductType))
            query = query.Where(p => p.ProductType == q.ProductType);

        if (!string.IsNullOrWhiteSpace(q.UseCase))
        {
            var tag = q.UseCase.Trim();
            query = query.Where(p => p.UseCaseTags != null && p.UseCaseTags.Contains(tag));
        }

        if (!string.IsNullOrWhiteSpace(q.Packaging))
        {
            query = q.Packaging switch
            {
                "bulk" => query.Where(p => p.Variants.Any(v => v.IsActive && (
                    v.PackagingType == PackagingType.Bulk
                    || v.SellUnit == SellUnitType.BulkNylon
                    || v.SellUnit == SellUnitType.BulkCarton))),
                "shrink" => query.Where(p => p.Variants.Any(v => v.IsActive && (
                    v.PackagingType == PackagingType.Shrink
                    || v.SellUnit == SellUnitType.ShrinkPack
                    || v.SellUnit == SellUnitType.ShrinkCarton))),
                _ => query
            };
        }

        if (q.CapacityMin.HasValue)
            query = query.Where(p => p.CapacityCc >= q.CapacityMin);
        if (q.CapacityMax.HasValue)
            query = query.Where(p => p.CapacityCc <= q.CapacityMax);
        if (!string.IsNullOrWhiteSpace(q.Capacity))
            query = CatalogCapacityBands.Apply(query, q.Capacity);
        if (q.Compartments.HasValue)
            query = query.Where(p => p.CompartmentCount == q.Compartments);
        if (q.Microwave == true)
            query = query.Where(p => p.MicrowaveSafe);

        return query;
    }

    private static async Task<CatalogFilterModel> BuildFacetsAsync(IQueryable<Product> all, ProductFilterQuery q, CancellationToken ct)
    {
        var facetQuery = new ProductFilterQuery { Q = q.Q, Sort = q.Sort, Page = q.Page };
        var scoped = ApplyFilters(all, facetQuery);

        var categories = await scoped.GroupBy(p => new { p.Category.Slug, p.Category.Name })
            .Select(g => new { g.Key.Slug, g.Key.Name, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var types = await scoped.Where(p => p.ProductType != null)
            .GroupBy(p => p.ProductType!)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var useCases = new List<FilterOption>();

        var bulk = await scoped.CountAsync(p => p.Variants.Any(v => v.IsActive && (
            v.PackagingType == PackagingType.Bulk
            || v.SellUnit == SellUnitType.BulkNylon
            || v.SellUnit == SellUnitType.BulkCarton)), ct);
        var shrink = await scoped.CountAsync(p => p.Variants.Any(v => v.IsActive && (
            v.PackagingType == PackagingType.Shrink
            || v.SellUnit == SellUnitType.ShrinkPack
            || v.SellUnit == SellUnitType.ShrinkCarton)), ct);
        var microwave = await scoped.CountAsync(p => p.MicrowaveSafe, ct);

        return new CatalogFilterModel
        {
            Categories = categories.Select(c => new FilterOption(c.Slug, c.Name, c.Count)).ToList(),
            ProductTypes = types.Select(t => new FilterOption(t.Type, t.Type, t.Count)).ToList(),
            UseCases = useCases,
            Packaging =
            [
                new FilterOption("bulk", "فله", bulk),
                new FilterOption("shrink", "شیرینگ", shrink)
            ],
            MicrowaveCount = microwave
        };
    }

    public static int PageSizeValue => PageSize;
}
