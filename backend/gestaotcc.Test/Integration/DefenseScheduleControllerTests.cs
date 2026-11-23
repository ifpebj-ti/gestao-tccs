using System.Security.Claims;
using FluentValidation;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Errors;
using gestaotcc.WebApi.Controllers;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class DefenseScheduleControllerTests
{
    private const long AdvisorId = 10;
    private static readonly DateOnly Date = DateOnly.FromDateTime(DateTime.Now.AddDays(6));
    private static readonly TimeOnly Time = new(10, 30);
    private static readonly ScheduleTccDTO Request = new(Date, Time, "Sala 9", 1);
    private readonly ITccGateway _projects = Substitute.For<ITccGateway>();
    private readonly IUserGateway _users = Substitute.For<IUserGateway>();
    private readonly IProfileGateway _profiles = Substitute.For<IProfileGateway>();
    private readonly IEmailGateway _email = Substitute.For<IEmailGateway>();
    private readonly IStorageGateway _storage = Substitute.For<IStorageGateway>();
    private readonly TccEntity _project = new()
    {
        Id = 1,
        Step = "PRESENTATION_AND_EVALUATION",
        UserTccs = [new UserTccEntity { UserId = AdvisorId, Profile = new ProfileEntity { Role = "ADVISOR" } }]
    };

    public DefenseScheduleControllerTests()
    {
        _projects.FindTccScheduling(1).Returns(_project);
        _profiles.FindByRole("BANKING").Returns(new ProfileEntity { Id = 2, Role = "BANKING" });
        _email.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));
    }

    [Theory]
    [InlineData("missing-user", typeof(UnauthorizedResult))]
    [InlineData("unknown-project", typeof(NotFoundObjectResult))]
    [InlineData("other-advisor", typeof(ObjectResult))]
    [InlineData("save-error", typeof(ObjectResult))]
    public async Task CreateAndEdit_MapErrorsWithoutSendingNotices(string scenario, Type expected)
    {
        var claim = scenario switch { "missing-user" => null, "other-advisor" => "99", _ => "10" };
        if (scenario == "unknown-project") _projects.FindTccScheduling(1).Returns((TccEntity?)null);
        if (scenario == "save-error") _projects.Update(_project).Returns(Task.FromException(new IOException("Test database error")));
        var controller = Controller(claim);

        var created = await controller.CreateSchedule(Request, Create());
        if (scenario == "save-error") Assert.NotNull(_project.TccSchedule);
        var edited = await controller.EditSchedule(Request, Edit());

        Assert.IsType(expected, created.Result);
        Assert.IsType(expected, edited.Result);
        if (scenario is "other-advisor" or "save-error")
        {
            var status = scenario == "other-advisor" ? 403 : 500;
            Assert.Equal(status, Assert.IsAssignableFrom<ObjectResult>(created.Result).StatusCode);
            Assert.Equal(status, Assert.IsAssignableFrom<ObjectResult>(edited.Result).StatusCode);
        }
        await _email.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task CreateAndEdit_SavesTheDefenseAndUpdatesItsLocation()
    {
        var controller = Controller();
        var created = await controller.CreateSchedule(Request, Create());

        Assert.IsType<OkObjectResult>(created.Result);
        Assert.Equal(Date.ToDateTime(Time, DateTimeKind.Utc), _project.TccSchedule!.ScheduledDate);
        Assert.Equal("Sala 9", _project.TccSchedule.Location);
        var edited = await controller.EditSchedule(Request with { ScheduleLocation = "Sala 4" }, Edit());

        Assert.IsType<OkObjectResult>(edited.Result);
        Assert.Equal("Sala 4", _project.TccSchedule.Location);
        await _projects.Received(2).Update(_project);
    }

    [Fact]
    public async Task Create_RejectsTheWrongWorkflowStage_AndEditRequiresASchedule()
    {
        _project.Step = "PROPOSAL_REGISTRATION";
        var controller = Controller();

        Assert.IsType<BadRequestObjectResult>((await controller.CreateSchedule(Request, Create())).Result);
        Assert.IsType<ConflictObjectResult>((await controller.EditSchedule(Request, Edit())).Result);
        await _projects.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateAndEdit_RejectDuplicateExaminerEntries(bool internalExaminer)
    {
        BankingMemberDto[] members = internalExaminer
            ? [new("", "", "", 20), new("", "", "", 20)]
            : [new("Ana", "ana@example.org", "Externo"), new("Ana", "ANA@example.org", "Externo")];
        var request = Request with { BankingMembers = members };
        var controller = Controller();

        await Assert.ThrowsAsync<ValidationException>(() => controller.CreateSchedule(request, Create()));
        await Assert.ThrowsAsync<ValidationException>(() => controller.EditSchedule(request, Edit()));
        await _projects.DidNotReceive().FindTccScheduling(Arg.Any<long>());
    }

    [Theory]
    [InlineData("missing-user", typeof(UnauthorizedResult))]
    [InlineData("unknown-project", typeof(NotFoundObjectResult))]
    [InlineData("other-advisor", typeof(ObjectResult))]
    [InlineData("no-schedule", typeof(ConflictObjectResult))]
    [InlineData("email-error", typeof(ObjectResult))]
    [InlineData("available", typeof(OkObjectResult))]
    public async Task SendScheduleEmail_OnlyNotifiesForAnAccessibleScheduledDefense(string scenario, Type expected)
    {
        var claim = scenario switch { "missing-user" => null, "other-advisor" => "99", _ => "10" };
        if (scenario == "unknown-project") _projects.FindTccScheduling(1).Returns((TccEntity?)null);
        if (scenario is "email-error" or "available")
        {
            _project.TccSchedule = new TccScheduleEntity { ScheduledDate = Date.ToDateTime(Time, DateTimeKind.Utc), Location = "Sala 9" };
            _project.BankingMembers = [new TccBankingMemberEntity("Ana", "ana@example.org", "Examinador Externo", "test-token", 1)];
        }
        if (scenario == "email-error")
            _email.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.FailureResult("Mail unavailable", 500));
        var useCase = new SendScheduleEmailUseCase(_projects, _email, _storage, Substitute.For<IAppLoggerGateway<SendScheduleEmailUseCase>>());

        var result = await Controller(claim).SendScheduleEmail(1, useCase);

        Assert.IsType(expected, result.Result);
        if (scenario is "other-advisor" or "email-error")
            Assert.Equal(scenario == "other-advisor" ? 403 : 500, Assert.IsAssignableFrom<ObjectResult>(result.Result).StatusCode);
        if (scenario is "available" or "email-error")
            await _email.Received(1).Send(Arg.Is<SendEmailDTO>(message =>
                message.Recipient == "ana@example.org" && message.TypeTemplate == "BANKING-INVITE" && message.CalendarIcsContent != null));
        else
            await _email.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    private CreateScheduleTccUseCase Create() =>
        new(_projects, _users, _profiles, Substitute.For<IAppLoggerGateway<CreateScheduleTccUseCase>>(),
            _email, Substitute.For<IBackgroundJobClient>(), _storage);

    private EditScheduleTccUseCase Edit() =>
        new(_projects, Substitute.For<IAppLoggerGateway<EditScheduleTccUseCase>>(), _users, _profiles, _email, _storage);

    private static TccController Controller(string? claim = "10")
    {
        var identity = claim is null ? new ClaimsIdentity() : new ClaimsIdentity([new Claim("userId", claim)], "test");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new TccController { ControllerContext = new ControllerContext { HttpContext = context } };
    }
}
