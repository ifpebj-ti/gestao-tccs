using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.Tcc;

public class ReviewAdvisorChangeUseCasesTests
{
    [Fact]
    public async Task Approve_replaces_old_advisor_and_records_approval()
    {
        var tccGateway = Substitute.For<ITccGateway>();
        var users = Substitute.For<IUserGateway>();
        var email = Substitute.For<IEmailGateway>();
        var logger = Substitute.For<IAppLoggerGateway<ApproveAdvisorChangeUseCase>>();
        email.Send(Arg.Any<SendEmailDTO>()).Returns(gestaotcc.Domain.Errors.ResultPattern<bool>.SuccessResult(true));
        var useCase = new ApproveAdvisorChangeUseCase(tccGateway, users, email, logger);
        var student = new UserEntity { Id = 20, Name = "Aluno", Email = "aluno@example.org", CampiCourseId = 7, Profile = new[] { new ProfileEntity { Role = "STUDENT" } } };
        var oldAdvisor = new UserEntity { Id = 30, Name = "Antigo", Email = "antigo@example.org", Profile = new[] { new ProfileEntity { Role = "ADVISOR" } } };
        var newAdvisor = new UserEntity { Id = 40, Name = "Novo", CampiCourseId = 7, Email = "novo@example.org", Status = "ACTIVE", Profile = new[] { new ProfileEntity { Id = 4, Role = "ADVISOR" } } };
        var coordinator = new UserEntity { Id = 10, Status = "ACTIVE", CampiCourseId = 7, Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } } };
        var oldLink = new UserTccEntity { UserId = oldAdvisor.Id, User = oldAdvisor, Profile = new ProfileEntity { Role = "ADVISOR" } };
        var tcc = new TccEntity { Id = 1, Title = "TCC", UserTccs = new List<UserTccEntity> { new() { UserId = student.Id, User = student, Profile = new ProfileEntity { Role = "STUDENT" } }, oldLink } };
        var change = new TccAdvisorChangeEntity { Id = 50, TccId = 1, Status = "PENDING_COORDINATION", CurrentAdvisorId = oldAdvisor.Id, CurrentAdvisor = oldAdvisor, SuggestedAdvisorId = newAdvisor.Id };
        users.FindById(10).Returns(coordinator);
        users.FindById(newAdvisor.Id).Returns(newAdvisor);
        tccGateway.FindTccById(1).Returns(tcc);
        tccGateway.FindPendingAdvisorChange(1).Returns(change);

        var result = await useCase.Execute(1, 10);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(oldLink, tcc.UserTccs);
        Assert.Contains(tcc.UserTccs, link => link.UserId == newAdvisor.Id && link.Profile.Role == "ADVISOR");
        Assert.Equal("APPROVED", change.Status);
        Assert.Equal(10, change.ReviewedByUserId);
        await email.Received(3).Send(Arg.Is<SendEmailDTO>(message =>
            message.TypeTemplate == "ADVISOR-CHANGE-APPROVED" &&
            new[] { "aluno@example.org", "antigo@example.org", "novo@example.org" }.Contains(message.Recipient)));
        await tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Reject_records_reason_and_does_not_change_current_advisor()
    {
        var tccGateway = Substitute.For<ITccGateway>();
        var users = Substitute.For<IUserGateway>();
        var email = Substitute.For<IEmailGateway>();
        var logger = Substitute.For<IAppLoggerGateway<RejectAdvisorChangeUseCase>>();
        email.Send(Arg.Any<SendEmailDTO>()).Returns(gestaotcc.Domain.Errors.ResultPattern<bool>.SuccessResult(true));
        var useCase = new RejectAdvisorChangeUseCase(tccGateway, users, email, logger);
        var coordinator = new UserEntity { Id = 10, Status = "ACTIVE", CampiCourseId = 7, Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } } };
        var student = new UserEntity { Id = 20, Name = "Aluno", Email = "aluno@example.org", CampiCourseId = 7, Profile = new[] { new ProfileEntity { Role = "STUDENT" } } };
        var oldAdvisor = new UserEntity { Id = 30, Name = "Orientador", Email = "orientador@example.org" };
        var currentLink = new UserTccEntity { UserId = 30, User = oldAdvisor, Profile = new ProfileEntity { Role = "ADVISOR" } };
        var tcc = new TccEntity { Id = 1, UserTccs = new List<UserTccEntity> { new() { UserId = 20, User = student, Profile = new ProfileEntity { Role = "STUDENT" } }, currentLink } };
        var change = new TccAdvisorChangeEntity { Id = 50, TccId = 1, Status = "PENDING_COORDINATION", RequesterRole = "STUDENT", RequesterUser = student, CurrentAdvisor = oldAdvisor };
        users.FindById(10).Returns(coordinator);
        tccGateway.FindTccById(1).Returns(tcc);
        tccGateway.FindPendingAdvisorChange(1).Returns(change);

        var result = await useCase.Execute(1, 10, new RejectAdvisorChangeDTO("Justificativa insuficiente."));

        Assert.True(result.IsSuccess);
        Assert.Equal("REJECTED", change.Status);
        Assert.Equal("Justificativa insuficiente.", change.ReviewNotes);
        Assert.Contains(currentLink, tcc.UserTccs);
        await email.Received(2).Send(Arg.Is<SendEmailDTO>(message =>
            message.TypeTemplate == "ADVISOR-CHANGE-REJECTED" &&
            new[] { "aluno@example.org", "orientador@example.org" }.Contains(message.Recipient)));
        await tccGateway.Received(1).UpdateAdvisorChange(change);
    }
}
