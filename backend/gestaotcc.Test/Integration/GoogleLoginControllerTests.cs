using System.Security.Claims;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Entities.User;
using gestaotcc.WebApi.Config;
using gestaotcc.WebApi.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class GoogleLoginControllerTests
{
    private const string Scheme = GoogleAuthenticationDefaults.ExternalCookieScheme;
    private const string Email = "aluno@discente.ifpe.edu.br";
    private const string LocalToken = "test-session-token";
    private const string Frontend = "https://gradus.example.org/login";
    private const string Callback = "https://api.example.org/api/Auth/google/callback";

    [Theory]
    [InlineData("ClientId", null)]
    [InlineData("ClientSecret", " ")]
    [InlineData("RedirectUri", "relative/callback")]
    [InlineData("RedirectUri", "http://api.example.org/callback")]
    [InlineData("RedirectUri", "ftp://localhost/callback")]
    [InlineData("RedirectUri", Callback + "?foo=bar")]
    [InlineData("RedirectUri", Callback + "#section")]
    public void Start_RedirectsToUnavailableWhenProviderConfigurationIsIncomplete(string key, string? value)
    {
        var settings = Configuration().AsEnumerable().ToDictionary();
        settings["GoogleAuth:" + key] = value;

        var response = Controller(new DefaultHttpContext()).GoogleLogin(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build());

        Assert.Equal(Frontend + "?googleError=unavailable", Assert.IsType<RedirectResult>(response).Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("relative/login")]
    [InlineData("ftp://gradus.example.org/login")]
    public void Start_ReturnsServiceUnavailableWhenFrontendCannotBeUsed(string? frontend)
    {
        var response = Controller(new DefaultHttpContext()).GoogleLogin(Configuration(frontend: frontend));

        Assert.Equal(503, Assert.IsType<ObjectResult>(response).StatusCode);
    }

    [Theory]
    [InlineData(Callback, Frontend)]
    [InlineData("http://localhost:8080/api/Auth/google/callback", "http://localhost:3000")]
    public void Start_ChallengesGoogleAndSetsTheLocalCompletionRoute(string callback, string frontend)
    {
        var url = Substitute.For<IUrlHelper>();
        url.Action(Arg.Is<UrlActionContext>(action => action.Action == nameof(AuthController.GoogleLoginComplete)
            && action.Controller == "Auth")).Returns("/api/Auth/google/complete");
        var controller = Controller(new DefaultHttpContext());
        controller.Url = url;

        var response = Assert.IsType<ChallengeResult>(controller.GoogleLogin(Configuration(callback, frontend)));

        Assert.Equal(GoogleAuthenticationDefaults.Scheme, Assert.Single(response.AuthenticationSchemes));
        Assert.Equal("/api/Auth/google/complete", response.Properties?.RedirectUri);
    }

    [Fact]
    public void Start_ReturnsServerErrorWhenTheCompletionRouteCannotBeGenerated()
    {
        var controller = Controller(new DefaultHttpContext());
        controller.Url = Substitute.For<IUrlHelper>();

        Assert.Equal(500, Assert.IsType<ObjectResult>(controller.GoogleLogin(Configuration())).StatusCode);
    }

    [Theory]
    [InlineData("email", "true", Frontend, "https", null, true)]
    [InlineData(ClaimTypes.Email, "True", "http://localhost:3000/", "http", "HTTPS", false)]
    [InlineData(ClaimTypes.Email, "true", Frontend, "http", null, true)]
    [InlineData("email", "true", "http://localhost:3000/", "http", null, false)]
    public async Task Complete_SetsSessionCookieAndRedirectsForVerifiedAccount(
        string emailClaim, string verified, string frontend, string requestScheme, string? forwardedProto, bool secure)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = requestScheme;
        if (forwardedProto is not null) context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
        var authentication = Ticket(context, emailClaim, verified);
        using var services = Services(context, authentication);
        var useCase = CreateLoginUseCase(out var users, out var tokens);

        var response = await Controller(context).GoogleLoginComplete(useCase, Configuration(frontend: frontend));

        Assert.Equal(frontend + "?googleLogin=success", Assert.IsType<RedirectResult>(response).Url);
        var cookie = Assert.Single(context.Response.Headers.SetCookie).ToLowerInvariant();
        Assert.StartsWith("token=" + LocalToken, cookie);
        Assert.Contains("samesite=lax", cookie);
        Assert.Equal(secure, cookie.Contains("; secure", StringComparison.Ordinal));
        await authentication.Received(1).SignOutAsync(context, Scheme, null);
        await users.Received(1).FindByEmail(Email);
        tokens.Received(1).CreateAccessToken(Arg.Is<UserEntity>(user => user.Email == Email));
    }

    [Theory]
    [InlineData(false, "email", "true", "discente.ifpe.edu.br")]
    [InlineData(true, null, "true", "discente.ifpe.edu.br")]
    [InlineData(true, "email", null, "discente.ifpe.edu.br")]
    [InlineData(true, "email", "invalid", "discente.ifpe.edu.br")]
    [InlineData(true, "email", "false", "discente.ifpe.edu.br")]
    [InlineData(true, "email", "true", null)]
    public async Task Complete_RejectsMissingTicketOrIneligibleIdentity(
        bool succeeded, string? emailClaim, string? verified, string? domains)
    {
        var context = new DefaultHttpContext();
        var authentication = succeeded ? Ticket(context, emailClaim, verified) : Substitute.For<IAuthenticationService>();
        if (!succeeded) authentication.AuthenticateAsync(context, Scheme).Returns(AuthenticateResult.NoResult());
        using var services = Services(context, authentication);
        var useCase = CreateLoginUseCase(out var users, out var tokens);

        var response = await Controller(context).GoogleLoginComplete(useCase, Configuration(domains: domains));

        Assert.Equal(Frontend + "?googleError=failed", Assert.IsType<RedirectResult>(response).Url);
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
        await authentication.Received(1).SignOutAsync(context, Scheme, null);
        await users.DidNotReceive().FindByEmail(Arg.Any<string>());
        tokens.DidNotReceive().CreateAccessToken(Arg.Any<UserEntity>());
    }

    [Theory]
    [InlineData("relative/login")]
    [InlineData("ftp://gradus.example.org/login")]
    public async Task Complete_DoesNotSetCookieWhenTheFrontendUrlIsInvalid(string frontend)
    {
        var context = new DefaultHttpContext();
        var authentication = Ticket(context, "email", "true");
        using var services = Services(context, authentication);

        var response = await Controller(context).GoogleLoginComplete(
            CreateLoginUseCase(out _, out _), Configuration(frontend: frontend));

        Assert.Equal(500, Assert.IsType<ObjectResult>(response).StatusCode);
        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
        await authentication.Received(1).SignOutAsync(context, Scheme, null);
    }

    private static AuthController Controller(HttpContext context) =>
        new(NullLogger<AuthController>.Instance) { ControllerContext = new ControllerContext { HttpContext = context } };

    private static IConfiguration Configuration(string callback = Callback, string? frontend = Frontend, string? domains = "ifpe.edu.br, , discente.ifpe.edu.br") =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GoogleAuth:ClientId"] = "test-client",
            ["GoogleAuth:ClientSecret"] = "test-secret",
            ["GoogleAuth:RedirectUri"] = callback,
            ["GoogleAuth:AllowedDomains"] = domains,
            ["CORS_SETTINGS:URL_FRONT"] = frontend
        }).Build();

    private static ServiceProvider Services(HttpContext context, IAuthenticationService authentication)
    {
        var services = new ServiceCollection().AddSingleton(authentication).BuildServiceProvider();
        context.RequestServices = services;
        return services;
    }

    private static IAuthenticationService Ticket(HttpContext context, string? emailClaim, string? verified)
    {
        var claims = new List<Claim>();
        if (emailClaim is not null) claims.Add(new Claim(emailClaim, Email));
        if (verified is not null) claims.Add(new Claim("email_verified", verified));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
        var authentication = Substitute.For<IAuthenticationService>();
        authentication.AuthenticateAsync(context, Scheme)
            .Returns(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme)));
        return authentication;
    }

    private static GoogleLoginUseCase CreateLoginUseCase(out IUserGateway users, out ITokenGateway tokens)
    {
        users = Substitute.For<IUserGateway>();
        users.FindByEmail(Email).Returns(new UserEntity { Email = Email, Status = "ACTIVE" });
        tokens = Substitute.For<ITokenGateway>();
        tokens.CreateAccessToken(Arg.Any<UserEntity>()).Returns(LocalToken);
        return new GoogleLoginUseCase(new GoogleAccountEligibilityUseCase(users), tokens);
    }
}
