using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class RejectTccProposalUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<RejectTccProposalUseCase> _logger = Substitute.For<IAppLoggerGateway<RejectTccProposalUseCase>>();

    private readonly RejectTccProposalUseCase _useCase;

    public RejectTccProposalUseCaseTests()
    {
        _useCase = new RejectTccProposalUseCase(_tccGateway, _emailGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenAdvisorRejectsProposalWithReason()
    {
        // Arrange
        var advisor = new UserEntity { Id = 10, Name = "Advisor", Email = "adv@ifpe.edu.br" };
        var student = new UserEntity { Id = 1, Name = "Student", Email = "stu@ifpe.edu.br" };

        var tcc = new TccEntity
        {
            Id = 5,
            Title = "Proposta TCC",
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            Step = StepTccType.PROPOSAL_REGISTRATION.ToString()
        };

        tcc.UserTccs = new List<UserTccEntity>
        {
            new UserTccEntity { TccId = 5, UserId = 10, User = advisor, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } },
            new UserTccEntity { TccId = 5, UserId = 1, User = student, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        var dto = new RejectTccProposalDTO("Escopo muito amplo para um TCC 1.");

        // Act
        var result = await _useCase.Execute(5, dto, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StatusTccType.REJECTED.ToString(), tcc.Status);
        Assert.Equal("Escopo muito amplo para um TCC 1.", tcc.RejectionReason);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenReasonIsEmpty()
    {
        // Arrange
        var dto = new RejectTccProposalDTO("");

        // Act
        var result = await _useCase.Execute(5, dto, 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        // Arrange
        _tccGateway.FindTccById(99).Returns(Task.FromResult<TccEntity?>(null));
        var dto = new RejectTccProposalDTO("Motivo");

        // Act
        var result = await _useCase.Execute(99, dto, 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserIsNotAssignedAdvisor()
    {
        // Arrange
        var tcc = new TccEntity
        {
            Id = 5,
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } }
            }
        };
        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        var dto = new RejectTccProposalDTO("Motivo");

        // Act
        var result = await _useCase.Execute(5, dto, 99);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccIsNotPendingApproval()
    {
        // Arrange
        var tcc = new TccEntity
        {
            Id = 5,
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 10, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } }
            }
        };
        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        var dto = new RejectTccProposalDTO("Motivo");

        // Act
        var result = await _useCase.Execute(5, dto, 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }
}
