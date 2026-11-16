using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace gestaotcc.Test.Config;

public class GoogleAuthenticationConfigurationTests
{
    [Fact]
    public void AuthenticationOptions_UseValidatedGoogleCodeFlowAndConfiguredCallback()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_SETTINGS:TOKEN_KEY"] = new string('k', 32),
                ["JWT_SETTINGS:ACCESSTOKEN_EXPIRATION_TIME"] = "8",
                ["GoogleAuth:ClientId"] = "test-client-id",
                ["GoogleAuth:ClientSecret"] = "test-client-secret",
                ["GoogleAuth:RedirectUri"] = "http://localhost:8080/api/Auth/google/callback"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAuthenticationExtension(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(GoogleAuthenticationDefaults.Scheme);

        Assert.Equal("https://accounts.google.com", options.Authority);
        Assert.Equal("test-client-id", options.ClientId);
        Assert.Equal("test-client-secret", options.ClientSecret);
        Assert.Equal("/api/Auth/google/callback", options.CallbackPath);
        Assert.Equal(OpenIdConnectResponseType.Code, options.ResponseType);
        Assert.True(options.UsePkce);
        Assert.True(options.ProtocolValidator.RequireNonce);
        Assert.Contains("email", options.Scope);
        Assert.Contains("profile", options.Scope);
        Assert.Equal(typeof(GoogleOpenIdConnectEvents), options.EventsType);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.Equal("test-client-id", options.TokenValidationParameters.ValidAudience);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
    }
}
