using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace MovasseghiShop.Web.Infrastructure;

public static class TrustedProxyForwarding
{
    public static ForwardedHeadersOptions CreateOptions()
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.KnownProxies.Add(IPAddress.Loopback);
        options.KnownProxies.Add(IPAddress.IPv6Loopback);
        return options;
    }
}
