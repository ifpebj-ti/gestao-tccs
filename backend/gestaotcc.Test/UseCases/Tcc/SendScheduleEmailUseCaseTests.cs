using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Errors;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.UseCases.Tcc;

public class SendScheduleEmailUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly SendScheduleEmailUseCase _useCase;
    private readonly IAppLoggerGateway<SendScheduleEmailUseCase> _logger = Substitute.For<IAppLoggerGateway<SendScheduleEmailUseCase>>();

    public SendScheduleEmailUseCaseTests()
    {
        _useCase = new SendScheduleEmailUseCase(_tccGateway, _emailGateway, _storageGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        // Arrange
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns((TccEntity?)null);

        // Act
        var result = await _useCase.Execute(1, 9);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails.Status);
        Assert.Equal("TCC não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenTccHasNoSchedule()
    {
        // Arrange
        var tcc = AdvisorTcc(new TccEntity { Id = 1, Title = "TCC Teste", Summary = "Resumo", TccSchedule = null });
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(tcc);

        // Act
        var result = await _useCase.Execute(1, 9);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails.Status);
        Assert.Equal("TCC não possui agendamento de defesa", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldSendEmailsAndReturnSuccess_WhenScheduleExists()
    {
        // Arrange
        var user1 = new UserEntity { Id = 1, Name = "João", Email = "joao@email.com" };
        var user2 = new UserEntity { Id = 2, Name = "Maria", Email = "maria@email.com" };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Legal",
            Summary = "Um resumo",
            TccSchedule = new TccScheduleEntity
            {
                ScheduledDate = new DateTime(2025, 12, 25, 14, 0, 0),
                Location = "Auditório 1"
            },
            UserTccs = new List<UserTccEntity>
            {
                new() { User = user1 },
                new() { User = user2 }
            }
        };
        AdvisorTcc(tcc);

        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(tcc);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult());

        // Act
        var result = await _useCase.Execute(tcc.Id, 9);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Agenda enviada com mensagens adequadas ao perfil de cada participante.", result.Data);

        await _emailGateway.Received(2).Send(Arg.Any<SendEmailDTO>());
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(e => e.Recipient == "joao@email.com"));
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(e => e.Recipient == "maria@email.com"));
    }

    [Fact]
    public async Task Execute_ShouldReturnInternalServerError_WhenEmailSendFails()
    {
        // Arrange
        var user = new UserEntity { Id = 1, Name = "Erro", Email = "erro@email.com" };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC com erro",
            Summary = "Erro esperado",
            TccSchedule = new TccScheduleEntity
            {
                ScheduledDate = DateTime.UtcNow,
                Location = "Sala de Teste"
            },
            UserTccs = new List<UserTccEntity>
            {
                new() { User = user }
            }
        };
        AdvisorTcc(tcc);

        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(tcc);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Throws(new Exception("Erro simulado"));

        // Act
        var result = await _useCase.Execute(tcc.Id, 9);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails.Status);
        Assert.Equal("Erro ao enviar os comunicados do agendamento do TCC", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldSendRoleSpecificMessagesAndAttachTccOnlyToNonStudents()
    {
        var advisor = new UserEntity { Id = 9, Name = "Orientadora", Email = "advisor@example.com" };
        var student = new UserEntity { Id = 10, Name = "Estudante", Email = "student@example.com" };
        var tcc = AdvisorTcc(new TccEntity
        {
            Id = 1,
            Title = "TCC de teste",
            Summary = "Resumo detalhado",
            TccFile = "filled/test.pdf",
            TccSchedule = new TccScheduleEntity { ScheduledDate = new DateTime(2025, 12, 25, 14, 0, 0), Location = "Auditório" },
            BankingMembers = new List<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity>
            {
                new("Examinador interno", "internal@example.com", "Examinador Interno", "internal-token", 1) { UserId = 11 },
                new("Examinador externo", "external@example.com", "Examinador Externo", "external-token", 1),
                new("Orientadora", advisor.Email, "Orientador(a)", "advisor-token", 1) { UserId = advisor.Id }
            },
            UserTccs = new List<UserTccEntity>
            {
                new() { User = advisor, UserId = advisor.Id, Profile = new ProfileEntity { Role = "ADVISOR" } },
                new() { User = student, UserId = student.Id, Profile = new ProfileEntity { Role = "STUDENT" } }
            }
        });
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _storageGateway.Download("filled/test.pdf", false).Returns(new byte[] { 1, 2, 3 });
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult());

        var result = await _useCase.Execute(1, 9);

        Assert.True(result.IsSuccess);
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(email =>
            email.Recipient == "internal@example.com" && email.TypeTemplate == "BANKING-INTERNAL-SCHEDULE" && email.Attachments!.Count == 1));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(email =>
            email.Recipient == "external@example.com" && email.TypeTemplate == "BANKING-INVITE" && email.Attachments!.Count == 1));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(email =>
            email.Recipient == "advisor@example.com" && email.TypeTemplate == "SCHEDULE-TCC-ADVISOR" && email.Attachments == null &&
            (bool)email.Variables["has_tcc_attachment"] == false));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(email =>
            email.Recipient == "student@example.com" && email.TypeTemplate == "SCHEDULE-TCC-STUDENT" && email.Attachments == null));
    }

    [Fact]
    public async Task Execute_ShouldTellExaminerWhenTccFileIsUnavailable()
    {
        var tcc = AdvisorTcc(new TccEntity
        {
            Id = 1,
            Title = "TCC sem arquivo",
            TccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow.AddDays(2), Location = "Sala 1" },
            BankingMembers = new List<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity>
            {
                new("Examinador", "external@example.com", "Examinador Externo", "token", 1)
            }
        });
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult());

        var result = await _useCase.Execute(1, 9);

        Assert.True(result.IsSuccess);
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(email =>
            email.Recipient == "external@example.com" &&
            email.TypeTemplate == "BANKING-INVITE" &&
            (bool)email.Variables["has_tcc_attachment"] == false &&
            email.Attachments == null));
    }

    private static TccEntity AdvisorTcc(TccEntity tcc)
    {
        tcc.UserTccs.Add(new UserTccEntity { UserId = 9, Profile = new ProfileEntity { Role = "ADVISOR" } });
        return tcc;
    }
}
