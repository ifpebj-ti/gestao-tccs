using System.Security.Claims;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Errors;
using gestaotcc.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class AdvisorReviewControllerTests
{
    private const long ProjectId = 11;
    private const long CoordinatorId = 21;
    private const long Course = 31;
    private readonly ITccGateway _projects = Substitute.For<ITccGateway>();
    private readonly IUserGateway _users = Substitute.For<IUserGateway>();
    private readonly IEmailGateway _email = Substitute.For<IEmailGateway>();
    private readonly UserEntity _coordinator = Account(CoordinatorId, "COORDINATOR");
    private readonly UserEntity _previous = Account(41, "ADVISOR");
    private readonly UserEntity _replacement = Account(42, "ADVISOR");
    private readonly UserEntity _student = Account(51, "STUDENT");
    private readonly TccEntity _project;
    private readonly TccAdvisorChangeEntity _change;

    public AdvisorReviewControllerTests()
    {
        _project = new TccEntity { Id = ProjectId, Title = "Projeto", UserTccs = [Link(_student), Link(_previous)] };
        _change = new TccAdvisorChangeEntity
        {
            TccId = ProjectId, Status = "PENDING_COORDINATION", CurrentAdvisorId = _previous.Id,
            CurrentAdvisor = _previous, SuggestedAdvisorId = _replacement.Id, SuggestedAdvisor = _replacement,
            RequesterUser = _student, RequesterRole = "STUDENT"
        };
        _projects.FindTccById(ProjectId).Returns(_project);
        _projects.FindPendingAdvisorChange(ProjectId).Returns(_change);
        _users.FindById(CoordinatorId).Returns(_coordinator);
        _users.FindById(_replacement.Id).Returns(_replacement);
        _email.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));
    }

    [Theory]
    [InlineData("missing-claim", typeof(UnauthorizedResult))]
    [InlineData("invalid-claim", typeof(UnauthorizedResult))]
    [InlineData("invalid-reviewer", typeof(BadRequestObjectResult))]
    [InlineData("invalid-project", typeof(BadRequestObjectResult))]
    [InlineData("unknown-reviewer", typeof(UnauthorizedResult))]
    [InlineData("inactive-reviewer", typeof(UnauthorizedResult))]
    [InlineData("wrong-role", typeof(ForbidResult))]
    [InlineData("unknown-project", typeof(NotFoundObjectResult))]
    [InlineData("no-course", typeof(ForbidResult))]
    [InlineData("different-course", typeof(ForbidResult))]
    [InlineData("no-request", typeof(ConflictObjectResult))]
    [InlineData("awaiting-signatures", typeof(ConflictObjectResult))]
    public async Task Review_RejectsIneligibleRequestsBeforeWriting(string scenario, Type expected)
    {
        var projectId = scenario == "invalid-project" ? 0 : ProjectId;
        var claim = scenario switch
        {
            "missing-claim" => null,
            "invalid-claim" => "invalid",
            "invalid-reviewer" => "0",
            "unknown-reviewer" => "22",
            _ => CoordinatorId.ToString()
        };
        switch (scenario)
        {
            case "inactive-reviewer": _coordinator.Status = "INACTIVE"; break;
            case "wrong-role": _coordinator.Profile = [new() { Role = "SUPERVISOR" }]; break;
            case "unknown-project": _projects.FindTccById(ProjectId).Returns((TccEntity?)null); break;
            case "no-course": _coordinator.CampiCourseId = null; break;
            case "different-course": _coordinator.CampiCourseId = Course + 1; break;
            case "no-request": _projects.FindPendingAdvisorChange(ProjectId).Returns((TccAdvisorChangeEntity?)null); break;
            case "awaiting-signatures": _change.Status = "PENDING_SIGNATURES"; break;
        }

        var controller = Controller(claim);
        var approve = await controller.ApproveAdvisorChange(projectId, Approve());
        var reject = await controller.RejectAdvisorChange(projectId, new("Parecer"), Reject());

        Assert.IsType(expected, approve);
        Assert.IsType(expected, reject);
        await AssertNoWrites();
    }

    [Theory]
    [InlineData("no-replacement")]
    [InlineData("unknown-replacement")]
    [InlineData("inactive-replacement")]
    [InlineData("no-advisor-role")]
    [InlineData("no-replacement-course")]
    [InlineData("other-replacement-course")]
    [InlineData("previous-link-missing")]
    [InlineData("previous-link-wrong-role")]
    public async Task Approve_ReturnsConflictForAnInvalidReplacement(string scenario)
    {
        switch (scenario)
        {
            case "no-replacement": _change.SuggestedAdvisorId = null; break;
            case "unknown-replacement": _change.SuggestedAdvisorId = 99; break;
            case "inactive-replacement": _replacement.Status = "INACTIVE"; break;
            case "no-advisor-role": _replacement.Profile = [new() { Role = "STUDENT" }]; break;
            case "no-replacement-course": _replacement.CampiCourseId = null; break;
            case "other-replacement-course": _replacement.CampiCourseId = Course + 1; break;
            case "previous-link-missing": _project.UserTccs.Remove(_project.UserTccs.Last()); break;
            case "previous-link-wrong-role": _project.UserTccs.Last().Profile = new() { Role = "BANKING" }; break;
        }

        var response = await Controller().ApproveAdvisorChange(ProjectId, Approve());

        Assert.IsType<ConflictObjectResult>(response);
        Assert.Equal("PENDING_COORDINATION", _change.Status);
        await AssertNoWrites();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("too-long")]
    public async Task Reject_RequiresAReasonWithinTheLengthLimit(string? notes)
    {
        var dto = notes is null ? null : new RejectAdvisorChangeDTO(notes == "too-long" ? new string('a', 2001) : notes);

        var response = await Controller().RejectAdvisorChange(ProjectId, dto!, Reject());

        Assert.IsType<BadRequestObjectResult>(response);
        await AssertNoWrites();
    }

    [Theory]
    [InlineData("COORDINATOR", false)]
    [InlineData("SUPERVISOR", true)]
    public async Task Approve_KeepsOneReplacementLinkAndPersistsEvenWhenAnEmailFails(string previousRole, bool throws)
    {
        var existing = Link(_replacement);
        _project.UserTccs.Last().Profile = new() { Role = previousRole };
        _project.UserTccs.Add(existing);
        _project.UserTccs.Add(Link(new UserEntity { Id = 52, Email = "", Profile = [new() { Role = "STUDENT" }] }));
        _project.Title = null;
        _change.CurrentAdvisor = null;
        _change.RequesterRole = "ADVISOR";
        _email.Send(Arg.Is<SendEmailDTO>(message => message.Recipient == _student.Email))
            .Returns(throws ? Task.FromException<ResultPattern<bool>>(new IOException("Mail unavailable"))
                : Task.FromResult(ResultPattern<bool>.FailureResult("Mail unavailable", 500)));

        var response = await Controller().ApproveAdvisorChange(ProjectId, Approve());

        Assert.IsType<OkObjectResult>(response);
        Assert.Same(existing, Assert.Single(_project.UserTccs.Where(link => link.UserId == _replacement.Id)));
        Assert.DoesNotContain(_project.UserTccs, link => link.UserId == _previous.Id);
        Assert.Equal("APPROVED", _change.Status);
        Assert.Equal(CoordinatorId, _change.ReviewedByUserId);
        await _projects.Received(1).Update(_project);
        await _email.Received(1).Send(Arg.Is<SendEmailDTO>(message => message.Recipient == _student.Email &&
            (string)message.Variables["oldAdvisorName"] == "Orientador anterior" &&
            (string)message.Variables["tccTitle"] == ""));
    }

    [Fact]
    public async Task Reject_PreservesLinksAndContinuesNotifyingAfterEmailFailures()
    {
        _project.Title = null;
        _student.Name = null!;
        _previous.Name = null!;
        _change.RequesterRole = "ADVISOR";
        _change.CurrentAdvisor = null;
        _change.RequesterUser = null;
        _project.UserTccs.Add(Link(new UserEntity { Id = 52, Name = null!, Email = null!, Profile = [new() { Role = "STUDENT" }] }));
        _email.Send(Arg.Is<SendEmailDTO>(message => message.Recipient == _student.Email))
            .Returns(Task.FromException<ResultPattern<bool>>(new IOException("Mail unavailable")));
        _email.Send(Arg.Is<SendEmailDTO>(message => message.Recipient == _previous.Email))
            .Returns(ResultPattern<bool>.FailureResult("Mail rejected", 500));

        var response = await Controller().RejectAdvisorChange(ProjectId, new("  Parecer registrado  "), Reject());

        Assert.IsType<OkObjectResult>(response);
        Assert.Equal("REJECTED", _change.Status);
        Assert.Equal("Parecer registrado", _change.ReviewNotes);
        Assert.Contains(_project.UserTccs, link => link.UserId == _previous.Id && link.Profile.Role == "ADVISOR");
        await _projects.Received(1).UpdateAdvisorChange(_change);
        await _email.Received(3).Send(Arg.Is<SendEmailDTO>(message => message.TypeTemplate == "ADVISOR-CHANGE-REJECTED" &&
            (string)message.Variables["reviewNotes"] == "Parecer registrado"));
    }

    private static UserEntity Account(long id, string role) => new()
    {
        Id = id, Name = $"Pessoa {id}", Email = $"user{id}@example.org",
        Status = "ACTIVE", CampiCourseId = Course, Profile = [new() { Role = role }]
    };

    private static UserTccEntity Link(UserEntity user) =>
        new() { User = user, UserId = user.Id, Profile = user.Profile.Single() };

    private static TccController Controller(string? claim = "21")
    {
        var identity = claim is null ? new ClaimsIdentity() : new ClaimsIdentity([new Claim("userId", claim)], "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new TccController { ControllerContext = new ControllerContext { HttpContext = context } };
    }

    private ApproveAdvisorChangeUseCase Approve() =>
        new(_projects, _users, _email, Substitute.For<IAppLoggerGateway<ApproveAdvisorChangeUseCase>>());

    private RejectAdvisorChangeUseCase Reject() =>
        new(_projects, _users, _email, Substitute.For<IAppLoggerGateway<RejectAdvisorChangeUseCase>>());

    private async Task AssertNoWrites()
    {
        await _projects.DidNotReceive().Update(Arg.Any<TccEntity>());
        await _projects.DidNotReceive().UpdateAdvisorChange(Arg.Any<TccAdvisorChangeEntity>());
        await _email.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }
}
