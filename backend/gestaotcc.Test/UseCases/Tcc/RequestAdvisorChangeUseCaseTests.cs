using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.UseCases.Tcc;

public class RequestAdvisorChangeUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly IAppLoggerGateway<RequestAdvisorChangeUseCase> _logger = Substitute.For<IAppLoggerGateway<RequestAdvisorChangeUseCase>>();
    private readonly RequestAdvisorChangeUseCase _useCase;

    public RequestAdvisorChangeUseCaseTests()
    {
        _useCase = new RequestAdvisorChangeUseCase(_tccGateway, _userGateway, _documentTypeGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenTccDoesNotExist()
    {
        _tccGateway.FindTccById(1).Returns((TccEntity?)null);

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Motivo", SuggestedAdvisorId: null, RequesterRole: "STUDENT");
        var result = await _useCase.Execute(dto, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("TCC não encontrado.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenPendingChangeAlreadyExists()
    {
        var tcc = new TccEntity { Id = 1 };
        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns(new TccAdvisorChangeEntity());

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Motivo", SuggestedAdvisorId: null, RequesterRole: "STUDENT");
        var result = await _useCase.Execute(dto, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("Já existe uma solicitação de substituição ou afastamento de orientador em andamento para este TCC.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenCurrentAdvisorIsNotFoundOnTcc()
    {
        var tcc = new TccEntity { Id = 1 }; // Sem UserTcc com ADVISOR
        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Motivo", SuggestedAdvisorId: null, RequesterRole: "STUDENT");
        var result = await _useCase.Execute(dto, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("Orientador atual não encontrado no TCC.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenRequesterIsNotStudentOfTcc()
    {
        var tcc = new TccEntity { Id = 1 };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } });
        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Motivo", SuggestedAdvisorId: null, RequesterRole: "STUDENT");
        var result = await _useCase.Execute(dto, 99); // 99 não é estudante deste TCC

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        Assert.Equal("Apenas estudantes vinculados a este TCC podem solicitar a substituição do orientador.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenRequesterIsNotCurrentAdvisor()
    {
        var tcc = new TccEntity { Id = 1 };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } });
        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Motivo", SuggestedAdvisorId: null, RequesterRole: "ADVISOR");
        var result = await _useCase.Execute(dto, 99); // 99 não é o orientador atual (que é 20)

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        Assert.Equal("Apenas o orientador atual do TCC pode solicitar o seu afastamento.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenRequesterRoleIsInvalid()
    {
        var tcc = new TccEntity { Id = 1 };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } });
        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Motivo", SuggestedAdvisorId: null, RequesterRole: "OUTRO_PAPEL");
        var result = await _useCase.Execute(dto, 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("Papel de solicitante inválido. Deve ser STUDENT ou ADVISOR.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenSuggestedAdvisorIsSameAsCurrent()
    {
        var tcc = new TccEntity { Id = 1 };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } });
        tcc.UserTccs.Add(new UserTccEntity { UserId = 10, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } });
        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Motivo", SuggestedAdvisorId: 20, RequesterRole: "STUDENT");
        var result = await _useCase.Execute(dto, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("O orientador substituto não pode ser o orientador atual.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenSuggestedAdvisorDoesNotExist()
    {
        var tcc = new TccEntity { Id = 1 };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } });
        tcc.UserTccs.Add(new UserTccEntity { UserId = 10, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } });
        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);
        _userGateway.FindById(30).Returns((UserEntity?)null);

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Motivo", SuggestedAdvisorId: 30, RequesterRole: "STUDENT");
        var result = await _useCase.Execute(dto, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Orientador substituto sugerido não encontrado.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldCreateChangeRequest_WhenStudentSubmitsValidRequest()
    {
        var tcc = new TccEntity { Id = 1, Title = "TCC Teste" };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } });
        tcc.UserTccs.Add(new UserTccEntity { UserId = 10, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } });

        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);
        _userGateway.FindById(30).Returns(new UserEntity { Id = 30, Name = "Novo Orientador" });

        var docType = new DocumentTypeEntity(1, "ANEXO IX - SUBSTITUIÇÃO DE ORIENTADOR", 1);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Mudança de escopo do projeto", SuggestedAdvisorId: 30, RequesterRole: "STUDENT");
        var result = await _useCase.Execute(dto, 10);

        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);
        await _tccGateway.Received(1).SaveAdvisorChange(Arg.Is<TccAdvisorChangeEntity>(c =>
            c.TccId == 1 &&
            c.RequesterUserId == 10 &&
            c.RequesterRole == "STUDENT" &&
            c.CurrentAdvisorId == 20 &&
            c.SuggestedAdvisorId == 30 &&
            c.Status == "PENDING_SIGNATURES"));
    }

    [Fact]
    public async Task Execute_ShouldReturnInternalServerError_WhenDatabaseThrowsException()
    {
        var tcc = new TccEntity { Id = 1, Title = "TCC Teste" };
        tcc.UserTccs.Add(new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } });
        _tccGateway.FindTccById(1).Returns(tcc);
        _tccGateway.FindPendingAdvisorChange(1).Returns((TccAdvisorChangeEntity?)null);
        _documentTypeGateway.FindAll().Throws(new Exception("DB connection timeout"));

        var dto = new RequestAdvisorChangeDTO(TccId: 1, Reason: "Afastamento médico", SuggestedAdvisorId: null, RequesterRole: "ADVISOR");
        var result = await _useCase.Execute(dto, 20);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro interno ao registrar solicitação de alteração de orientador.", result.Message);
    }
}
