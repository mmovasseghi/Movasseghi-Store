using Microsoft.AspNetCore.OutputCaching;
using MovasseghiShop.Web.Services;

namespace MovasseghiShop.Web.Infrastructure;

/// <summary>Output cache فقط برای مهمان با سبد خالی — جلوگیری از HTML شخصی‌سازی‌شده در کش.</summary>
public sealed class GuestPublicOutputCachePolicy : IOutputCachePolicy
{
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        if (http.User.Identity?.IsAuthenticated == true)
            DisableCache(context);
        else
        {
            var cart = http.RequestServices.GetService<ICartService>();
            if (cart != null && cart.GetItemCount(http) > 0)
                DisableCache(context);
        }

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
