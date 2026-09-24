using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;

namespace MovasseghiShop.Web.Areas.Admin;

/// <summary>نشان‌های منوی ادمین در ViewData برای لایهٔ مشترک.</summary>
public class AdminNavBadgeFilter(IAdminNavBadgeService badges) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.Controller is Microsoft.AspNetCore.Mvc.Controller controller
            && context.ActionDescriptor is ControllerActionDescriptor
            && string.Equals(context.RouteData.Values["area"] as string, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var ct = context.HttpContext.RequestAborted;
            var openInq = await badges.GetOpenInquiryCountAsync(ct);
            var openOrders = await badges.GetOpenOrderCountAsync(ct);
            controller.ViewData["OpenInquiryCount"] = openInq;
            controller.ViewData["OpenOrderCount"] = openOrders;
            context.HttpContext.Items["OpenInquiryCount"] = openInq;
            context.HttpContext.Items["OpenOrderCount"] = openOrders;
        }

        await next();
    }
}

/// <summary>پوشش نازک روی IMemoryCache برای کش async قابل تست.</summary>
public interface IMemoryCacheAccessor
{
    Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory);
    void Remove(string key);
}

public class MemoryCacheAccessor(Microsoft.Extensions.Caching.Memory.IMemoryCache cache) : IMemoryCacheAccessor
{
    public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory)
    {
        if (cache.TryGetValue(key, out var cached) && cached is T hit) return hit;

        var value = await factory();
        using (var entry = cache.CreateEntry(key))
        {
            entry.Value = value;
            entry.AbsoluteExpirationRelativeToNow = ttl;
        }
        return value;
    }

    public void Remove(string key) => cache.Remove(key);
}
