using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class ApproveCancellationTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly ApproveCancellationTccUseCase _useCase;

    public ApproveCancellationTccUseCaseTests()
    {
        _useCase = new ApproveCancellationTccUseCase(_tccGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        _tccGateway.FindTccById(1).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails.Status);
        Assert.Equal("TCC não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenTccDoesNotHaveCancellationRequest()
    {
        var tcc = new TccEntity { Id = 1, TccCancellation = null };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails.Status);
        Assert.Equal("TCC não possui solicitação de cancelamento", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldApproveCancellation_WhenTccIsValid()
    {
        var cancellation = new TccCancellationEntity { Id = 1, Status = "PENDING" };
        var tcc = new TccEntity { Id = 1, Status = StatusTccType.IN_PROGRESS.ToString(), TccCancellation = cancellation };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);
        Assert.Equal(StatusTccType.CANCELED.ToString(), tcc.Status);
        Assert.Equal("APPROVED", tcc.TccCancellation.Status);

        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUpdateThrowsException()
    {
        var cancellation = new TccCancellationEntity { Id = 1, Status = "PENDING" };
        var tcc = new TccEntity { Id = 1, TccCancellation = cancellation };

        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.When(g => g.Update(Arg.Any<TccEntity>())).Do(_ => throw new Exception("DB error"));

        var result = await _useCase.Execute(1);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails.Status);
        Assert.Equal("Erro ao aprovar cancelamento do TCC", result.Message);
    }
}