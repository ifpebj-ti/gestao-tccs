using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.Tcc;

public class ForwardToLibraryUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<ForwardToLibraryUseCase> _logger = Substitute.For<IAppLoggerGateway<ForwardToLibraryUseCase>>();
    private readonly ForwardToLibraryUseCase _useCase;

    public ForwardToLibraryUseCaseTests()
    {
        _useCase = new ForwardToLibraryUseCase(_tccGateway, _userGateway, _emailGateway, _logger);
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
    public async Task Execute_ShouldReturnUnauthorized_WhenUserNotFoundOrInactive()
    {
        _userGateway.FindById(10).Returns((UserEntity?)null);

        var result = await _useCase.Execute(1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenUserIsNotCoordinatorOrAdmin()
    {
        var student = new UserEntity
        {
            Id = 5,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "STUDENT" } }
        };
        _userGateway.FindById(5).Returns(student);

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
    public async Task Execute_ShouldReturnBadRequest_WhenHardCoverNotReceivedYet()
    {
        var coordinator = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } }
        };
        _userGateway.FindById(10).Returns(coordinator);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            HardCoverReceivedAt = null // capa dura pendente!
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Contains("capa dura", result.ErrorDetails?.Detail ?? "");
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenAlreadyForwardedToLibrary()
    {
        var coordinator = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } }
        };
        _userGateway.FindById(10).Returns(coordinator);

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            HardCoverReceivedAt = DateTime.UtcNow.AddDays(-2),
            ForwardedToLibraryAt = DateTime.UtcNow.AddDays(-1) // já encaminhado
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10);
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldSucceed_AndSendEmailToLibrary()
    {
        var coordinator = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "COORDINATOR" } }
        };
        _userGateway.FindById(10).Returns(coordinator);

        var student = new UserEntity
        {
            Id = 20,
            Name = "João Estudante",
            Email = "joao@ifpe.edu.br",
            CampiCourse = new CampiCourseEntity { Course = new CourseEntity { Name = "ADS" } }
        };

        var advisor = new UserEntity
        {
            Id = 30,
            Name = "Prof. Orientador",
            Email = "orientador@ifpe.edu.br"
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "Sistema de Gestão",
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            HardCoverReceivedAt = DateTime.UtcNow.AddDays(-1),
            ForwardedToLibraryAt = null,
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { User = student, Profile = new ProfileEntity { Role = "STUDENT" } },
                new UserTccEntity { User = advisor, Profile = new ProfileEntity { Role = "ADVISOR" } }
            }
        };
        _tccGateway.FindTccById(1).Returns(tcc);

        var librarian = new UserEntity
        {
            Id = 50,
            Name = "Bibliotecária Ana",
            Email = "biblioteca@ifpe.edu.br",
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "LIBRARY" } }
        };
        _userGateway.FindAllByFilter(Arg.Any<UserFilterDTO>(), Arg.Any<long>())
            .Returns(new List<UserEntity> { librarian });

        var result = await _useCase.Execute(1, 10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(tcc.ForwardedToLibraryAt);
        await _tccGateway.Received(1).Update(tcc);
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(e =>
            e.TypeTemplate == "FORWARD-LIBRARY" &&
            e.Recipient == "biblioteca@ifpe.edu.br"));
    }
}
