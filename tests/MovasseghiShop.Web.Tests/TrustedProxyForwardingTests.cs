using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MovasseghiShop.Web.Infrastructure;
using Xunit;

namespace MovasseghiShop.Web.Tests;

public class TrustedProxyForwardingTests
{
    [Theory]
    [InlineData("127.0.0.1", "https")]
    [InlineData("::1", "https")]
    [InlineData("::ffff:127.0.0.1", "https")]
    [InlineData("203.0.113.19", "http")]
    [InlineData("127.0.0.2", "http")]
    public async Task Only_the_local_reverse_proxy_can_change_the_effective_scheme(string remote, string expected)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.8";
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask,
            NullLoggerFactory.Instance, Options.Create(TrustedProxyForwarding.CreateOptions()));
        await middleware.Invoke(context);
        Assert.Equal(expected, context.Request.Scheme);
        Assert.Equal(expected == "https" ? "198.51.100.8" : remote, context.Connection.RemoteIpAddress!.ToString());
    }
}
