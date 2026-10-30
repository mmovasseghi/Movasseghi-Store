using Microsoft.AspNetCore.OutputCaching;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Infrastructure;

/// <summary>Output cache فقط برای مهمان با سبد خالی — جلوگیری از HTML شخصی‌سازی‌شده در کش.</summary>
public sealed class GuestPublicOutputCachePolicy : IOutputCachePolicy
{
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        if (http.User.Identity?.IsAuthenticated == true
            || !string.IsNullOrEmpty(http.Request.Headers.Authorization))
            DisableCache(context);
        else if (CartService.ReadCountCookie(http) > 0
            || http.Request.Cookies.ContainsKey(".AspNetCore.Session"))
            DisableCache(context);

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    static void DisableCache(OutputCacheContext context)
    {
        context.AllowCacheLookup = false;
        context.AllowCacheStorage = false;
        context.AllowLocking = false;
    }
}
