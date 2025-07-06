using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccSchedule;
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
    private readonly SendScheduleEmailUseCase _useCase;

    public SendScheduleEmailUseCaseTests()
    {
        _useCase = new SendScheduleEmailUseCase(_tccGateway, _emailGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        // Arrange
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns((TccEntity?)null);

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails.Status);
        Assert.Equal("TCC não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenTccHasNoSchedule()
    {
        // Arrange
        var tcc = new TccEntity { Id = 1, Title = "TCC Teste", Summary = "Resumo", TccSchedule = null };
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(tcc);

        // Act
        var result = await _useCase.Execute(1);

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

        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(tcc);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult());

        // Act
        var result = await _useCase.Execute(tcc.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Emails de agendamento de defesa do TCC enviados com sucesso.", result.Data);

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

        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(tcc);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Throws(new Exception("Erro simulado"));

        // Act
        var result = await _useCase.Execute(tcc.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails.Status);
        Assert.Equal("Erro ao enviar email de agendamento de defesa do TCC", result.Message);
    }
}