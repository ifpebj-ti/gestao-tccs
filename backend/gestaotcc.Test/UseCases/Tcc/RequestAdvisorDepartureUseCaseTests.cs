using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class RequestAdvisorDepartureUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<RequestAdvisorDepartureUseCase> _logger =
        Substitute.For<IAppLoggerGateway<RequestAdvisorDepartureUseCase>>();

    private readonly RequestAdvisorDepartureUseCase _useCase;

    private const long AdvisorUserId = 10;
    private const long SuggestedAdvisorId = 20;
    private const long TccId = 1;

    public RequestAdvisorDepartureUseCaseTests()
    {
        _useCase = new RequestAdvisorDepartureUseCase(_tccGateway, _userGateway, _documentTypeGateway, _emailGateway, _logger);
    }

    private static TccEntity BuildTccWithAdvisor(long advisorUserId)
    {
        var advisor = new UserEntity { Id = advisorUserId, Name = "Orientador Teste" };
        var advisorProfile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() };
        var student = new UserEntity { Id = 5, Name = "Estudante Teste" };
        var studentProfile = new ProfileEntity { Role = RoleType.STUDENT.ToString() };

        return new TccEntity
        {
            Id = TccId,
            Title = "TCC de Teste",
            Documents = [],
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = advisorUserId, User = advisor, Profile = advisorProfile },
                new() { UserId = 5, User = student, Profile = studentProfile }
            }
        };
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        // Arrange
        var dto = new RequestAdvisorDepartureDTO(TccId, "Justificativa de afastamento válida.", SuggestedAdvisorId);
        _tccGateway.FindTccById(TccId).Returns((TccEntity?)null);

        // Act
        var result = await _useCase.Execute(dto, AdvisorUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenPendingChangeExists()
    {
        // Arrange
        var dto = new RequestAdvisorDepartureDTO(TccId, "Justificativa de afastamento válida.", SuggestedAdvisorId);
        var tcc = BuildTccWithAdvisor(AdvisorUserId);
        _tccGateway.FindTccById(TccId).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(TccId).Returns(new TccAdvisorChangeEntity { Id = 99 });

        // Act
        var result = await _useCase.Execute(dto, AdvisorUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenRequesterIsNotCurrentAdvisor()
    {
        // Arrange
        var dto = new RequestAdvisorDepartureDTO(TccId, "Justificativa de afastamento válida.", SuggestedAdvisorId);
        var tcc = BuildTccWithAdvisor(AdvisorUserId);
        _tccGateway.FindTccById(TccId).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(TccId).Returns((TccAdvisorChangeEntity?)null);

        const long outroUsuarioId = 999;

        // Act
        var result = await _useCase.Execute(dto, outroUsuarioId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenSuggestedAdvisorIsSelf()
    {
        // Arrange
        var dto = new RequestAdvisorDepartureDTO(TccId, "Justificativa de afastamento válida.", AdvisorUserId);
        var tcc = BuildTccWithAdvisor(AdvisorUserId);
        _tccGateway.FindTccById(TccId).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(TccId).Returns((TccAdvisorChangeEntity?)null);

        // Act
        var result = await _useCase.Execute(dto, AdvisorUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenSuggestedAdvisorDoesNotExist()
    {
        // Arrange
        var dto = new RequestAdvisorDepartureDTO(TccId, "Justificativa de afastamento válida.", SuggestedAdvisorId);
        var tcc = BuildTccWithAdvisor(AdvisorUserId);
        _tccGateway.FindTccById(TccId).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(TccId).Returns((TccAdvisorChangeEntity?)null);
        _userGateway.FindById(SuggestedAdvisorId).Returns((UserEntity?)null);

        // Act
        var result = await _useCase.Execute(dto, AdvisorUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenDepartureIsRequested()
    {
        // Arrange
        var dto = new RequestAdvisorDepartureDTO(TccId, "Justificativa de afastamento válida.", SuggestedAdvisorId);
        var tcc = BuildTccWithAdvisor(AdvisorUserId);
        var suggestedAdvisor = new UserEntity { Id = SuggestedAdvisorId, Name = "Substituto Teste" };

        _tccGateway.FindTccById(TccId).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(TccId).Returns((TccAdvisorChangeEntity?)null);
        _userGateway.FindById(SuggestedAdvisorId).Returns(suggestedAdvisor);
        _documentTypeGateway.FindAll().Returns(new List<Domain.Entities.DocumentType.DocumentTypeEntity>());

        // Act
        var result = await _useCase.Execute(dto, AdvisorUserId);

        // Assert
        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).SaveAdvisorChange(
            Arg.Is<TccAdvisorChangeEntity>(ac =>
                ac.TccId == TccId &&
                ac.RequesterRole == "ADVISOR" &&
                ac.RequesterUserId == AdvisorUserId &&
                ac.SuggestedAdvisorId == SuggestedAdvisorId &&
                ac.Status == "PENDING_SIGNATURES"
            )
        );
    }

    [Fact]
    public async Task Execute_ShouldReturnServerError_WhenSaveThrowsException()
    {
        // Arrange
        var dto = new RequestAdvisorDepartureDTO(TccId, "Justificativa de afastamento válida.", SuggestedAdvisorId);
        var tcc = BuildTccWithAdvisor(AdvisorUserId);
        var suggestedAdvisor = new UserEntity { Id = SuggestedAdvisorId, Name = "Substituto Teste" };

        _tccGateway.FindTccById(TccId).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(TccId).Returns((TccAdvisorChangeEntity?)null);
        _userGateway.FindById(SuggestedAdvisorId).Returns(suggestedAdvisor);
        _documentTypeGateway.FindAll().Returns(new List<Domain.Entities.DocumentType.DocumentTypeEntity>());
        _tccGateway.When(g => g.SaveAdvisorChange(Arg.Any<TccAdvisorChangeEntity>()))
            .Do(_ => throw new Exception("Falha ao salvar"));

        // Act
        var result = await _useCase.Execute(dto, AdvisorUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
    }
}
