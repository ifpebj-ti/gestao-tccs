using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class ApproveTccProposalUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<ApproveTccProposalUseCase> _logger = Substitute.For<IAppLoggerGateway<ApproveTccProposalUseCase>>();

    private readonly ApproveTccProposalUseCase _useCase;

    public ApproveTccProposalUseCaseTests()
    {
        _useCase = new ApproveTccProposalUseCase(_tccGateway, _documentTypeGateway, _emailGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenAdvisorApprovesProposal()
    {
        // Arrange
        var advisor = new UserEntity { Id = 10, Name = "Advisor", Email = "adv@ifpe.edu.br" };
        var student = new UserEntity { Id = 1, Name = "Student", Email = "stu@ifpe.edu.br" };

        var tcc = new TccEntity
        {
            Id = 5,
            Title = "Proposta TCC",
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            Step = StepTccType.PROPOSAL_REGISTRATION.ToString(),
            Documents = new List<DocumentEntity>()
        };

        tcc.UserTccs = new List<UserTccEntity>
        {
            new UserTccEntity { TccId = 5, UserId = 10, User = advisor, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } },
            new UserTccEntity { TccId = 5, UserId = 1, User = student, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        // Act
        var result = await _useCase.Execute(5, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StatusTccType.IN_PROGRESS.ToString(), tcc.Status);
        Assert.Equal(StepTccType.START_AND_ORGANIZATION.ToString(), tcc.Step);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        // Arrange
        _tccGateway.FindTccById(99).Returns(Task.FromResult<TccEntity?>(null));

        // Act
        var result = await _useCase.Execute(99, 10);

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

        // Act
        var result = await _useCase.Execute(5, 99);

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

        // Act
        var result = await _useCase.Execute(5, 10);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }
}
