using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.WebApi.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class TccFilesEndpointTests : IAsyncLifetime
{
    private readonly ITccGateway _tccs = Substitute.For<ITccGateway>();
    private readonly IUserGateway _users = Substitute.For<IUserGateway>();
    private readonly IStorageGateway _storage = Substitute.For<IStorageGateway>();
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _users.FindById(21).Returns(new UserEntity { Id = 21, Status = "ACTIVE", Profile = [new() { Role = "STUDENT" }] });
        _tccs.FindTccById(42).Returns(new TccEntity {
            Id = 42, UserTccs = [new() { UserId = 21, Profile = new() { Role = "STUDENT" }, User = new() { Id = 21 } }],
            Documents = [new() { Id = 10, FileName = "assinado", DocumentType = new() { Name = "Anexo" }, Signatures = [new() { SignatureDate = new(2026, 10, 13) }] }]
        });
        _storage.Download("assinado.pdf", true).Returns(new byte[] { 37, 80, 68, 70 });
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration.Sources.Clear(); builder.Configuration.AddInMemoryCollection(); builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(_tccs); builder.Services.AddSingleton(_users); builder.Services.AddSingleton(_storage);
        builder.Services.AddScoped<TccFilesUseCase>();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, IdentityHandler>("Test", _ => {});
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(TccFilesController).Assembly);
        _app = builder.Build(); _app.UseAuthentication(); _app.UseAuthorization(); _app.MapControllers(); await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        _client = new HttpClient { BaseAddress = new(address) };
    }

    public async Task DisposeAsync() { _client.Dispose(); await _app.DisposeAsync(); }

    [Theory]
    [InlineData("/api/Tcc/42/files")]
    [InlineData("/api/Tcc/42/files/download")]
    [InlineData("/api/Tcc/42/files/10/download")]
    [InlineData("/api/Signature/all/documents/download/42")]
    public async Task AnonymousCannotReadFiles(string path) => Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync(path)).StatusCode);

    [Theory]
    [InlineData("/api/Tcc/43/files")]
    [InlineData("/api/Tcc/43/files/10/download")]
    [InlineData("/api/Tcc/42/files/99/download")]
    [InlineData("/api/Signature/all/documents/download/43")]
    public async Task ChangingIdsCannotReadOtherFiles(string path)
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "21");
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(path)).StatusCode);
        await _storage.DidNotReceiveWithAnyArgs().Download(default!, default);
    }

    [Fact]
    public async Task OwnFilesAreListedAndDownloadedAsAttachments()
    {
        _client.DefaultRequestHeaders.Add("X-Test-User", "21");
        var list = await _client.GetAsync("/api/Tcc/42/files");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.True(list.Headers.CacheControl?.NoStore);
        Assert.Single((await list.Content.ReadFromJsonAsync<List<TccFileDTO>>())!);
        var file = await _client.GetAsync("/api/Tcc/42/files/10/download");
        Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        Assert.Equal("attachment", file.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("nosniff", Assert.Single(file.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal(4, (await file.Content.ReadAsByteArrayAsync()).Length);
    }

    private class IdentityHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-User", out var id)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim("userId", id.ToString()), new Claim(ClaimTypes.Role, "STUDENT")], "Test");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "Test")));
        }
    }
}
