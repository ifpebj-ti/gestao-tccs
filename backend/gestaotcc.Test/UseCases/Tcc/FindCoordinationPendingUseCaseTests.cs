using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class FindCoordinationPendingUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly FindCoordinationPendingUseCase _useCase;

    public FindCoordinationPendingUseCaseTests()
    {
        _useCase = new FindCoordinationPendingUseCase(_tccGateway, _userGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenUserIdIsInvalid()
    {
        var result = await _useCase.Execute(0);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("Parâmetros inválidos.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnUnauthorized_WhenUserNotFoundOrInactive()
    {
        _userGateway.FindById(10).Returns(new UserEntity { Id = 10, Status = "INACTIVE" });

        var result = await _useCase.Execute(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.ErrorDetails?.Status);
        Assert.Equal("Usuário não autorizado ou inativo.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenUserIsNotCoordinatorOrSupervisor()
    {
        var user = new UserEntity
        {
            Id = 10,
            Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new() { Role = "STUDENT" } }
        };
        _userGateway.FindById(10).Returns(user);

        var result = await _useCase.Execute(10);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        Assert.Equal("Acesso restrito à Coordenação e Supervisão.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnCategorizedPendingItems_WhenUserIsCoordinator()
    {
        var coordinator = new UserEntity
        {
            Id = 1,
            Status = "ACTIVE",
            CampiCourseId = 100,
            Profile = new List<ProfileEntity> { new() { Role = "COORDINATOR" } }
        };
        _userGateway.FindById(1).Returns(coordinator);

        var student = new UserEntity { Id = 50, Name = "Aluno Teste" };

        // 1. TCC aguardando capa dura
        var tccHardCover = new TccEntity
        {
            Id = 1,
            Title = "TCC Capa Dura",
            Step = StepTccType.FINALIZATION_AND_PUBLICATION.ToString(),
            HardCoverReceivedAt = null
        };
        tccHardCover.UserTccs.Add(new UserTccEntity { UserId = student.Id, User = student, Profile = new ProfileEntity { Role = "STUDENT" } });

        // 2. TCC com capa dura recebida, aguardando envio à biblioteca
        var tccLibrary = new TccEntity
        {
            Id = 2,
            Title = "TCC Biblioteca",
            HardCoverReceivedAt = DateTime.UtcNow,
            ForwardedToLibraryAt = null
        };
        tccLibrary.UserTccs.Add(new UserTccEntity { UserId = student.Id, User = student, Profile = new ProfileEntity { Role = "STUDENT" } });

        // 3. TCC com cancelamento pendente
        var tccCancellation = new TccEntity
        {
            Id = 3,
            Title = "TCC Cancelamento",
            TccCancellation = new TccCancellationEntity(3, "Trancamento", "PENDING", 3)
        };
        tccCancellation.UserTccs.Add(new UserTccEntity { UserId = student.Id, User = student, Profile = new ProfileEntity { Role = "STUDENT" } });

        // 4. TCC com alteração de orientador pendente
        var tccAdvisorChange = new TccEntity
        {
            Id = 4,
            Title = "TCC Troca Orientador"
        };
        tccAdvisorChange.AdvisorChanges.Add(new TccAdvisorChangeEntity
        {
            Id = 10,
            TccId = 4,
            Status = "PENDING_COORDINATION",
            RequesterUser = student,
            RequesterRole = "STUDENT",
            Reason = "Mudança de tema",
            CreatedAt = DateTime.UtcNow
        });
        tccAdvisorChange.UserTccs.Add(new UserTccEntity { UserId = student.Id, User = student, Profile = new ProfileEntity { Role = "STUDENT" } });

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), 100)
            .Returns(new List<TccEntity> { tccHardCover, tccLibrary, tccCancellation, tccAdvisorChange });

        var result = await _useCase.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.AwaitingHardCoverCount);
        Assert.Equal(1, result.Data.AwaitingLibraryForwardCount);
        Assert.Equal(1, result.Data.AwaitingCancellationCount);
        Assert.Equal(1, result.Data.AwaitingAdvisorChangesCount);
        Assert.Single(result.Data.AwaitingHardCover);
        Assert.Single(result.Data.AwaitingLibraryForward);
        Assert.Single(result.Data.AwaitingCancellation);
        Assert.Single(result.Data.AwaitingAdvisorChanges);
    }
}
