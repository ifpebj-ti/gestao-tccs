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

public class ReformulateTccProposalUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<ReformulateTccProposalUseCase> _logger = Substitute.For<IAppLoggerGateway<ReformulateTccProposalUseCase>>();

    private readonly ReformulateTccProposalUseCase _useCase;

    public ReformulateTccProposalUseCaseTests()
    {
        _useCase = new ReformulateTccProposalUseCase(_userGateway, _tccGateway, _emailGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenStudentReformulatesRejectedProposal()
    {
        // Arrange
        var advisor = new UserEntity { Id = 10, Name = "Advisor", Email = "adv@ifpe.edu.br", Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.ADVISOR.ToString() } } };
        var student = new UserEntity { Id = 1, Name = "Student", Email = "stu@ifpe.edu.br", Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.STUDENT.ToString() } } };

        var tcc = new TccEntity
        {
            Id = 5,
            Title = "Antigo Título",
            Summary = "Antigo Resumo",
            Status = StatusTccType.REJECTED.ToString(),
            RejectionReason = "Escopo muito amplo."
        };

        tcc.UserTccs = new List<UserTccEntity>
        {
            new UserTccEntity { TccId = 5, UserId = 10, User = advisor, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } },
            new UserTccEntity { TccId = 5, UserId = 1, User = student, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        var dto = new ReformulateTccProposalDTO("Novo Título Ajustado", "Novo Resumo Focado");

        // Act
        var result = await _useCase.Execute(5, dto, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Novo Título Ajustado", tcc.Title);
        Assert.Equal("Novo Resumo Focado", tcc.Summary);
        Assert.Equal(StatusTccType.PENDING_APPROVAL.ToString(), tcc.Status);
        Assert.Null(tcc.RejectionReason);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        // Arrange
        _tccGateway.FindTccById(99).Returns(Task.FromResult<TccEntity?>(null));
        var dto = new ReformulateTccProposalDTO("Título", "Resumo");

        // Act
        var result = await _useCase.Execute(99, dto, 1);

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
            Status = StatusTccType.REJECTED.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 2, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
            }
        };
        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        var dto = new ReformulateTccProposalDTO("Título", "Resumo");

        // Act
        var result = await _useCase.Execute(5, dto, 99);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccIsNotInRejectedStatus()
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
        var dto = new ReformulateTccProposalDTO("Título", "Resumo");

        // Act
        var result = await _useCase.Execute(5, dto, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }
}
