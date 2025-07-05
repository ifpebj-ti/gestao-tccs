using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccSchedule;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class CreateScheduleTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly CreateScheduleTccUseCase _useCase;

    public CreateScheduleTccUseCaseTests()
    {
        _useCase = new CreateScheduleTccUseCase(_tccGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns((TccEntity?)null);

        var dto = new ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(10, 0), "Sala 101", 1);
        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenTccAlreadyHasSchedule()
    {
        var tcc = new TccEntity { Id = 1, TccSchedule = new TccScheduleEntity() };
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var dto = new ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(14, 0), "Sala 202", 1);
        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("TCC já possui agendamento de defesa", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenScheduleIsCreated()
    {
        // Arrange
        var scheduleDate = DateOnly.FromDateTime(DateTime.Today);
        var scheduleTime = new TimeOnly(9, 30);
        var location = "Auditório";

        var tcc = new TccEntity { Id = 1, TccSchedule = null };
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var dto = new ScheduleTccDTO(scheduleDate, scheduleTime, location, 1);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);

        // Verifica se o agendamento foi criado corretamente
        Assert.NotNull(tcc.TccSchedule);
        Assert.Equal(location, tcc.TccSchedule.Location);
        Assert.Equal(1, tcc.TccSchedule.TccId);

        var expectedDateTime = DateTime.SpecifyKind(scheduleDate.ToDateTime(scheduleTime), DateTimeKind.Utc);
        Assert.Equal(expectedDateTime, tcc.TccSchedule.ScheduledDate);

        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnServerError_WhenUpdateFails()
    {
        var tcc = new TccEntity { Id = 1, TccSchedule = null };
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _tccGateway.When(g => g.Update(tcc)).Do(x => throw new Exception("DB Error"));

        var dto = new ScheduleTccDTO(DateOnly.FromDateTime(DateTime.Today), new TimeOnly(15, 0), "Sala 303", 1);
        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao criar agendamento de defesa do TCC", result.Message);
    }
}