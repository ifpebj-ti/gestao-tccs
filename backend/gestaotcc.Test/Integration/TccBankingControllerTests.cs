using System.Security.Claims;
using System.Text;
using System.Text.Json;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Errors;
using gestaotcc.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class TccBankingControllerTests
{
    private const long TccId = 42;
    private const long UserId = 17;
    private const string Invite = "test-invitation";
    private const string Detail = """{"oral":{"postura":"4"}}""";
    private readonly ITccGateway _tccs = Substitute.For<ITccGateway>();

    [Theory]
    [InlineData("missing-token", typeof(BadRequestObjectResult), 400)]
    [InlineData("unknown-token", typeof(UnauthorizedObjectResult), 401)]
    [InlineData("internal-examiner", typeof(ForbidResult), null)]
    [InlineData("storage-error", typeof(ObjectResult), 500)]
    [InlineData("available", typeof(OkObjectResult), 200)]
    public async Task EvaluateExternal_MapsOutcomeAndSavesOnlyAnEligibleEvaluation(
        string scenario, Type resultType, int? status)
    {
        var member = Member("Examinador Externo");
        var token = scenario == "missing-token" ? "" : Invite;
        if (scenario == "internal-examiner") member.UserId = UserId;
        if (scenario != "unknown-token") _tccs.FindBankingMemberByToken(Invite).Returns(member);
        if (scenario == "storage-error")
            _tccs.UpdateBankingMember(member).Returns(Task.FromException(new IOException("Test database failure")));

        var result = await Controller().Evaluate(new(token, 8m, null, Detail), EvaluateUseCase());

        var response = result.Result;
        Assert.IsType(resultType, response);
        if (response is ObjectResult body) Assert.Equal(status, body.StatusCode);
        if (response is ObjectResult { Value: FormatDetails error })
            Assert.Equal("https://gradus.example.org/api/Tcc/evaluate", error.Type);
        if (scenario == "available")
        {
            AssertSavedResult(Assert.IsType<OkObjectResult>(response));
            Assert.Equal(8m, member.Grade);
            Assert.Equal("Aprovado", member.EvaluationComments);
            Assert.Equal(Detail, member.EvaluationDetails);
            Assert.Equal(Invite, member.AccessToken);
            await _tccs.Received(1).UpdateBankingMember(member);
        }
        else if (scenario != "storage-error")
        {
            await _tccs.DidNotReceive().UpdateBankingMember(Arg.Any<TccBankingMemberEntity>());
        }
    }

    [Theory]
    [InlineData("no-user", typeof(UnauthorizedResult), null)]
    [InlineData("invalid-grade", typeof(BadRequestObjectResult), 400)]
    [InlineData("unlinked-examiner", typeof(ForbidResult), null)]
    [InlineData("before-presentation", typeof(ConflictObjectResult), 409)]
    [InlineData("already-evaluated", typeof(ConflictObjectResult), 409)]
    [InlineData("storage-error", typeof(ObjectResult), 500)]
    [InlineData("available", typeof(OkObjectResult), 200)]
    public async Task EvaluateInternal_MapsOutcomeAndPreservesEvaluationDetails(
        string scenario, Type resultType, int? status)
    {
        var member = Member("Examinador Interno");
        member.UserId = UserId;
        if (scenario == "already-evaluated") member.Grade = 6m;
        if (scenario != "before-presentation") member.TokenExpiryDate = new DateTime(2030, 1, 1);
        if (scenario != "unlinked-examiner") _tccs.FindBankingMemberByTccAndUser(TccId, UserId).Returns(member);
        if (scenario == "storage-error")
            _tccs.UpdateBankingMember(member).Returns(Task.FromException(new IOException("Test database failure")));

        var result = await Controller(scenario == "no-user" ? null : UserId).EvaluateAsRegisteredUser(
            new(TccId, scenario == "invalid-grade" ? 11m : 8m, "Parecer", Detail), EvaluateUseCase());

        var response = result.Result;
        Assert.IsType(resultType, response);
        if (response is ObjectResult body) Assert.Equal(status, body.StatusCode);
        if (scenario == "available")
        {
            AssertSavedResult(Assert.IsType<OkObjectResult>(response));
            Assert.Equal(8m, member.Grade);
            Assert.Equal("Parecer", member.EvaluationComments);
            Assert.Equal(Detail, member.EvaluationDetails);
            await _tccs.Received(1).UpdateBankingMember(member);
        }
        else if (scenario != "storage-error")
        {
            await _tccs.DidNotReceive().UpdateBankingMember(Arg.Any<TccBankingMemberEntity>());
        }
    }

    [Theory]
    [InlineData("no-user", typeof(UnauthorizedResult), null)]
    [InlineData("missing-tcc", typeof(NotFoundObjectResult), 404)]
    [InlineData("different-advisor", typeof(ObjectResult), 403)]
    [InlineData("missing-account", typeof(NotFoundObjectResult), 404)]
    [InlineData("available", typeof(OkObjectResult), 200)]
    public async Task LinkBanking_PersistsExaminerLinksOnlyForTheAssignedAdvisor(string scenario, Type resultType, int? status)
    {
        var users = Substitute.For<IUserGateway>();
        var profiles = Substitute.For<IProfileGateway>();
        var profile = new ProfileEntity { Id = 3, Role = "BANKING" };
        var tcc = new TccEntity
        {
            Id = TccId,
            UserTccs = [new UserTccEntity { UserId = UserId, Profile = new ProfileEntity { Role = "ADVISOR" } }]
        };
        if (scenario != "missing-tcc") _tccs.FindTccById(TccId).Returns(tcc);
        users.FindById(21).Returns(new UserEntity { Id = 21, Email = "internal@example.org" });
        if (scenario != "missing-account")
            users.FindById(22).Returns(new UserEntity { Id = 22, Email = "external@example.org" });
        profiles.FindByRole("BANKING").Returns(profile);
        var useCase = new LinkBankingUserUseCase(_tccs, users, profiles, Substitute.For<IEmailGateway>(),
            Substitute.For<IStorageGateway>(), Substitute.For<IAppLoggerGateway<LinkBankingUserUseCase>>());

        var requestedUser = scenario switch { "no-user" => (long?)null, "different-advisor" => 99L, _ => UserId };
        var result = await Controller(requestedUser).LinkBankingUser(new(21, 22, TccId), useCase);

        var response = result.Result;
        Assert.IsType(resultType, response);
        if (response is ObjectResult body) Assert.Equal(status, body.StatusCode);
        if (scenario == "available")
        {
            var linked = tcc.UserTccs.Where(link => link.Profile.Role == "BANKING").ToList();
            Assert.Equal([21L, 22L], linked.Select(link => link.User.Id));
            Assert.All(linked, link => Assert.Same(tcc, link.Tcc));
            await _tccs.Received(1).Update(tcc);
        }
        else
        {
            await _tccs.DidNotReceive().Update(Arg.Any<TccEntity>());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Calendar_IsUnavailableUntilAScheduleExists(bool projectExists)
    {
        if (projectExists) _tccs.FindTccScheduling(TccId).Returns(new TccEntity { Id = TccId });

        var response = await Controller().DownloadScheduleCalendar(TccId, _tccs);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Theory]
    [InlineData(null, "TCC")]
    [InlineData("Projeto de redes", "Projeto de redes")]
    public async Task Calendar_ReturnsAnIcsDownloadForTheScheduledDefense(string? title, string expectedTitle)
    {
        _tccs.FindTccScheduling(TccId).Returns(new TccEntity
        {
            Id = TccId,
            Title = title,
            TccSchedule = new TccScheduleEntity
            {
                Location = "Sala 2", ScheduledDate = new DateTime(2026, 11, 18, 14, 0, 0, DateTimeKind.Utc)
            }
        });

        var response = await Controller().DownloadScheduleCalendar(TccId, _tccs);

        var file = Assert.IsType<FileContentResult>(response);
        Assert.Equal("text/calendar; charset=utf-8", file.ContentType);
        Assert.Equal("defesa-tcc-42.ics", file.FileDownloadName);
        var ics = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains($"SUMMARY:Defesa de TCC: {expectedTitle}", ics);
        Assert.Contains("DTSTART:20261118T140000Z", ics);
        Assert.Contains("LOCATION:Sala 2", ics);
    }

    private EvaluateTccUseCase EvaluateUseCase() => new(_tccs, Substitute.For<IAppLoggerGateway<EvaluateTccUseCase>>());

    private static TccBankingMemberEntity Member(string role) =>
        new("Examinador", "examiner@example.org", role, Invite, TccId);

    private static void AssertSavedResult(OkObjectResult response) =>
        Assert.Equal(TccId, JsonSerializer.SerializeToElement(response.Value).GetProperty("tccId").GetInt64());

    private static TccController Controller(long? userId = UserId)
    {
        var claims = userId is null ? Array.Empty<Claim>() : [new Claim("userId", userId.Value.ToString())];
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("gradus.example.org");
        context.Request.Path = "/api/Tcc/evaluate";
        return new TccController { ControllerContext = new ControllerContext { HttpContext = context } };
    }
}
