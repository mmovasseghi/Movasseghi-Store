using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using MovasseghiShop.Web.Data;
using MovasseghiShop.Web.Infrastructure;
using MovasseghiShop.Web.Services;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class PublicCacheSafetyTests
{
    [Theory]
    [InlineData("/", "", 200, "", "", true)]
    [InlineData("/", "_mv=analytics-only", 200, "", "", true)]
    [InlineData("/", "ms_cart_n=2", 200, "", "", false)]
    [InlineData("/", ".AspNetCore.Session=session", 200, "", "", false)]
    [InlineData("/", "", 500, "", "", false)]
    [InlineData("/Shop/Product/missing", "", 404, "", "", false)]
    [InlineData("/", "", 200, "session=new", "no-store", false)]
    [InlineData("/", "", 200, "", "private, max-age=0", false)]
    [InlineData("/", "", 200, "", "no-store", false)]
    [InlineData("/Cart/Panel", "", 200, "", "", false)]
    [InlineData("/Order/Invoice", "", 200, "", "", false)]
    [InlineData("/Payment/Mock", "", 200, "", "", false)]
    public async Task Shared_cache_headers_are_only_added_to_public_cookie_free_successes(
        string path, string cookie, int status, string setCookie, string cacheControl, bool expectedPublic)
    {
        var response = new StartingResponseFeature();
        var http = new DefaultHttpContext();
        http.Features.Set<IHttpResponseFeature>(response);
        http.Request.Method = "GET";
        http.Request.Path = path;
        if (cookie.Length > 0) http.Request.Headers.Cookie = cookie;
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var middleware = new StorefrontPublicCacheHeadersMiddleware(context =>
        {
            context.Response.StatusCode = status;
            if (setCookie.Length > 0) context.Response.Headers.SetCookie = setCookie;
            if (cacheControl.Length > 0) context.Response.Headers.CacheControl = cacheControl;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(http, new CartService(db));
        await response.StartAsync();

        Assert.Equal(expectedPublic, http.Response.Headers.CacheControl.ToString().StartsWith("public"));
        if (!expectedPublic) Assert.Contains("no-store", http.Response.Headers.CacheControl.ToString());
        Assert.Equal(setCookie, http.Response.Headers.SetCookie.ToString());
    }

    [Theory]
    [InlineData("_mv=visitor", "", true)]
    [InlineData(".AspNetCore.Session=existing", "", false)]
    [InlineData("ms_cart_n=1", "", false)]
    [InlineData("", "Bearer local-test", false)]
    public async Task Output_cache_does_not_share_session_or_cart_responses(string cookie, string authorization, bool allowed)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers.Cookie = cookie;
        http.Request.Headers.Authorization = authorization;
        var context = new OutputCacheContext
        {
            HttpContext = http,
            AllowCacheLookup = true,
            AllowCacheStorage = true,
            AllowLocking = true
        };
        await new GuestPublicOutputCachePolicy().CacheRequestAsync(context, default);
        Assert.Equal(allowed, context.AllowCacheLookup);
        Assert.Equal(allowed, context.AllowCacheStorage);
    }

    sealed class StartingResponseFeature : HttpResponseFeature
    {
        readonly List<(Func<object, Task> Callback, object State)> callbacks = [];
        public override void OnStarting(Func<object, Task> callback, object state) => callbacks.Add((callback, state));
        public async Task StartAsync()
        {
            for (var i = callbacks.Count - 1; i >= 0; i--)
                await callbacks[i].Callback(callbacks[i].State);
        }
    }
}
