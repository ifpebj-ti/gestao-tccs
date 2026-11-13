using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Infra.Gateways;
using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class SecurityConfigurationTests
{
    [Fact]
    public void TokenLifetimeKeepsTheExistingHoursConfiguration()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["JWT_SETTINGS:TOKEN_KEY"] = "security-test-only-key-at-least-thirty-two-bytes", ["JWT_SETTINGS:ACCESSTOKEN_EXPIRATION_TIME"] = "1" }).Build();
        var before = DateTime.UtcNow;
        var token = new TokenGateway(config).CreateAccessToken(new UserEntity { Id = 1, Name = "Synthetic", Profile = [] });
        var expires = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo;
        Assert.InRange(expires, before.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));
    }

    [Fact]
    public async Task PasswordRoleAndStatusChangesRevokePreviouslyIssuedSessions()
    {
        var user = new UserEntity { Id = 1, Name = "Synthetic", Password = "old-hash", Status = "ACTIVE",
            Profile = [new gestaotcc.Domain.Entities.Profile.ProfileEntity { Role = "STUDENT" }] };
        var users = Substitute.For<gestaotcc.Application.Gateways.IUserGateway>(); users.FindById(1).Returns(user);
        var builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        { ["JWT_SETTINGS:TOKEN_KEY"] = "security-test-only-key-at-least-thirty-two-bytes", ["JWT_SETTINGS:ACCESSTOKEN_EXPIRATION_TIME"] = "30" });
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(users); builder.Services.AddAuthenticationExtension(builder.Configuration);
        builder.Services.AddAuthorization();
        await using var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization();
        app.MapGet("/private", () => "ok").RequireAuthorization();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var token = new TokenGateway(builder.Configuration).CreateAccessToken(user);
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/private")).StatusCode);
        user.Password = "new-hash";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/private")).StatusCode);
        user.Password = "old-hash"; user.Status = "INACTIVE";
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/private")).StatusCode);
        user.Status = "ACTIVE"; user.Profile = [new gestaotcc.Domain.Entities.Profile.ProfileEntity { Role = "ADMIN" }];
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/private")).StatusCode);
        user.Profile = [new gestaotcc.Domain.Entities.Profile.ProfileEntity { Role = "STUDENT" }]; user.CampiCourseId = 42;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/private")).StatusCode);
    }

    [Fact]
    public async Task PublicAuthenticationThrottlesAndDoesNotTrustSpoofedForwardedHeaders()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddOwnPasswordRateLimit();
        await using var app = builder.Build();
        app.UseRateLimiter();
        app.MapPost("/auth", () => "ok").RequireRateLimiting("public-auth");
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        for (var i = 0; i < 10; i++)
        {
            using var result = await client.PostAsync("/auth", null);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.10");
        using var rejected = await client.PostAsync("/auth", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(60), rejected.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public void ProductionCorsAllowsOnlyConfiguredOrigin()
    {
        var environment = Substitute.For<IHostEnvironment>(); environment.EnvironmentName = "Production";
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["CORS_SETTINGS:URL_FRONT"] = "https://app.example.com" }).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddCorsExtension(config, environment);
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy("CorsPolicy")!;
        Assert.True(policy.IsOriginAllowed("https://app.example.com"));
        Assert.False(policy.IsOriginAllowed("https://evil.example.com"));
        Assert.False(policy.IsOriginAllowed("http://localhost:3000"));
        Assert.True(policy.SupportsCredentials);
    }

    [Fact]
    public void JwtRequiresSignatureExpirationIssuerAudienceAndApprovedAlgorithm()
    {
        const string key = "security-test-only-64-character-key-not-used-in-production-12345678";
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["JWT_SETTINGS:TOKEN_KEY"] = key, ["JWT_SETTINGS:ACCESSTOKEN_EXPIRATION_TIME"] = "1" }).Build();
        var services = new ServiceCollection().AddLogging(); services.AddAuthenticationExtension(config);
        using var provider = services.BuildServiceProvider();
        var validation = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
        var handler = new JwtSecurityTokenHandler();
        var valid = new TokenGateway(config).CreateAccessToken(new UserEntity
        { Id = 1, Name = "Synthetic", Profile = [] })!;
        Assert.NotNull(handler.ValidateToken(valid, validation, out _));
        foreach (var variant in new[] { "audience", "issuer", "expiry", "algorithm" })
        {
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = variant == "issuer" ? "another-service" : "gradusflow",
                Audience = variant == "audience" ? "another-app" : "gradusflow.web",
                IssuedAt = DateTime.UtcNow.AddHours(-2), NotBefore = DateTime.UtcNow.AddHours(-2),
                Expires = variant == "expiry" ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    variant == "algorithm" ? SecurityAlgorithms.HmacSha384 : SecurityAlgorithms.HmacSha256)
            };
            var token = handler.WriteToken(handler.CreateToken(descriptor));
            Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(token, validation, out _));
        }
    }

    [Fact]
    public void JwtConfigurationRejectsShortSecrets()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["JWT_SETTINGS:TOKEN_KEY"] = "short" }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddAuthenticationExtension(config));
    }
}
