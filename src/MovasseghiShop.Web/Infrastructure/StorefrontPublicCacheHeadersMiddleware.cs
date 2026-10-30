using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Infrastructure;

/// <summary>
/// Session معمولاً Cache-Control: no-cache, no-store می‌گذارد. این middleware قبل از Session
/// OnStarting ثبت می‌کند تا بعد از Session اجرا شود و هدر public را نهایی کند.
/// </summary>
public sealed class StorefrontPublicCacheHeadersMiddleware(RequestDelegate next)
{
    static readonly HashSet<string> PublicControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Home", "Catalog", "Shop", "Blog", "News", "Page", "Sitemap"
    };

    public async Task InvokeAsync(HttpContext context, ICartService cart)
    {
        if (TryGetMaxAge(context, cart, out var maxAge))
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.CacheControl = $"public, max-age={maxAge}, s-maxage={maxAge}";
                context.Response.Headers.Remove("Pragma");
                context.Response.Headers.Remove("Expires");
                return Task.CompletedTask;
            });
        }

        await next(context);
    }

    static bool TryGetMaxAge(HttpContext context, ICartService cart, out int maxAge)
    {
        maxAge = 0;
        if (!HttpMethods.IsGet(context.Request.Method))
            return false;

        if (context.User.Identity?.IsAuthenticated == true)
            return false;

        if (!CartLooksEmpty(context, cart))
            return false;

        var controller = context.GetRouteData().Values["controller"]?.ToString();
        if (string.IsNullOrEmpty(controller) || !PublicControllers.Contains(controller))
            return false;

        var area = context.GetRouteData().Values["area"]?.ToString();
        if (!string.IsNullOrEmpty(area))
            return false;

        maxAge = string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase) ? 900 : 300;
        return true;
    }

    static bool CartLooksEmpty(HttpContext context, ICartService cart)
    {
        try
        {
            return cart.GetItemCount(context) == 0;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }
}
