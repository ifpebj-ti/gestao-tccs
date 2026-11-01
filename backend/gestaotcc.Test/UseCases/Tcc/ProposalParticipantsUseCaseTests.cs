using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using gestaotcc.WebApi.Validators.Tcc;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class ProposalParticipantsUseCaseTests
{
    private readonly IProposalGateway proposals = Substitute.For<IProposalGateway>();
    private readonly ITccGateway tccs = Substitute.For<ITccGateway>();
    private readonly IUserGateway users = Substitute.For<IUserGateway>();
    private readonly UserEntity owner = Student(1);
    private readonly UserEntity guest = Student(2);
    private readonly TccEntity tcc;
    private readonly ProposalParticipantEntity invitation;
    private readonly ProposalParticipantsUseCase useCase;

    private static UserEntity Student(long id) => new() { Id = id, Name = $"Aluno {id}", Email = $"aluno{id}@example.com", Status = "ACTIVE", CampiCourseId = 7, Profile = [new() { Id = 1, Role = "STUDENT" }] };

    public ProposalParticipantsUseCaseTests()
    {
        tcc = new() { Id = 10, Title = "Pesquisa", Status = "AWAITING_PARTICIPANTS", UserTccs = [new() { UserId = 1, User = owner, Profile = owner.Profile.First() }] };
        invitation = new() { Id = 20, Tcc = tcc, TccId = 10, User = guest, UserId = 2, Email = guest.Email };
        tcc.ParticipantInvitations.Add(invitation);
        users.FindById(1).Returns(owner); users.FindById(2).Returns(guest);
        tccs.FindTccById(10).Returns(tcc);
        tccs.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), 0).Returns(new List<TccEntity>());
        proposals.Find(20).Returns(invitation);
        proposals.Atomic(Arg.Any<Func<Task<ResultPattern<string>>>>()).Returns(call => call.Arg<Func<Task<ResultPattern<string>>>>()());
        useCase = new(proposals, tccs, users);
    }

    private static ISemesterGateway ActiveSemester()
    {
        var semesters = Substitute.For<ISemesterGateway>();
        semesters.FindActive().Returns(new List<gestaotcc.Domain.Entities.Semester.SemesterEntity> { new() { Id = 1, Name = "2026.2", IsActive = true } });
        return semesters;
    }

    [Fact]
    public async Task AcceptBindsExactlyOnceAndRepeatIsConflict()
    {
        Assert.True((await useCase.Respond(20, 2, true)).IsSuccess);
        Assert.Equal("ACCEPTED", invitation.Status); Assert.NotNull(invitation.RespondedAt);
        Assert.Single(tcc.UserTccs.Where(x => x.UserId == 2));
        Assert.Equal(409, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
        await tccs.Received(1).Update(tcc);
    }

    [Fact]
    public async Task DeclineCancelsProposalAndOtherInvitationsWithoutBinding()
    {
        var other = new ProposalParticipantEntity { UserId = 3, Tcc = tcc, TccId = tcc.Id };
        tcc.ParticipantInvitations.Add(other);
        Assert.True((await useCase.Respond(20, 2, false)).IsSuccess);
        Assert.Equal("DECLINED", invitation.Status); Assert.Equal("CANCELED", other.Status);
        Assert.Equal("CANCELED", tcc.Status); Assert.Single(tcc.UserTccs);
        Assert.Equal(409, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
    }

    [Theory]
    [InlineData(99, 2)]
    [InlineData(20, 1)]
    [InlineData(20, 99)]
    public async Task OnlyRecipientCanRespond(long id, long userId)
    {
        Assert.Equal(404, (await useCase.Respond(id, userId, true)).ErrorDetails?.Status);
        await tccs.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Theory]
    [InlineData("INACTIVE", "STUDENT", 7L, 403)]
    [InlineData("ACTIVE", "ADVISOR", 7L, 403)]
    [InlineData("ACTIVE", "STUDENT", 8L, 409)]
    [InlineData("ACTIVE", "STUDENT", null, 409)]
    public async Task RevalidatesAccountAndCampusAtAcceptance(string status, string role, long? course, int code)
    {
        guest.Status = status; guest.Profile.First().Role = role; guest.CampiCourseId = course;
        Assert.Equal(code, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
        Assert.Equal("PENDING", invitation.Status); await tccs.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Theory]
    [InlineData("IN_PROGRESS")]
    [InlineData("PENDING_APPROVAL")]
    public async Task ExistingActiveWorkPreventsAcceptance(string status)
    {
        tccs.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), 0).Returns(new List<TccEntity> { new() { Id = 99, Status = status } });
        Assert.Equal(409, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
        Assert.Single(tcc.UserTccs);
    }

    [Theory]
    [InlineData("CANCELED")]
    [InlineData("REJECTED")]
    [InlineData("IN_PROGRESS")]
    [InlineData("COMPLETED")]
    public async Task ClosedProposalsCannotBeAccepted(string status)
    {
        tcc.Status = status;
        Assert.Equal(409, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
        await tccs.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task LimitAndDuplicateBindingAreRechecked()
    {
        for (var id = 3; id <= 5; id++) tcc.UserTccs.Add(new() { UserId = id, Profile = owner.Profile.First() });
        Assert.Equal(409, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
        tcc.UserTccs.Clear();
        tcc.UserTccs.Add(new() { UserId = 1, User = owner, Profile = owner.Profile.First() });
        tcc.UserTccs.Add(new() { UserId = 2, User = guest, Profile = guest.Profile.First() });
        Assert.Equal(409, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
    }

    [Fact]
    public async Task OutsiderCannotReadProposalConsent()
    {
        users.FindById(3).Returns(Student(3));
        Assert.Equal(404, (await useCase.Status(10, 3)).ErrorDetails?.Status);
        Assert.Equal("PENDING", Assert.Single((await useCase.Status(10, 1)).Data).Status);
    }

    [Fact]
    public async Task SubmitStoresInvitationWithoutBindingOrPrematureAdvisorNotification()
    {
        users.FindProposalParticipantByEmail(guest.Email).Returns(guest);
        var advisor = new UserEntity { Id = 3, Email = "advisor@example.com", Profile = [new() { Role = "ADVISOR" }] };
        users.FindById(3).Returns(advisor);
        var email = Substitute.For<IEmailGateway>();
        var submit = new SubmitTccProposalUseCase(users, tccs, Substitute.For<IProfileGateway>(), email,
            ActiveSemester(), Substitute.For<IAppLoggerGateway<SubmitTccProposalUseCase>>());
        var result = await submit.Execute(new("Projeto", "Resumo", 3, [" ALUNO2@example.com "]), 1);
        Assert.True(result.IsSuccess);
        await tccs.Received().Save(Arg.Is<TccEntity>(x => x.UserTccs.Count == 2 && x.ParticipantInvitations.Count == 1 && x.ParticipantInvitations.First().UserId == 2));
        await email.Received().Send(Arg.Is<gestaotcc.Domain.Dtos.Email.SendEmailDTO>(x => x.Recipient == guest.Email && x.TypeTemplate == "PROPOSAL-PARTICIPANT"));
        await email.DidNotReceive().Send(Arg.Is<gestaotcc.Domain.Dtos.Email.SendEmailDTO>(x => x.Recipient == advisor.Email));
    }

    [Theory]
    [InlineData("aluno1@example.com")]
    public async Task SubmitRejectsSelfOrMissingAccount(string address)
    {
        users.FindProposalParticipantByEmail(owner.Email).Returns(owner);
        users.FindById(3).Returns(new UserEntity { Profile = [new() { Role = "ADVISOR" }] });
        var submit = new SubmitTccProposalUseCase(users, tccs, Substitute.For<IProfileGateway>(), Substitute.For<IEmailGateway>(), ActiveSemester(), Substitute.For<IAppLoggerGateway<SubmitTccProposalUseCase>>());
        Assert.Equal(400, (await submit.Execute(new("Projeto", "Resumo", 3, [address]), 1)).ErrorDetails?.Status);
        await tccs.DidNotReceiveWithAnyArgs().Save(default!);
    }

    [Fact]
    public void ValidatorRejectsInvalidEmailAndTooManyParticipants()
    {
        var validator = new SubmitTccProposalValidator();
        Assert.False(validator.Validate(new SubmitTccProposalDTO("Projeto", "Resumo", 3, ["invalid"])).IsValid);
        Assert.False(validator.Validate(new SubmitTccProposalDTO("Projeto", "Resumo", 3, ["a@x.com", "b@x.com", "c@x.com", "d@x.com"])).IsValid);
        Assert.True(validator.Validate(new SubmitTccProposalDTO("Projeto", "Resumo", 3, ["a@x.com"])).IsValid);
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("DECLINED")]
    [InlineData("CANCELED")]
    public async Task AdvisorCannotApproveBeforeAllConsents(string status)
    {
        var advisor = new UserEntity { Id = 3, Profile = [new() { Role = "ADVISOR" }] };
        tcc.UserTccs.Add(new() { UserId = 3, User = advisor, Profile = advisor.Profile.First() });
        invitation.Status = status;
        var approve = new ApproveTccProposalUseCase(tccs, Substitute.For<IDocumentTypeGateway>(), Substitute.For<IEmailGateway>(), Substitute.For<IAppLoggerGateway<ApproveTccProposalUseCase>>());
        Assert.Equal(409, (await approve.Execute(10, 3)).ErrorDetails?.Status);
        Assert.Equal("AWAITING_PARTICIPANTS", tcc.Status); await tccs.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task StudentBindingDoesNotAuthorizeAdvisorApprovalOrRejection()
    {
        var approve = new ApproveTccProposalUseCase(tccs, Substitute.For<IDocumentTypeGateway>(), Substitute.For<IEmailGateway>(), Substitute.For<IAppLoggerGateway<ApproveTccProposalUseCase>>());
        Assert.Equal(403, (await approve.Execute(10, 1)).ErrorDetails?.Status);
        var reject = new RejectTccProposalUseCase(tccs, Substitute.For<IEmailGateway>(), Substitute.For<IAppLoggerGateway<RejectTccProposalUseCase>>());
        Assert.Equal(403, (await reject.Execute(10, new("Revisar"), 1)).ErrorDetails?.Status);
        await tccs.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task CancelEndsPendingInvitations()
    {
        var cancel = new CancelTccProposalUseCase(tccs, Substitute.For<IAppLoggerGateway<CancelTccProposalUseCase>>());
        Assert.True((await cancel.Execute(10, 1)).IsSuccess);
        Assert.Equal("CANCELED", invitation.Status); Assert.NotNull(invitation.RespondedAt);
        Assert.Equal(409, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
    }

    [Fact]
    public async Task ReformulationRequiresNewConsentFromPreviousGuest()
    {
        await useCase.Respond(20, 2, true); tcc.Status = "REJECTED";
        var reformulate = new ReformulateTccProposalUseCase(users, tccs, Substitute.For<IEmailGateway>(), Substitute.For<IAppLoggerGateway<ReformulateTccProposalUseCase>>());
        Assert.True((await reformulate.Execute(10, new("Revisão", "Novo resumo", null), 1)).IsSuccess);
        Assert.Equal("PENDING", invitation.Status); Assert.Null(invitation.RespondedAt);
        Assert.DoesNotContain(tcc.UserTccs, x => x.UserId == guest.Id);
    }

    [Fact]
    public async Task DuplicateEmailsAreRejectedBeforeSaving()
    {
        users.FindById(3).Returns(new UserEntity { Profile = [new() { Role = "ADVISOR" }] });
        var submit = new SubmitTccProposalUseCase(users, tccs, Substitute.For<IProfileGateway>(), Substitute.For<IEmailGateway>(), ActiveSemester(), Substitute.For<IAppLoggerGateway<SubmitTccProposalUseCase>>());
        Assert.Equal(400, (await submit.Execute(new("Projeto", "Resumo", 3, ["guest@example.com", " GUEST@example.com "]), 1)).ErrorDetails?.Status);
        await tccs.DidNotReceiveWithAnyArgs().Save(default!);
    }

    [Fact]
    public async Task NotificationFailureDoesNotUndoAcceptedParticipation()
    {
        var advisor = new UserEntity { Id = 3, Profile = [new() { Role = "ADVISOR" }] };
        tcc.UserTccs.Add(new() { UserId = 3, User = advisor, Profile = advisor.Profile.First() });
        var mail = Substitute.For<IEmailGateway>();
        var transactionActive = false;
        proposals.Atomic(Arg.Any<Func<Task<ResultPattern<string>>>>()).Returns(async call =>
        {
            transactionActive = true;
            var result = await call.Arg<Func<Task<ResultPattern<string>>>>()();
            transactionActive = false; return result;
        });
        mail.Send(Arg.Any<gestaotcc.Domain.Dtos.Email.SendEmailDTO>()).Returns(call =>
        {
            Assert.False(transactionActive);
            return Task.FromException<ResultPattern<bool>>(new InvalidOperationException("SMTP fictício indisponível"));
        });
        var notifying = new ProposalParticipantsUseCase(proposals, tccs, users, mail);
        Assert.True((await notifying.Respond(20, 2, true)).IsSuccess);
        Assert.Equal("ACCEPTED", invitation.Status); Assert.Single(tcc.UserTccs.Where(x => x.UserId == 2));
    }

    [Fact]
    public async Task AdvisorNotificationWaitsForLastConsent()
    {
        var advisor = new UserEntity { Id = 3, Profile = [new() { Role = "ADVISOR" }] };
        tcc.UserTccs.Add(new() { UserId = 3, User = advisor, Profile = advisor.Profile.First() });
        tcc.ParticipantInvitations.Add(new() { UserId = 4, Tcc = tcc });
        var mail = Substitute.For<IEmailGateway>();
        Assert.True((await new ProposalParticipantsUseCase(proposals, tccs, users, mail).Respond(20, 2, true)).IsSuccess);
        await mail.DidNotReceiveWithAnyArgs().Send(default!);
    }

    [Fact]
    public async Task UnknownEmailCreatesPendingInvitationWithoutAccountOrBinding()
    {
        users.FindById(3).Returns(new UserEntity { Profile = [new() { Role = "ADVISOR" }] });
        var mail = Substitute.For<IEmailGateway>();
        var submit = new SubmitTccProposalUseCase(users, tccs, Substitute.For<IProfileGateway>(), mail, ActiveSemester(), Substitute.For<IAppLoggerGateway<SubmitTccProposalUseCase>>());
        Assert.True((await submit.Execute(new("Projeto", "Resumo", 3, [" NEW@example.com "]), 1)).IsSuccess);
        await tccs.Received().Save(Arg.Is<TccEntity>(x => x.Status == "AWAITING_PARTICIPANTS" && x.UserTccs.Count == 2 &&
            x.ParticipantInvitations.Single().UserId == null && x.ParticipantInvitations.Single().Email == "new@example.com"));
        await users.DidNotReceiveWithAnyArgs().Save(default!);
        await mail.Received().Send(Arg.Is<gestaotcc.Domain.Dtos.Email.SendEmailDTO>(x => x.Recipient == "new@example.com"));
    }

    [Fact]
    public async Task EmailInvitationCanOnlyBeAnsweredByItsVerifiedAccount()
    {
        invitation.UserId = null; invitation.User = null;
        Assert.Equal(404, (await useCase.Respond(20, 1, true)).ErrorDetails?.Status);
        Assert.True((await useCase.Respond(20, 2, true)).IsSuccess);
        Assert.Equal(guest.Id, invitation.UserId); Assert.Equal("PENDING_APPROVAL", tcc.Status);
    }

    [Fact]
    public async Task DeclineNotifiesOwnerAndOnlyOwnerCanDismiss()
    {
        var mail = Substitute.For<IEmailGateway>();
        Assert.True((await new ProposalParticipantsUseCase(proposals, tccs, users, mail).Respond(20, 2, false)).IsSuccess);
        await mail.Received().Send(Arg.Is<gestaotcc.Domain.Dtos.Email.SendEmailDTO>(x => x.Recipient == owner.Email && x.TypeTemplate == "PROPOSAL-DECLINED"));
        Assert.Equal(404, (await useCase.Dismiss(20, 2)).ErrorDetails?.Status);
        Assert.True((await useCase.Dismiss(20, 1)).IsSuccess); Assert.NotNull(invitation.NotificationDismissedAt);
    }

    [Fact]
    public async Task AccountCannotAnswerIfItsEmailDiffersFromInvitation()
    {
        guest.Email = "other@example.com";
        Assert.Equal(404, (await useCase.Respond(20, 2, true)).ErrorDetails?.Status);
        Assert.Equal("PENDING", invitation.Status);
    }
}
