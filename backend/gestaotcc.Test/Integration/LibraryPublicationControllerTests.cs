using System.Security.Claims;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class LibraryPublicationControllerTests
{
    private static readonly DateTime ReceivedOn = new(2026, 11, 18, 14, 0, 0, DateTimeKind.Utc);
    private readonly ITccGateway _projects = Substitute.For<ITccGateway>();
    private readonly IUserGateway _users = Substitute.For<IUserGateway>();
    private readonly IEmailGateway _email = Substitute.For<IEmailGateway>();
    private readonly TccEntity _project = new()
    {
        Id = 1, Step = "6", Status = "IN_PROGRESS", HardCoverReceivedAt = ReceivedOn
    };

    public LibraryPublicationControllerTests()
    {
        _projects.FindTccById(1).Returns(_project);
        _users.FindById(2).Returns(User(2, "COORDINATOR"));
        _users.FindById(3).Returns(User(3, "LIBRARY"));
    }

    [Theory]
    [InlineData("no-claim", typeof(UnauthorizedResult))]
    [InlineData("invalid-claim", typeof(UnauthorizedResult))]
    [InlineData("invalid-id", typeof(BadRequestObjectResult))]
    [InlineData("missing-project", typeof(NotFoundObjectResult))]
    [InlineData("wrong-role", typeof(ForbidResult))]
    [InlineData("missing-cover", typeof(BadRequestObjectResult))]
    [InlineData("already-forwarded", typeof(ConflictObjectResult))]
    public async Task Forward_MapsFailuresAndDoesNotSave(string scenario, Type expected)
    {
        if (scenario == "missing-cover") _project.HardCoverReceivedAt = null;
        if (scenario == "already-forwarded") _project.ForwardedToLibraryAt = ReceivedOn;
        var claim = scenario switch { "no-claim" => null, "invalid-claim" => "none", "wrong-role" => "3", _ => "2" };
        var id = scenario switch { "invalid-id" => 0, "missing-project" => 10, _ => 1 };

        var result = await Controller(claim).ForwardToLibrary(id, Forward());

        Assert.IsType(expected, result.Result);
        await _projects.DidNotReceive().Update(Arg.Any<TccEntity>());
        await _email.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task Forward_SavesAndNotifiesOnlyActiveLibraryAddresses()
    {
        _project.Step = "FINALIZATION_AND_PUBLICATION";
        var active = User(10, "LIBRARY");
        var inactive = User(11, "LIBRARY");
        inactive.Status = "INACTIVE";
        var noEmail = User(12, "LIBRARY");
        noEmail.Email = "";
        _users.FindAllByFilter(new UserFilterDTO(null, null, null, "LIBRARY"), 0)
            .Returns(new List<UserEntity> { active, inactive, noEmail });

        var result = await Controller("2").ForwardToLibrary(1, Forward());

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(_project.ForwardedToLibraryAt);
        await _projects.Received(1).Update(_project);
        await _email.Received(1).Send(Arg.Is<SendEmailDTO>(message =>
            message.Recipient == active.Email && message.TypeTemplate == "FORWARD-LIBRARY"));
        await _email.Received(1).Send(Arg.Any<SendEmailDTO>());
    }

    [Theory]
    [InlineData("no-claim", typeof(UnauthorizedResult))]
    [InlineData("invalid-claim", typeof(UnauthorizedResult))]
    [InlineData("invalid-id", typeof(BadRequestObjectResult))]
    [InlineData("missing-project", typeof(NotFoundObjectResult))]
    [InlineData("unknown-user", typeof(NotFoundObjectResult))]
    [InlineData("inactive-user", typeof(NotFoundObjectResult))]
    [InlineData("wrong-role", typeof(ForbidResult))]
    [InlineData("wrong-stage", typeof(BadRequestObjectResult))]
    [InlineData("not-forwarded", typeof(BadRequestObjectResult))]
    [InlineData("already-cataloged", typeof(ConflictObjectResult))]
    public async Task Confirm_RejectsInvalidCatalogingRequestsBeforeSaving(string scenario, Type expected)
    {
        _project.ForwardedToLibraryAt = scenario == "not-forwarded" ? null : ReceivedOn;
        if (scenario == "already-cataloged") _project.LibraryReceivedAt = ReceivedOn;
        if (scenario == "wrong-stage") _project.Step = "PROPOSAL_REGISTRATION";
        if (scenario == "inactive-user")
        {
            var inactive = User(3, "LIBRARY");
            inactive.Status = "INACTIVE";
            _users.FindById(3).Returns(inactive);
        }
        var claim = scenario switch { "no-claim" => null, "invalid-claim" => "none", "wrong-role" => "2", "unknown-user" => "9", _ => "3" };
        var id = scenario switch { "invalid-id" => 0, "missing-project" => 10, _ => 1 };

        var result = await Controller(claim).ConfirmLibrary(id, new("CAT-1"), Confirm());

        Assert.IsType(expected, result.Result);
        Assert.Equal("IN_PROGRESS", _project.Status);
        await _projects.DidNotReceive().Update(Arg.Any<TccEntity>());
        await _email.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Confirm_FinalizesLegacyProjectsWithOrWithoutCatalogCode(bool withCode, bool withParticipants)
    {
        _project.Step = withCode ? "legacy" : "6";
        _project.FinalSubmissionDeadline = withCode ? ReceivedOn : null;
        _project.ForwardedToLibraryAt = ReceivedOn;
        if (withParticipants)
        {
            var student = User(5, "STUDENT");
            student.Name = null!;
            var advisor = User(6, "ADVISOR");
            advisor.Email = "";
            _project.UserTccs =
            [
                Link(student), Link(advisor),
                Link(new UserEntity { Id = 7, Name = null!, Email = null!, Profile = [new() { Role = "STUDENT" }] })
            ];
            var active = User(8, "COORDINATOR");
            active.Name = null!;
            var inactive = User(9, "COORDINATOR");
            inactive.Status = "INACTIVE";
            var noEmail = User(10, "COORDINATOR");
            noEmail.Email = "";
            _users.FindAllByFilter(new UserFilterDTO(null, null, null, "COORDINATOR"), 7)
                .Returns(new List<UserEntity> { active, inactive, noEmail });
        }

        var result = await Controller("3").ConfirmLibrary(1, withCode ? new(" CAT-1 ") : null, Confirm());

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("COMPLETED", _project.Status);
        Assert.Equal("FINALIZATION_AND_PUBLICATION", _project.Step);
        Assert.Equal(withCode ? "CAT-1" : null, _project.LibraryCatalogCode);
        Assert.Equal(3, _project.LibraryReceivedByUserId);
        await _projects.Received(1).Update(_project);
        await _email.Received(withParticipants ? 2 : 0).Send(Arg.Any<SendEmailDTO>());
        if (withParticipants)
        {
            await _email.Received(1).Send(Arg.Is<SendEmailDTO>(message => message.Recipient == "user5@example.org" &&
                message.TypeTemplate == "TCC-COMPLETED" && (string)message.Variables["catalogCode"] == "CAT-1"));
        }
    }

    [Fact]
    public async Task PendingQueues_RequireIdentityAndReturnEmptyListsForAuthorizedUsers()
    {
        _projects.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), 7).Returns(new List<TccEntity>());
        var library = new FindLibraryPendingUseCase(_projects, _users);
        var coordination = new FindCoordinationPendingUseCase(_projects, _users);

        Assert.IsType<UnauthorizedResult>((await Controller(null).GetLibraryPending(library)).Result);
        Assert.IsType<UnauthorizedResult>((await Controller("none").GetCoordinationPending(coordination)).Result);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await Controller("2").GetLibraryPending(library)).Result).StatusCode);
        Assert.Equal(403, Assert.IsType<ObjectResult>((await Controller("3").GetCoordinationPending(coordination)).Result).StatusCode);

        var libraryResponse = Assert.IsType<OkObjectResult>((await Controller("3").GetLibraryPending(library)).Result);
        var coordinationResponse = Assert.IsType<OkObjectResult>((await Controller("2").GetCoordinationPending(coordination)).Result);
        Assert.Empty(Assert.IsType<LibraryPendingDTO>(libraryResponse.Value).PendingCataloging);
        Assert.Empty(Assert.IsType<CoordinationPendingDTO>(coordinationResponse.Value).AwaitingHardCover);
    }

    private ForwardToLibraryUseCase Forward() =>
        new(_projects, _users, _email, Substitute.For<IAppLoggerGateway<ForwardToLibraryUseCase>>());

    private ConfirmLibraryReceiptUseCase Confirm() =>
        new(_projects, _users, _email, Substitute.For<IAppLoggerGateway<ConfirmLibraryReceiptUseCase>>());

    private static UserEntity User(long id, string role) =>
        new() { Id = id, Status = "ACTIVE", Name = $"Pessoa {id}", Email = $"user{id}@example.org",
            CampiCourseId = 7, Profile = [new ProfileEntity { Role = role }] };

    private static UserTccEntity Link(UserEntity user) =>
        new() { User = user, UserId = user.Id, Profile = user.Profile.Single() };

    private static TccController Controller(string? claim)
    {
        var identity = claim is null ? new ClaimsIdentity() : new ClaimsIdentity([new Claim("userId", claim)], "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("gradus.example.org");
        return new TccController { ControllerContext = new ControllerContext { HttpContext = context } };
    }
}
