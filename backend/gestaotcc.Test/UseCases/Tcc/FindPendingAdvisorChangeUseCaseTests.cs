using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class FindPendingAdvisorChangeUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IAppLoggerGateway<FindPendingAdvisorChangeUseCase> _logger = Substitute.For<IAppLoggerGateway<FindPendingAdvisorChangeUseCase>>();
    private readonly FindPendingAdvisorChangeUseCase _useCase;

    public FindPendingAdvisorChangeUseCaseTests()
    {
        _useCase = new FindPendingAdvisorChangeUseCase(_tccGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnNull_WhenNoPendingChangeExists()
    {
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);

        var result = await _useCase.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Execute_ShouldReturnMappedDto_WhenPendingChangeExists()
    {
        var change = new TccAdvisorChangeEntity
        {
            Id = 5,
            TccId = 1,
            RequesterUserId = 10,
            RequesterUser = new UserEntity { Id = 10, Name = "Aluno Solicitante" },
            RequesterRole = "STUDENT",
            CurrentAdvisorId = 20,
            CurrentAdvisor = new UserEntity { Id = 20, Name = "Prof Atual" },
            SuggestedAdvisorId = 30,
            SuggestedAdvisor = new UserEntity { Id = 30, Name = "Prof Novo" },
            Reason = "Novo tema",
            Status = "PENDING_COORDINATION",
            DocumentId = 99,
            CreatedAt = new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc)
        };

        _tccGateway.FindPendingAdvisorChange(1).Returns(change);

        var result = await _useCase.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data.Id);
        Assert.Equal(1, result.Data.TccId);
        Assert.Equal("Aluno Solicitante", result.Data.RequesterName);
        Assert.Equal("Prof Atual", result.Data.CurrentAdvisorName);
        Assert.Equal("Prof Novo", result.Data.SuggestedAdvisorName);
        Assert.Equal("Novo tema", result.Data.Reason);
        Assert.Equal("PENDING_COORDINATION", result.Data.Status);
        Assert.Equal(99, result.Data.DocumentId);
    }
}

