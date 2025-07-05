using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class FindTccCancellationUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly FindTccCancellationUseCase _useCase;

    public FindTccCancellationUseCaseTests()
    {
        _useCase = new FindTccCancellationUseCase(_tccGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        // Arrange
        _tccGateway.FindTccCancellation(Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(null));

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Cancelamento do TCC não encontrado.", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccCancellationIsNull()
    {
        // Arrange
        var tcc = new TccEntity
        {
            TccCancellation = null
        };
        _tccGateway.FindTccCancellation(Arg.Any<long>()).Returns(Task.FromResult(tcc));

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Cancelamento do TCC não encontrado.", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WithCancellationDTO()
    {
        // Arrange
        var tcc = new TccEntity
        {
            Title = "Projeto X",
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity
                {
                    Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() },
                    User = new UserEntity { Name = "Aluno1" }
                },
                new UserTccEntity
                {
                    Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() },
                    User = new UserEntity { Name = "Orientador1" }
                }
            },
            TccCancellation = new TccCancellationEntity
            {
                Reason = "Motivo teste"
            }
        };
        _tccGateway.FindTccCancellation(Arg.Any<long>()).Returns(Task.FromResult(tcc));

        // Act
        var result = await _useCase.Execute(1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Projeto X", result.Data!.TitleTCC);
        Assert.Contains("Aluno1", result.Data.StudentName!);
        Assert.Equal("Orientador1", result.Data.AdvisorName);
        Assert.Equal("Motivo teste", result.Data.ReasonCancellation);
    }
}