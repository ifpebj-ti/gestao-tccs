using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccSchedule;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.UseCases.Tcc;

public class SaveScheduleInfoUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IAppLoggerGateway<SaveScheduleInfoUseCase> _logger = Substitute.For<IAppLoggerGateway<SaveScheduleInfoUseCase>>();
    private readonly SaveScheduleInfoUseCase _useCase;

    public SaveScheduleInfoUseCaseTests()
    {
        _useCase = new SaveScheduleInfoUseCase(_tccGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        _tccGateway.FindTccScheduling(1).Returns((TccEntity?)null);

        var dto = new ScheduleTccDTO(new DateOnly(2026, 12, 1), new TimeOnly(14, 0), "Sala 2", 1);
        var result = await _useCase.Execute(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenFirstScheduleIsMissingFields()
    {
        var tcc = new TccEntity { Id = 1, TccSchedule = null };
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var dto = new ScheduleTccDTO(null, null, null, 1);
        var result = await _useCase.Execute(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("Data, horário e local são obrigatórios para o primeiro agendamento", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldCreateSchedule_WhenFirstScheduleIsValid()
    {
        var tcc = new TccEntity { Id = 1, TccSchedule = null };
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var date = new DateOnly(2026, 12, 1);
        var time = new TimeOnly(14, 30);
        var dto = new ScheduleTccDTO(date, time, "Auditório Principal", 1);

        var result = await _useCase.Execute(1, dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(tcc.TccSchedule);
        Assert.Equal("Auditório Principal", tcc.TccSchedule.Location);
        Assert.Equal(DateTimeKind.Utc, tcc.TccSchedule.ScheduledDate.Kind);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldUpdateExistingSchedule_WhenValidDataProvided()
    {
        var existingSchedule = new TccScheduleEntity(10, DateTime.UtcNow.AddDays(2), "Sala Antiga", 1);
        var tcc = new TccEntity { Id = 1, TccSchedule = existingSchedule };
        _tccGateway.FindTccScheduling(1).Returns(tcc);

        var date = new DateOnly(2026, 12, 5);
        var time = new TimeOnly(16, 0);
        var dto = new ScheduleTccDTO(date, time, "Novo Local", 1);

        var result = await _useCase.Execute(1, dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Novo Local", tcc.TccSchedule.Location);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnInternalServerError_WhenDatabaseFails()
    {
        var existingSchedule = new TccScheduleEntity(10, DateTime.UtcNow.AddDays(2), "Sala", 1);
        var tcc = new TccEntity { Id = 1, TccSchedule = existingSchedule };
        _tccGateway.FindTccScheduling(1).Returns(tcc);
        _tccGateway.Update(Arg.Any<TccEntity>()).Throws(new Exception("Falha de banco de dados"));

        var dto = new ScheduleTccDTO(new DateOnly(2026, 12, 5), new TimeOnly(16, 0), "Sala 2", 1);
        var result = await _useCase.Execute(1, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao salvar info de agendamento do TCC", result.Message);
    }
}

