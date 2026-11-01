using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class ConfirmHardCoverUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ConfirmHardCoverUseCase _useCase;

    public ConfirmHardCoverUseCaseTests()
    {
        _useCase = new ConfirmHardCoverUseCase(_tccGateway, _userGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenParametersInvalid()
    {
        var result = await _useCase.Execute(0, 1);
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);

        var result2 = await _useCase.Execute(1, 0);
        Assert.False(result2.IsSuccess);
        Assert.Equal(400, result2.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnUnauthorized_WhenCoordinatorNotFoundOrInactive()
    {
        _userGateway.FindById(10).Returns((UserEntity?)null);

        var result = await _useCase.Execute(1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenUserIsNotManagement()
    {
        var studentUser = new UserEntity
        {
            Id = 5,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "STUDENT" } }
        };
        _userGateway.FindById(5).Returns(studentUser);

        var result = await _useCase.Execute(1, 5);
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccNotFound()
    {
        var coordinator = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } }
        };
        _userGateway.FindById(10).Returns(coordinator);
        _tccGateway.FindTccById(1).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenCoordinatorCampusCourseDoesNotMatch()
    {
        var coordinator = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            CampiCourseId = 999,
            Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } }
        };
        _userGateway.FindById(10).Returns(coordinator);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new()
                {
                    UserId = 1,
                    Profile = new ProfileEntity { Role = "STUDENT" },
                    User = new UserEntity { Id = 1, CampiCourseId = 111 }
                }
            }
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenTccIsNotInFinalStage()
    {
        var coordinator = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "ADMIN" } }
        };
        _userGateway.FindById(10).Returns(coordinator);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.PROPOSAL_REGISTRATION.ToString(),
            UserTccs = new List<UserTccEntity>()
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Contains("etapa final", result.ErrorDetails?.Detail);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenHardCoverAlreadyReceived()
    {
        var coordinator = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "ADMIN" } }
        };
        _userGateway.FindById(10).Returns(coordinator);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            FinalSubmissionDeadline = DateTime.UtcNow.AddDays(10),
            HardCoverReceivedAt = DateTime.UtcNow.AddDays(-2),
            UserTccs = new List<UserTccEntity>()
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Contains("já foi confirmada", result.ErrorDetails?.Detail);
    }

    [Fact]
    public async Task Execute_ShouldConfirmSuccessfully_WhenDataIsValid()
    {
        var coordinator = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            CampiCourseId = 100,
            Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } }
        };
        _userGateway.FindById(10).Returns(coordinator);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            FinalSubmissionDeadline = DateTime.UtcNow.AddDays(20),
            UserTccs = new List<UserTccEntity>
            {
                new()
                {
                    UserId = 1,
                    Profile = new ProfileEntity { Role = "STUDENT" },
                    User = new UserEntity { Id = 1, CampiCourseId = 100 }
                }
            }
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.NotNull(tcc.HardCoverReceivedAt);
        Assert.Equal(10, tcc.HardCoverReceivedByUserId);

        await _tccGateway.Received(1).Update(tcc);
    }
}
