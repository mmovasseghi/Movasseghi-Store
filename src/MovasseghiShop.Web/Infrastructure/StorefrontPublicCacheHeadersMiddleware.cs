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
        var isPublic = TryGetMaxAge(context, cart, out var maxAge);
        context.Response.OnStarting(() =>
        {
            // Never make errors, session/antiforgery cookies or explicitly
            // private responses cacheable at a shared proxy.
            var response = context.Response;
            if (!isPublic || response.StatusCode != StatusCodes.Status200OK
                || response.Headers.ContainsKey("Set-Cookie")
                || response.Headers.CacheControl.ToString().Contains("private", StringComparison.OrdinalIgnoreCase)
                || response.Headers.CacheControl.ToString().Contains("no-store", StringComparison.OrdinalIgnoreCase))
            {
                if (!response.Headers.CacheControl.ToString().Contains("no-store", StringComparison.OrdinalIgnoreCase))
                    response.Headers.CacheControl = "private, no-store";
                return Task.CompletedTask;
            }

            context.Response.Headers.CacheControl = $"public, max-age={maxAge}, s-maxage={maxAge}";
            context.Response.Headers.Remove("Pragma");
            context.Response.Headers.Remove("Expires");
            return Task.CompletedTask;
        });

        await next(context);
    }

    static bool TryGetMaxAge(HttpContext context, ICartService cart, out int maxAge)
    {
        maxAge = 0;
        if (!HttpMethods.IsGet(context.Request.Method))
            return false;

        if (context.User.Identity?.IsAuthenticated == true)
            return false;

        if (!string.IsNullOrEmpty(context.Request.Headers.Authorization))
            return false;

        if (context.Request.Cookies.ContainsKey(".AspNetCore.Session"))
            return false;

        if (!CartLooksEmpty(context, cart))
            return false;

        var path = context.Request.Path.Value ?? "/";
        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Checkout", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Cart", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path == "/" || path.Equals("/Home", StringComparison.OrdinalIgnoreCase))
        {
            maxAge = 900;
            return true;
        }

        if (path.StartsWith("/Catalog", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Shop", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Blog", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/News", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/sitemap", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/llms.txt", StringComparison.OrdinalIgnoreCase))
        {
            maxAge = 300;
            return true;
        }

        var controller = context.GetRouteData().Values["controller"]?.ToString();
        if (!string.IsNullOrEmpty(controller) && PublicControllers.Contains(controller)
            && string.IsNullOrEmpty(context.GetRouteData().Values["area"]?.ToString()))
        {
            maxAge = string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase) ? 900 : 300;
            return true;
        }

        return false;
    }

    static bool CartLooksEmpty(HttpContext context, ICartService cart)
    {
        if (context.User.Identity?.IsAuthenticated == true)
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

        return CartService.ReadCountCookie(context) == 0;
    }
}
