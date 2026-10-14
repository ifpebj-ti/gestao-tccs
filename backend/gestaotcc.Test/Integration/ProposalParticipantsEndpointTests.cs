using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
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

public class ProposalParticipantsEndpointTests : IAsyncLifetime
{
    private readonly IProposalGateway proposals = Substitute.For<IProposalGateway>();
    private readonly ITccGateway tccs = Substitute.For<ITccGateway>();
    private readonly IUserGateway users = Substitute.For<IUserGateway>();
    private WebApplication app = null!;
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        var guest = new UserEntity { Id = 2, Name = "Convidado", Email = "guest@example.com", Status = "ACTIVE", CampiCourseId = 7, Profile = [new() { Role = "STUDENT" }] };
        var owner = new UserEntity { Id = 1, Name = "Solicitante", Email = "owner@example.com", Status = "ACTIVE", CampiCourseId = 7, Profile = [new() { Role = "STUDENT" }] };
        var tcc = new TccEntity { Id = 10, Title = "Projeto", Summary = "Resumo", Status = "AWAITING_PARTICIPANTS", UserTccs = [new() { UserId = 1, User = owner, Profile = owner.Profile.First() }] };
        var invitation = new ProposalParticipantEntity { Id = 20, UserId = 2, User = guest, TccId = 10, Tcc = tcc, Email = guest.Email };
        tcc.ParticipantInvitations.Add(invitation);
        users.FindById(2).Returns(guest); users.FindById(1).Returns(owner);
        proposals.Find(20).Returns(invitation); proposals.PendingFor(2).Returns(new List<ProposalParticipantEntity> { invitation });
        proposals.Atomic(Arg.Any<Func<Task<ResultPattern<string>>>>()).Returns(c => c.Arg<Func<Task<ResultPattern<string>>>>()());
        tccs.FindTccById(10).Returns(tcc); tccs.FindAllTccByFilter(Arg.Any<gestaotcc.Domain.Dtos.Tcc.TccFilterDTO>(), 0).Returns(new List<TccEntity>());
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.Configuration.Sources.Clear(); builder.Configuration.AddInMemoryCollection(); builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(proposals); builder.Services.AddSingleton(tccs); builder.Services.AddSingleton(users);
        builder.Services.AddScoped<ProposalParticipantsUseCase>();
        builder.Services.AddSingleton(Substitute.For<IDocumentTypeGateway>());
        builder.Services.AddSingleton(Substitute.For<IEmailGateway>());
        builder.Services.AddSingleton(Substitute.For<IAppLoggerGateway<ApproveTccProposalUseCase>>());
        builder.Services.AddSingleton(Substitute.For<IAppLoggerGateway<RejectTccProposalUseCase>>());
        builder.Services.AddScoped<ApproveTccProposalUseCase>();
        builder.Services.AddScoped<RejectTccProposalUseCase>();
        builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, IdentityHandler>("Test", _ => { });
        builder.Services.AddAuthorization(); builder.Services.AddControllers().AddApplicationPart(typeof(ProposalParticipantsController).Assembly);
        app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        client = new() { BaseAddress = new(app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
    }

    public async Task DisposeAsync() { client.Dispose(); await app.DisposeAsync(); }

    [Theory]
    [InlineData("approve", "IN_PROGRESS")]
    [InlineData("reject", "REJECTED")]
    public async Task AdvisorDecisionsUsePatchWithRouteId(string decision, string expected)
    {
        var tcc = (await tccs.FindTccById(10))!;
        tcc.Status = "PENDING_APPROVAL";
        tcc.ParticipantInvitations.Clear();
        tcc.UserTccs.Add(new() { UserId = 3, User = new() { Id = 3, Name = "Professor" }, Profile = new() { Role = "ADVISOR" } });
        var documentTypes = app.Services.GetRequiredService<IDocumentTypeGateway>();
        documentTypes.FindAll().Returns(new List<gestaotcc.Domain.Entities.DocumentType.DocumentTypeEntity>());
        client.DefaultRequestHeaders.Add("X-User", "3");
        client.DefaultRequestHeaders.Add("X-Role", "ADVISOR");
        var response = await client.PatchAsJsonAsync($"/api/Tcc/10/{decision}-proposal", new { reason = "Rever abordagem" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, tcc.Status);
        await tccs.Received().Update(tcc);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync($"/api/Tcc/10/{decision}-proposal", new { reason = "Rever abordagem" })).StatusCode);
    }

    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task AnotherAdvisorCannotDecideProposal(string decision)
    {
        client.DefaultRequestHeaders.Add("X-User", "99");
        client.DefaultRequestHeaders.Add("X-Role", "ADVISOR");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync($"/api/Tcc/10/{decision}-proposal", new { reason = "Rever abordagem" })).StatusCode);
        await tccs.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task AdvisorCannotCreateManagementProposal()
    {
        client.DefaultRequestHeaders.Add("X-User", "3");
        client.DefaultRequestHeaders.Add("X-Role", "ADVISOR");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/Tcc", new { })).StatusCode);
    }

    [Fact]
    public async Task AnonymousCannotReadOrRespond()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Tcc/participant-invitations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PatchAsJsonAsync("/api/Tcc/participant-invitations/20", new { accept = true })).StatusCode);
    }

    [Fact]
    public async Task WrongRoleAndMissingIdentityAreDenied()
    {
        client.DefaultRequestHeaders.Add("X-User", "2"); client.DefaultRequestHeaders.Add("X-Role", "ADVISOR");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/Tcc/participant-invitations")).StatusCode);
        client.DefaultRequestHeaders.Remove("X-Role"); client.DefaultRequestHeaders.Remove("X-User"); client.DefaultRequestHeaders.Add("X-User", "invalid");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Tcc/participant-invitations")).StatusCode);
    }

    [Fact]
    public async Task ListUsesAuthenticatedIdentityAndDoesNotExposeOtherAccountFields()
    {
        client.DefaultRequestHeaders.Add("X-User", "2");
        var response = await client.GetAsync("/api/Tcc/participant-invitations?userId=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.True(response.Headers.CacheControl?.NoStore);
        var invitation = Assert.Single((await response.Content.ReadFromJsonAsync<List<ProposalInvitationDTO>>())!);
        Assert.Equal("Solicitante", invitation.RequestedBy); Assert.Equal("Resumo", invitation.Summary);
        var json = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("password", json); Assert.DoesNotContain("cpf", json);
        await proposals.Received().PendingFor(2); await proposals.DidNotReceive().PendingFor(1);
    }

    [Fact]
    public async Task ForgedBodyIdentityCannotAcceptAnotherPersonsInvitation()
    {
        client.DefaultRequestHeaders.Add("X-User", "1");
        Assert.Equal(HttpStatusCode.NotFound, (await client.PatchAsJsonAsync("/api/Tcc/participant-invitations/20", new { accept = true, userId = 2 })).StatusCode);
        await tccs.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"accept\":null}")]
    [InlineData("{\"accept\":\"yes\"}")]
    public async Task RequiresExplicitBooleanConsent(string json)
    {
        client.DefaultRequestHeaders.Add("X-User", "2");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsync("/api/Tcc/participant-invitations/20", new StringContent(json, System.Text.Encoding.UTF8, "application/json"))).StatusCode);
        await tccs.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task AcceptSucceedsAndDuplicateResponseIsConflict()
    {
        client.DefaultRequestHeaders.Add("X-User", "2");
        Assert.Equal(HttpStatusCode.OK, (await client.PatchAsJsonAsync("/api/Tcc/participant-invitations/20", new { accept = true })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PatchAsJsonAsync("/api/Tcc/participant-invitations/20", new { accept = false })).StatusCode);
    }

    private sealed class IdentityHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-User", out var user)) return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim("userId", user.ToString()), new Claim(ClaimTypes.Role, Request.Headers["X-Role"].FirstOrDefault() ?? "STUDENT")], "Test");
            return Task.FromResult(AuthenticateResult.Success(new(new ClaimsPrincipal(identity), "Test")));
        }
    }
}
