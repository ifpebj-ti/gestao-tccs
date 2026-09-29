using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class CancelTccProposalUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IAppLoggerGateway<CancelTccProposalUseCase> _logger = Substitute.For<IAppLoggerGateway<CancelTccProposalUseCase>>();

    private readonly CancelTccProposalUseCase _useCase;

    public CancelTccProposalUseCaseTests()
    {
        _useCase = new CancelTccProposalUseCase(_tccGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenStudentCancelsProposal()
    {
        // Arrange
        var tcc = new TccEntity
        {
            Id = 5,
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 1, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
            }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.Execute(5, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StatusTccType.CANCELED.ToString(), tcc.Status);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        // Arrange
        _tccGateway.FindTccById(99).Returns(Task.FromResult<TccEntity?>(null));

        // Act
        var result = await _useCase.Execute(99, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserIsNotStudentOfTcc()
    {
        // Arrange
        var tcc = new TccEntity
        {
            Id = 5,
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 2, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
            }
        };
        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));

        // Act
        var result = await _useCase.Execute(5, 99);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccIsNotInCancelableProposalStatus()
    {
        // Arrange
        var tcc = new TccEntity
        {
            Id = 5,
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 1, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
            }
        };
        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));

        // Act
        var result = await _useCase.Execute(5, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }
}
