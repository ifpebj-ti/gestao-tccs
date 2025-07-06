using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccSchedule;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.UseCases.Tcc;

public class EditScheduleTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly EditScheduleTccUseCase _useCase;

    public EditScheduleTccUseCaseTests()
    {
        _useCase = new EditScheduleTccUseCase(_tccGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccNotFound()
    {
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(null));

        var dto = new ScheduleTccDTO(null, null, null, 1);
        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenTccScheduleIsNull()
    {
        var tcc = new TccEntity { TccSchedule = null };
        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(tcc));

        var dto = new ScheduleTccDTO(null, null, null, 1);
        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("TCC ainda não possui agendamento de defesa", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldUpdateSchedule_WhenDataIsValid()
    {
        var originalDate = DateTime.UtcNow.AddDays(1);
        var tccSchedule = new TccScheduleEntity { ScheduledDate = originalDate, Location = "Old Location" };
        var tcc = new TccEntity { TccSchedule = tccSchedule };

        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);

        var newDate = new DateOnly(originalDate.Year, originalDate.Month, originalDate.Day);
        var newTime = new TimeOnly(originalDate.Hour, originalDate.Minute);
        var newLocation = "New Location";

        var dto = new ScheduleTccDTO(newDate, newTime, newLocation, 1);

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);
        Assert.Equal(DateTimeKind.Utc, tcc.TccSchedule.ScheduledDate.Kind);
        Assert.Equal(newLocation, tcc.TccSchedule.Location);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenExceptionOccurs()
    {
        var tccSchedule = new TccScheduleEntity { ScheduledDate = DateTime.UtcNow, Location = "Location" };
        var tcc = new TccEntity { TccSchedule = tccSchedule };

        _tccGateway.FindTccScheduling(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Throws(new Exception("DB error"));

        var dto = new ScheduleTccDTO(null, null, "New Location", 1);

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao editar agendamento de defesa do TCC", result.Message);
    }
}