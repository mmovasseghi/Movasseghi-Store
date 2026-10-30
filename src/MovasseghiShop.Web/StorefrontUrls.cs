using Microsoft.AspNetCore.WebUtilities;
using MovasseghiShop.Web.Models;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web;

/// <summary>URLهای عمومی فروشگاه برای canonical، OG و sitemap.</summary>
public static class StorefrontUrls
{
    public static string PublicBase(HttpContext ctx, IConfiguration config)
    {
        var configured = config["SiteSettings:PublicBaseUrl"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.TrimEnd('/');

        var pathBase = ctx.Request.PathBase.Value ?? "";
        return $"{ctx.Request.Scheme}://{ctx.Request.Host}{pathBase}".TrimEnd('/');
    }

    /// <summary>Canonical برای لیست کاتالوگ — جستجو و فیلترهای ترکیبی noindex؛ دسته فقط slug؛ صفحه‌بندی با page.</summary>
    public static string CatalogCanonical(HttpContext ctx, IConfiguration config, ProductFilterQuery filter)
    {
        var catalogPath = AppPath.H("/Catalog", ctx);
        var origin = PublicBase(ctx, config);

        if (!string.IsNullOrWhiteSpace(filter.Q))
            return origin + catalogPath + QueryString.Create("q", filter.Q.Trim());

        if (CatalogLandingRules.ShouldShowCategoryLanding(filter)
            && !string.IsNullOrWhiteSpace(filter.Category))
        {
            return origin + catalogPath + QueryString.Create("category", filter.Category.Trim());
        }

        if (filter.Page > 1)
            return origin + catalogPath + QueryString.Create("page", filter.Page.ToString());

        return origin + catalogPath;
    }

    public static string? CatalogRobots(ProductFilterQuery filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Q))
            return "noindex, follow";

        var hasFacet =
            !string.IsNullOrWhiteSpace(filter.ProductType)
            || !string.IsNullOrWhiteSpace(filter.Packaging)
            || filter.Compartments.HasValue
            || filter.Microwave == true
            || !string.IsNullOrWhiteSpace(filter.Sort)
            || !string.IsNullOrWhiteSpace(filter.Capacity);

        if (hasFacet && !CatalogLandingRules.ShouldShowCategoryLanding(filter))
            return "noindex, follow";

        return null;
    }
}
