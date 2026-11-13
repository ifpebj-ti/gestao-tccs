using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.TccBankingMember;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace gestaotcc.Test.UseCases.Tcc;

public class EvaluateTccUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IAppLoggerGateway<EvaluateTccUseCase> _logger = Substitute.For<IAppLoggerGateway<EvaluateTccUseCase>>();
    private readonly EvaluateTccUseCase _useCase;

    public EvaluateTccUseCaseTests()
    {
        _useCase = new EvaluateTccUseCase(_tccGateway, _logger);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10.5)]
    public async Task ExecuteRegisteredUser_ShouldReturnBadRequest_WhenGradeIsOutOfRange(decimal grade)
    {
        var result = await _useCase.ExecuteRegisteredUser(1, 10, grade, "Comentário", "Detalhes");

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("A nota deve estar entre 0 e 10.", result.Message);
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldReturnForbidden_WhenUserIsNotExaminerOrAdvisor()
    {
        _tccGateway.FindBankingMemberByTccAndUser(1, 10).Returns((TccBankingMemberEntity?)null);

        var result = await _useCase.ExecuteRegisteredUser(1, 10, 8.5m, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        Assert.Equal("O usuário não está cadastrado como examinador interno deste TCC.", result.Message);
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldReturnConflict_WhenPresentationNotConcludedYet()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Interno", "", 1)
        {
            UserId = 10,
            TokenExpiryDate = null // Apresentação ainda não concluída
        };
        _tccGateway.FindBankingMemberByTccAndUser(1, 10).Returns(member);

        var result = await _useCase.ExecuteRegisteredUser(1, 10, 9.0m, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("A avaliacao ficara disponivel apos a conclusao da apresentacao.", result.Message);
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldReturnConflict_WhenGradeAlreadySubmitted()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Interno", "", 1)
        {
            UserId = 10,
            TokenExpiryDate = DateTime.UtcNow.AddHours(1),
            Grade = 8.0m // Já avaliou
        };
        _tccGateway.FindBankingMemberByTccAndUser(1, 10).Returns(member);

        var result = await _useCase.ExecuteRegisteredUser(1, 10, 9.0m, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("A avaliação já foi enviada.", result.Message);
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldSaveEvaluation_WhenDataIsValid()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Interno", "", 1)
        {
            UserId = 10,
            TokenExpiryDate = DateTime.UtcNow.AddHours(1)
        };
        _tccGateway.FindBankingMemberByTccAndUser(1, 10).Returns(member);

        var result = await _useCase.ExecuteRegisteredUser(1, 10, 8.5m, "Excelente trabalho", "Rubrica preenchida");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data);
        Assert.Equal(8.5m, member.Grade);
        Assert.Equal("Excelente trabalho", member.EvaluationComments);
        Assert.Equal("Rubrica preenchida", member.EvaluationDetails);
        await _tccGateway.Received(1).UpdateBankingMember(member);
    }

    [Fact]
    public async Task ExecuteRegisteredUser_ShouldReturnInternalServerError_WhenDatabaseFails()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Interno", "", 1)
        {
            UserId = 10,
            TokenExpiryDate = DateTime.UtcNow.AddHours(1)
        };
        _tccGateway.FindBankingMemberByTccAndUser(1, 10).Returns(member);
        _tccGateway.UpdateBankingMember(Arg.Any<TccBankingMemberEntity>()).Throws(new Exception("DB erro"));

        var result = await _useCase.ExecuteRegisteredUser(1, 10, 8.5m, null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao salvar a avaliação.", result.Message);
    }

    // --- Testes de Execute (External Examiner via Token) ---

    [Fact]
    public async Task ExecuteExternal_ShouldReturnBadRequest_WhenTokenIsMissing()
    {
        var dto = new EvaluateTccDTO("", 9.0m, "Aprovado", null);

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("Token inválido ou não fornecido.", result.Message);
    }

    [Fact]
    public async Task ExecuteExternal_ShouldReturnUnauthorized_WhenTokenNotFound()
    {
        _tccGateway.FindBankingMemberByToken("tok-inexistente").Returns((TccBankingMemberEntity?)null);

        var dto = new EvaluateTccDTO("tok-inexistente", 9.0m, "Aprovado", null);

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.ErrorDetails?.Status);
        Assert.Equal("Acesso negado ou token inválido.", result.Message);
    }

    [Fact]
    public async Task ExecuteExternal_ShouldReturnForbidden_WhenMemberHasUserId()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Interno", "token-ext", 1)
        {
            UserId = 50
        };
        _tccGateway.FindBankingMemberByToken("token-ext").Returns(member);

        var dto = new EvaluateTccDTO("token-ext", 9.0m, "Aprovado", null);

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        Assert.Equal("Este examinador deve enviar a avaliação autenticado no sistema.", result.Message);
    }

    [Fact]
    public async Task ExecuteExternal_ShouldReturnBadRequest_WhenTokenIsExpired()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Externo", "token-ext", 1)
        {
            TokenExpiryDate = DateTime.UtcNow.AddMinutes(-5)
        };
        _tccGateway.FindBankingMemberByToken("token-ext").Returns(member);

        var dto = new EvaluateTccDTO("token-ext", 9.0m, "Aprovado", null);

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("O link de avaliação expirou.", result.Message);
    }

    [Fact]
    public async Task ExecuteExternal_ShouldReturnBadRequest_WhenGradeAlreadySubmitted()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Externo", "token-ext", 1)
        {
            TokenExpiryDate = DateTime.UtcNow.AddHours(1),
            Grade = 7.5m
        };
        _tccGateway.FindBankingMemberByToken("token-ext").Returns(member);

        var dto = new EvaluateTccDTO("token-ext", 9.0m, "Aprovado", null);

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
        Assert.Equal("A avaliação já foi realizada anteriormente.", result.Message);
    }

    [Fact]
    public async Task ExecuteExternal_ShouldSaveEvaluation_WhenDataIsValid()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Externo", "token-ext", 7)
        {
            TokenExpiryDate = DateTime.UtcNow.AddHours(1)
        };
        _tccGateway.FindBankingMemberByToken("token-ext").Returns(member);

        var dto = new EvaluateTccDTO("token-ext", 8.0m, null, "Critérios atendidos");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Data);
        Assert.Equal(8.0m, member.Grade);
        Assert.Equal("Aprovado", member.EvaluationComments); // Default para nota >= 7
        Assert.Equal("Critérios atendidos", member.EvaluationDetails);
        await _tccGateway.Received(1).UpdateBankingMember(member);
    }
}

