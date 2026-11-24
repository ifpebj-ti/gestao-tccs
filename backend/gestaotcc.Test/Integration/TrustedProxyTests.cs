using System.Net;
using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace gestaotcc.Test.Integration;

public class TrustedProxyTests
{
    [Theory]
    [InlineData("192.0.2.10", "192.0.2.20", "https")]
    [InlineData("192.0.2.11", "192.0.2.11", "http")]
    public async Task OnlyTheConfiguredProxyCanForwardAddressAndScheme(string peer, string expected, string scheme)
    {
        var options = TrustedProxy.FromConfiguration(Configure("192.0.2.10"))!;
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        context.Request.Headers["X-Forwarded-For"] = "192.0.2.20";
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));

        await middleware.Invoke(context);

        Assert.Equal(IPAddress.Parse(expected), context.Connection.RemoteIpAddress);
        Assert.Equal(scheme, context.Request.Scheme);
    }

    [Fact]
    public void MissingConfigurationDisablesForwardingAndWildcardFailsValidation()
    {
        Assert.Null(TrustedProxy.FromConfiguration(Configure(null)));
        Assert.Throws<InvalidOperationException>(() => TrustedProxy.FromConfiguration(Configure("0.0.0.0")));
    }

    private static IConfiguration Configure(string? proxies) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ReverseProxy:TrustedAddresses"] = proxies }).Build();
}
