using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class SubmitTccProposalUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IProfileGateway _profileGateway = Substitute.For<IProfileGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<SubmitTccProposalUseCase> _logger = Substitute.For<IAppLoggerGateway<SubmitTccProposalUseCase>>();

    private readonly SubmitTccProposalUseCase _useCase;

    public SubmitTccProposalUseCaseTests()
    {
        _useCase = new SubmitTccProposalUseCase(_userGateway, _tccGateway, _profileGateway, _emailGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenProposalIsSubmitted()
    {
        // Arrange
        var student = new UserEntity
        {
            Id = 1,
            Name = "Aluno Silva",
            Email = "aluno@discente.ifpe.edu.br",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
        };

        var advisor = new UserEntity
        {
            Id = 2,
            Name = "Prof. Orientador",
            Email = "orientador@ifpe.edu.br",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.ADVISOR.ToString() } }
        };

        _userGateway.FindById(1).Returns(Task.FromResult<UserEntity?>(student));
        _userGateway.FindById(2).Returns(Task.FromResult<UserEntity?>(advisor));
        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), Arg.Any<long>()).Returns(Task.FromResult(new List<TccEntity>()));
        _tccGateway.Save(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        var dto = new SubmitTccProposalDTO("Novo TCC sobre IA", "Resumo detalhado", 2);

        // Act
        var result = await _useCase.Execute(dto, 1);

        // Assert
        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).Save(Arg.Is<TccEntity>(t =>
            t.Title == "Novo TCC sobre IA" &&
            t.Status == StatusTccType.PENDING_APPROVAL.ToString() &&
            t.Step == StepTccType.PROPOSAL_REGISTRATION.ToString() &&
            t.UserTccs.Count == 2));
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenStudentNotFound()
    {
        // Arrange
        _userGateway.FindById(99).Returns(Task.FromResult<UserEntity?>(null));
        var dto = new SubmitTccProposalDTO("Título", "Resumo", 2);

        // Act
        var result = await _useCase.Execute(dto, 99);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserIsNotStudent()
    {
        // Arrange
        var nonStudent = new UserEntity
        {
            Id = 1,
            Name = "Docente",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.ADVISOR.ToString() } }
        };
        _userGateway.FindById(1).Returns(Task.FromResult<UserEntity?>(nonStudent));
        var dto = new SubmitTccProposalDTO("Título", "Resumo", 2);

        // Act
        var result = await _useCase.Execute(dto, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenAdvisorNotFound()
    {
        // Arrange
        var student = new UserEntity
        {
            Id = 1,
            Name = "Aluno",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
        };
        _userGateway.FindById(1).Returns(Task.FromResult<UserEntity?>(student));
        _userGateway.FindById(999).Returns(Task.FromResult<UserEntity?>(null));

        var dto = new SubmitTccProposalDTO("Título", "Resumo", 999);

        // Act
        var result = await _useCase.Execute(dto, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenStudentAlreadyHasActiveTcc()
    {
        // Arrange
        var student = new UserEntity
        {
            Id = 1,
            Name = "Aluno",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
        };
        var advisor = new UserEntity
        {
            Id = 2,
            Name = "Orientador",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.ADVISOR.ToString() } }
        };

        var activeTcc = new TccEntity { Id = 10, Status = StatusTccType.IN_PROGRESS.ToString() };

        _userGateway.FindById(1).Returns(Task.FromResult<UserEntity?>(student));
        _userGateway.FindById(2).Returns(Task.FromResult<UserEntity?>(advisor));
        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), Arg.Any<long>()).Returns(Task.FromResult(new List<TccEntity> { activeTcc }));

        var dto = new SubmitTccProposalDTO("Título", "Resumo", 2);

        // Act
        var result = await _useCase.Execute(dto, 1);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }
}
