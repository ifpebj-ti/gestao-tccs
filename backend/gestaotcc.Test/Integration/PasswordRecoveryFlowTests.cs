using System.Net;
using System.Net.Http.Json;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Entities.User;
using gestaotcc.WebApi.Config;
using gestaotcc.WebApi.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class PasswordRecoveryFlowTests
{
    [Fact]
    public async Task ValidRecoveryStillWorksOverHttpWithCorsAndTheProtectedCookie()
    {
        var user = new UserEntity { Id = 1, Email = "synthetic@example.com", Password = "old-hash", Status = "ACTIVE",
            AccessCode = new() { Id = 1, UserId = 1, Code = "ABC234", IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5) } };
        var users = Substitute.For<IUserGateway>(); users.FindByEmail(user.Email).Returns(user);
        users.ResetPassword(user.Id, "old-hash", 1, "ABC234", "new-hash").Returns(_ =>
        { user.Password = "new-hash"; user.AccessCode.IsUserUpdatePassword = true; return true; });
        var bcrypt = Substitute.For<IBcryptGateway>(); bcrypt.GenerateHashPassword("Newpass!123").Returns("new-hash");
        var tccs = Substitute.For<ITccGateway>();
        tccs.FindBankingMemberByToken("bank-token").Returns(new gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity
        { Email = user.Email, TokenExpiryDate = DateTime.UtcNow.AddMinutes(5) });
        var tokens = Substitute.For<ITokenGateway>(); tokens.CreateAccessToken(user).Returns("synthetic-session");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.Logging.ClearProviders(); builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(users); builder.Services.AddSingleton(bcrypt);
        builder.Services.AddSingleton(tccs); builder.Services.AddSingleton(tokens);
        builder.Services.AddSingleton(Substitute.For<IAppLoggerGateway<BankingLoginUseCase>>());
        builder.Services.AddScoped<BankingLoginUseCase>();
        builder.Services.AddSingleton(Substitute.For<IAppLoggerGateway<VerifyAccessCodeUseCase>>());
        builder.Services.AddSingleton(Substitute.For<IAppLoggerGateway<UpdatePasswordUseCase>>());
        builder.Services.AddScoped<VerifyAccessCodeUseCase>(); builder.Services.AddScoped<UpdatePasswordUseCase>();
        builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        builder.Services.AddScoped<PasswordResetSession>(); builder.Services.AddOwnPasswordRateLimit();
        builder.Services.AddCorsExtension(builder.Configuration, builder.Environment);
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        await using var app = builder.Build(); app.UseRouting(); app.UseCors("CorsPolicy"); app.UseRateLimiter(); app.MapControllers();
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var handler = new HttpClientHandler { CookieContainer = new CookieContainer(), UseCookies = true };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };
        client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:3000");
        var payload = new { userEmail = user.Email, userPassword = "Newpass!123" };
        using var unauthorized = await client.PostAsJsonAsync("/api/Auth/update-password", payload);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        using var verified = await client.PostAsJsonAsync("/api/AccessCode/verify", new { userEmail = user.Email, accessCode = "ABC234" });
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        Assert.Equal("true", verified.Headers.GetValues("Access-Control-Allow-Credentials").Single());
        Assert.Equal("http://127.0.0.1:3000", verified.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains(PasswordResetSession.CookieName, handler.CookieContainer.GetCookieHeader(new Uri(client.BaseAddress!, "/api/Auth/update-password")));
        using var updated = await client.PostAsJsonAsync("/api/Auth/update-password", payload);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("new-hash", user.Password);
        Assert.DoesNotContain(PasswordResetSession.CookieName, handler.CookieContainer.GetCookieHeader(new Uri(client.BaseAddress!, "/api/Auth/update-password")));
        using var replay = await client.PostAsJsonAsync("/api/Auth/update-password", payload);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        await users.Received(1).ResetPassword(user.Id, "old-hash", 1, "ABC234", "new-hash");
        using var banking = await client.PostAsJsonAsync("/api/Auth/banking-login", new { token = "bank-token" });
        Assert.Equal(HttpStatusCode.OK, banking.StatusCode);
        Assert.Contains("synthetic-session", await banking.Content.ReadAsStringAsync());
    }
}
