using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace gestaotcc.WebApi.Config;

public static class TrustedProxy
{
    public static ForwardedHeadersOptions? FromConfiguration(IConfiguration configuration)
    {
        var addresses = (configuration["ReverseProxy:TrustedAddresses"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (addresses.Length == 0) return null;

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var address in addresses)
        {
            if (!IPAddress.TryParse(address, out var proxy) || proxy.Equals(IPAddress.Any) || proxy.Equals(IPAddress.IPv6Any))
                throw new InvalidOperationException("ReverseProxy:TrustedAddresses deve conter somente IPs explícitos do proxy.");

            options.KnownProxies.Add(proxy);
        }
        return options;
    }
}
