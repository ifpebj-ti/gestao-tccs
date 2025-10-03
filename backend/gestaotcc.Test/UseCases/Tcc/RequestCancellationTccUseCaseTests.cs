using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class RequestCancellationTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly RequestCancellationTccUseCase _useCase;
    private readonly IAppLoggerGateway<RequestCancellationTccUseCase> _logger = Substitute.For<IAppLoggerGateway<RequestCancellationTccUseCase>>();

    public RequestCancellationTccUseCaseTests()
    {
        _useCase = new RequestCancellationTccUseCase(_tccGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        // Arrange
        var dto = new RequestCancellationTccDTO("Motivo de teste", 1);
        _tccGateway.FindTccById(dto.IdTcc).Returns((TccEntity?)null);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TCC não encontrado", result.Message);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenTccAlreadyHasCancellation()
    {
        // Arrange
        var dto = new RequestCancellationTccDTO("Motivo de teste", 1);
        var tcc = new TccEntity { TccCancellation = new TccCancellationEntity() };

        _tccGateway.FindTccById(dto.IdTcc).Returns(tcc);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TCC já possui solicitação de cancelamento", result.Message);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnError_WhenUpdateThrowsException()
    {
        // Arrange
        var dto = new RequestCancellationTccDTO("Motivo de teste", 1);
        var tcc = new TccEntity { Id = dto.IdTcc };

        _tccGateway.FindTccById(dto.IdTcc).Returns(tcc);
        _tccGateway
            .When(x => x.Update(Arg.Any<TccEntity>()))
            .Do(x => throw new Exception("Falha ao atualizar"));

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Erro ao solicitar cancelamento do TCC", result.Message);
        Assert.Equal(500, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenCancellationIsCreated()
    {
        // Arrange
        var dto = new RequestCancellationTccDTO("Motivo de cancelamento válido", 1);
        var tcc = new TccEntity { Id = dto.IdTcc, TccCancellation = null };

        _tccGateway.FindTccById(dto.IdTcc).Returns(tcc);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);
        await _tccGateway.Received(1).Update(Arg.Is<TccEntity>(t => t.TccCancellation != null));
    }
}