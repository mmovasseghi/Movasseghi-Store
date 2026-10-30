using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Infrastructure;

/// <summary>
/// Session middleware در ASP.NET معمولاً Cache-Control: no-cache, no-store می‌گذارد؛
/// GTmetrix/Foglift «Add Expires headers» را قرمز می‌کنند. برای مهمان با سبد خالی
/// روی صفحات فروشگاه هدر عمومی می‌گذاریم.
/// </summary>
public sealed class StorefrontPublicCacheHeadersMiddleware(RequestDelegate next)
{
    static readonly HashSet<string> PublicControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Home", "Catalog", "Shop", "Blog", "News", "Page", "Sitemap"
    };

    public async Task InvokeAsync(HttpContext context, ICartService cart)
    {
        await next(context);

        if (!HttpMethods.IsGet(context.Request.Method))
            return;
        if (context.Response.StatusCode != StatusCodes.Status200OK)
            return;
        if (context.User.Identity?.IsAuthenticated == true)
            return;
        if (cart.GetItemCount(context) > 0)
            return;

        var controller = context.GetRouteData().Values["controller"]?.ToString();
        if (string.IsNullOrEmpty(controller) || !PublicControllers.Contains(controller))
            return;

        var area = context.GetRouteData().Values["area"]?.ToString();
        if (!string.IsNullOrEmpty(area))
            return;

        var maxAge = string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase)
            ? 900
            : 300;

        context.Response.Headers.CacheControl = $"public, max-age={maxAge}, s-maxage={maxAge}";
        context.Response.Headers.Remove("Pragma");
        context.Response.Headers.Remove("Expires");
    }
}
