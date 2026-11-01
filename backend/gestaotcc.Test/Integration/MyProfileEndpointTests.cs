using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using gestaotcc.WebApi.Controllers;
using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace gestaotcc.Test.Integration;

// Host isolado: controller e binding reais, gateway fictício, sem banco ou credenciais.
public class MyProfileEndpointTests : IAsyncLifetime
{
    private readonly IUserGateway _gateway = Substitute.For<IUserGateway>();
    private readonly IBcryptGateway _bcrypt = Substitute.For<IBcryptGateway>();
    private readonly UserEntity _user = new()
    {
        Id = 21, Name = "Pessoa de Teste", Email = "teste@discente.ifpe.edu.br",
        Phone = "81988888888", Password = "nao-expor", CPF = "123.456.789-09",
        Status = "ACTIVE", Profile = new List<ProfileEntity> { new() { Role = "STUDENT" } }
    };
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _gateway.FindById(21).Returns(_user);
        _gateway.UpdatePhone(21, Arg.Any<string>()).Returns(true);
        _gateway.UpdateStudentProfile(21, Arg.Any<gestaotcc.Domain.Dtos.User.UpdateStudentProfileDTO>(), Arg.Any<string>()).Returns(true);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(_gateway);
        builder.Services.AddScoped<MyProfileUseCase>();
        builder.Services.AddSingleton(_bcrypt);
        builder.Services.AddScoped<ChangeOwnPasswordUseCase>();
        builder.Services.AddOwnPasswordRateLimit();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestIdentityHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(MyProfileController).Assembly);
        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseRateLimiter();
        _app.MapControllers();
        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _client = new HttpClient { BaseAddress = new Uri(address) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task AnonymousRequests_AreUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/User/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PutAsJsonAsync("/api/User/me", new { phone = "81999991234" })).StatusCode);
        await _gateway.DidNotReceiveWithAnyArgs().FindById(default);
    }

    [Fact]
    public async Task Student_CanUpdateOnlyTheirOwnPhone()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        var response = await _client.PutAsJsonAsync("/api/User/me?userId=999", new { phone = "(81) 99999-1234" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("81999991234", _user.Phone);
        Assert.Equal("Pessoa de Teste", _user.Name);
        Assert.Equal("STUDENT", _user.Profile.Single().Role);
        Assert.Equal("ACTIVE", _user.Status);
        await _gateway.Received(1).FindById(21);
        await _gateway.DidNotReceive().FindById(999);
        await _gateway.Received(1).UpdatePhone(21, "81999991234");
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Get_IgnoresRequestedIdentityAndDoesNotExposeSecrets()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        var response = await _client.GetAsync("/api/User/me?userId=999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        using var document = JsonDocument.Parse(json);
        Assert.Equal(21, document.RootElement.GetProperty("id").GetInt64());
        Assert.False(document.RootElement.TryGetProperty("password", out _));
        Assert.Equal(_user.CPF, document.RootElement.GetProperty("cpf").GetString());
        Assert.DoesNotContain("nao-expor", json);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("profile")]
    [InlineData("roles")]
    [InlineData("status")]
    [InlineData("password")]
    [InlineData("campiCourseId")]
    public async Task ExtraFields_AreRejectedWithoutSaving(string field)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        var payload = new Dictionary<string, object> { ["phone"] = "81999991234", [field] = "nao-permitido" };
        var response = await _client.PutAsJsonAsync("/api/User/me", payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("81988888888", _user.Phone);
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task InvalidPhone_ReturnsBadRequestWithoutSaving()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        var response = await _client.PutAsJsonAsync("/api/User/me", new { phone = "123" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task InvalidClaim_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "invalido");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/User/me")).StatusCode);
        await _gateway.DidNotReceiveWithAnyArgs().FindById(default);
    }

    private class TestIdentityHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Identity", out var userId))
                return Task.FromResult(AuthenticateResult.NoResult());
            var role = Request.Headers.TryGetValue("X-Test-Role", out var chosenRole) ? chosenRole.ToString() : "STUDENT";
            var identity = new ClaimsIdentity(new[] { new Claim("userId", userId.ToString()), new Claim(ClaimTypes.Role, role) }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private static Dictionary<string, object> StudentPayload() => new()
    {
        ["name"] = "Nome corrigido", ["phone"] = "81999991234", ["cpf"] = "123.456.789-09",
        ["registration"] = "TESTE-2", ["userClass"] = "Turma de teste", ["shift"] = 3
    };

    [Fact]
    public async Task OwnPassword_RejectsAnonymousAndExtraIdentity()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PostAsJsonAsync("/api/User/me/password", new { currentPassword = "teste", newPassword = "nova-de-teste" })).StatusCode);
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/User/me/password", new { currentPassword = "teste", newPassword = "nova-de-teste", userId = 999 })).StatusCode);
        await _gateway.DidNotReceiveWithAnyArgs().UpdateOwnPassword(default, default!, default!);
    }

    [Fact]
    public async Task OwnPassword_ValidatesCurrentPasswordAndRateLimitsAttempts()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/User/me/password", new { currentPassword = "errada-de-teste", newPassword = "nova-de-teste" })).StatusCode);
        var rejected = await _client.PostAsJsonAsync("/api/User/me/password", new { currentPassword = "errada-de-teste", newPassword = "nova-de-teste" });
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.NotNull(rejected.Headers.RetryAfter);
        await _gateway.DidNotReceiveWithAnyArgs().UpdateOwnPassword(default, default!, default!);
    }

    [Fact]
    public async Task OwnPassword_ChangesOnlyAuthenticatedHashWithoutReturningPasswords()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        _bcrypt.VerifyHashPassword(_user, "atual-de-teste").Returns(true);
        _bcrypt.GenerateHashPassword("nova-de-teste").Returns("novo-hash-de-teste");
        _gateway.UpdateOwnPassword(21, "nao-expor", "novo-hash-de-teste").Returns(true);
        var response = await _client.PostAsJsonAsync("/api/User/me/password?userId=999", new { currentPassword = "atual-de-teste", newPassword = "nova-de-teste" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("atual-de-teste", body);
        Assert.DoesNotContain("novo-hash-de-teste", body);
        await _gateway.Received(1).UpdateOwnPassword(21, "nao-expor", "novo-hash-de-teste");
        await _gateway.DidNotReceive().FindById(999);
    }

    [Fact]
    public async Task StudentDetails_UpdateOnlyAllowedFieldsOfAuthenticatedUser()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        var response = await _client.PatchAsJsonAsync("/api/User/me?userId=999", StudentPayload());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Nome corrigido", _user.Name);
        Assert.Equal("TESTE-2", _user.Registration);
        Assert.Equal("Noturno", _user.Shift);
        Assert.Equal("teste@discente.ifpe.edu.br", _user.Email);
        Assert.Equal("ACTIVE", _user.Status);
        Assert.Equal("STUDENT", _user.Profile.Single().Role);
        Assert.Equal("nao-expor", _user.Password);
        await _gateway.DidNotReceive().FindById(999);
        await _gateway.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Theory]
    [InlineData("email")]
    [InlineData("id")]
    [InlineData("roles")]
    [InlineData("profile")]
    [InlineData("status")]
    [InlineData("password")]
    [InlineData("campusId")]
    [InlineData("courseId")]
    public async Task StudentDetails_RejectsExtraFields(string field)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        var data = StudentPayload();
        data[field] = "nao-permitido";
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PatchAsJsonAsync("/api/User/me", data)).StatusCode);
        await _gateway.DidNotReceiveWithAnyArgs().UpdateStudentProfile(default, default!, default!);
    }

    [Theory]
    [InlineData("name", "  ")]
    [InlineData("cpf", "000.000.000-00")]
    [InlineData("registration", "")]
    [InlineData("userClass", "")]
    [InlineData("phone", "abc81999991234")]
    public async Task StudentDetails_RejectsInvalidValues(string field, string value)
    {
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        var data = StudentPayload();
        data[field] = value;
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PatchAsJsonAsync("/api/User/me", data)).StatusCode);
        await _gateway.DidNotReceiveWithAnyArgs().UpdateStudentProfile(default, default!, default!);
    }

    [Fact]
    public async Task StudentDetails_RejectsAnonymousAndOtherRoles()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PatchAsJsonAsync("/api/User/me", StudentPayload())).StatusCode);
        _client.DefaultRequestHeaders.Add("X-Test-Identity", "21");
        _client.DefaultRequestHeaders.Add("X-Test-Role", "ADVISOR");
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PatchAsJsonAsync("/api/User/me", StudentPayload())).StatusCode);
        await _gateway.DidNotReceiveWithAnyArgs().UpdateStudentProfile(default, default!, default!);
    }
}
