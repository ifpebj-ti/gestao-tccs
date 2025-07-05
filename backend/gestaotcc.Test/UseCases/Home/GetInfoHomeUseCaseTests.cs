using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Home;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Home;

public class GetInfoHomeUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly FindAllPendingSignaturesUseCase _realPendingSignaturesUseCase;
    private readonly GetInfoHomeUseCase _useCase;

    public GetInfoHomeUseCaseTests()
    {
        _realPendingSignaturesUseCase = new FindAllPendingSignaturesUseCase(_tccGateway, _documentTypeGateway);
        _useCase = new GetInfoHomeUseCase(_userGateway, _tccGateway, _realPendingSignaturesUseCase);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserNull()
    {
        _userGateway.FindById(Arg.Any<long>()).Returns((UserEntity?)null);

        var result = await _useCase.Execute(123);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Contains("não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserHasNoProfiles()
    {
        var user = new UserEntity { Profile = new List<Domain.Entities.Profile.ProfileEntity>() };
        _userGateway.FindById(123).Returns(user);

        var result = await _useCase.Execute(123);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Contains("não possui perfil", result.Message);
    }

    [Fact]
    public async Task Execute_AsCoordinatorOrSupervisor_ShouldFetchPendingSignaturesAndTccs()
    {
        var user = new UserEntity
        {
            Profile = new List<Domain.Entities.Profile.ProfileEntity>
            {
                new() { Role = RoleType.COORDINATOR.ToString() }
            }
        };
        _userGateway.FindById(Arg.Any<long>()).Returns(user);

        var tccs = new List<TccEntity>
        {
            new TccEntity { Id = 1, Step = "START_AND_ORGANIZATION", Status = "IN_PROGRESS", Documents = [], UserTccs = [], TccInvites = [] },
            new TccEntity { Id = 2, Step = "START_AND_ORGANIZATION", Status = "IN_PROGRESS", Documents = [], UserTccs = [], TccInvites = [] },
            new TccEntity { Id = 3, Step = "START_AND_ORGANIZATION", Status = "IN_PROGRESS", Documents = [], UserTccs = [], TccInvites = [] }
        };
        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>()).Returns(tccs);
        _documentTypeGateway.FindAll().Returns(new List<Domain.Entities.DocumentType.DocumentTypeEntity>());

        var result = await _useCase.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data.TccInprogress);
        Assert.Equal(0, result.Data.PendingSignature);
    }

    [Theory]
    [InlineData(RoleType.ADVISOR)]
    [InlineData(RoleType.BANKING)]
    public async Task Execute_AsAdvisorOrBanking_ShouldFetchPendingSignaturesAndTccs(RoleType role)
    {
        var user = new UserEntity
        {
            Profile = new List<Domain.Entities.Profile.ProfileEntity>
            {
                new() { Role = role.ToString() }
            }
        };
        _userGateway.FindById(Arg.Any<long>()).Returns(user);

        var tccs = new List<TccEntity>
        {
            new TccEntity { Id = 1, Step = "START_AND_ORGANIZATION", Status = "IN_PROGRESS", Documents = [], UserTccs = [], TccInvites = [] },
            new TccEntity { Id = 2, Step = "START_AND_ORGANIZATION", Status = "IN_PROGRESS", Documents = [], UserTccs = [], TccInvites = [] }
        };
        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>()).Returns(tccs);
        _documentTypeGateway.FindAll().Returns(new List<Domain.Entities.DocumentType.DocumentTypeEntity>());

        var result = await _useCase.Execute(10);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.TccInprogress);
        Assert.Equal(0, result.Data.PendingSignature);
    }

    [Fact]
    public async Task Execute_AsStudent_ShouldFetchPendingSignaturesAndTccs()
    {
        var user = new UserEntity
        {
            Profile = new List<Domain.Entities.Profile.ProfileEntity>
            {
                new() { Role = RoleType.STUDENT.ToString() }
            }
        };
        _userGateway.FindById(Arg.Any<long>()).Returns(user);

        var tccs = new List<TccEntity>
        {
            new TccEntity { Id = 1, Step = "START_AND_ORGANIZATION", Status = "IN_PROGRESS", Documents = [], UserTccs = [], TccInvites = [] }
        };
        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>()).Returns(tccs);
        _documentTypeGateway.FindAll().Returns(new List<Domain.Entities.DocumentType.DocumentTypeEntity>());

        var result = await _useCase.Execute(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data.TccInprogress);
        Assert.Equal(0, result.Data.PendingSignature);

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>()).Returns(new List<TccEntity>());
        result = await _useCase.Execute(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data.TccInprogress);
    }
}