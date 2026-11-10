using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Auth;

public class BankingLoginUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITokenGateway _tokenGateway = Substitute.For<ITokenGateway>();
    private readonly IAppLoggerGateway<BankingLoginUseCase> _logger = Substitute.For<IAppLoggerGateway<BankingLoginUseCase>>();
    private readonly BankingLoginUseCase _useCase;

    public BankingLoginUseCaseTests()
    {
        _useCase = new BankingLoginUseCase(_tccGateway, _userGateway, _tokenGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnUnauthorized_WhenTokenIsNotFound()
    {
        _tccGateway.FindBankingMemberByToken("invalid-token").Returns((TccBankingMemberEntity?)null);

        var result = await _useCase.Execute("invalid-token");

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.ErrorDetails?.Status);
        Assert.Equal("Link inválido ou expirado.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenMemberHasRegisteredUserId()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Interno", "token-123", 1)
        {
            UserId = 42
        };
        _tccGateway.FindBankingMemberByToken("token-123").Returns(member);

        var result = await _useCase.Execute("token-123");

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        Assert.Equal("Este examinador deve acessar o sistema com sua conta cadastrada.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnUnauthorized_WhenTokenIsExpired()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Externo", "token-123", 1)
        {
            TokenExpiryDate = DateTime.UtcNow.AddMinutes(-10)
        };
        _tccGateway.FindBankingMemberByToken("token-123").Returns(member);

        var result = await _useCase.Execute("token-123");

        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.ErrorDetails?.Status);
        Assert.Equal("Este link expirou. Solicite um novo link de avaliação.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenUserDoesNotExistOrIsInactive()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Externo", "token-123", 1)
        {
            TokenExpiryDate = DateTime.UtcNow.AddHours(1)
        };
        _tccGateway.FindBankingMemberByToken("token-123").Returns(member);
        _userGateway.FindByEmail("exam@teste.com").Returns(new UserEntity { Id = 1, Status = "INACTIVE" });

        var result = await _useCase.Execute("token-123");

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Usuário não encontrado no sistema.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnInternalServerError_WhenTokenCreationFails()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Externo", "token-123", 1)
        {
            TokenExpiryDate = DateTime.UtcNow.AddHours(1)
        };
        var user = new UserEntity { Id = 10, Email = "exam@teste.com", Status = "ACTIVE" };

        _tccGateway.FindBankingMemberByToken("token-123").Returns(member);
        _userGateway.FindByEmail("exam@teste.com").Returns(user);
        _tokenGateway.CreateAccessToken(user).Returns((string?)null);

        var result = await _useCase.Execute("token-123");

        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao gerar autorização. Tente novamente.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenDataIsValid()
    {
        var member = new TccBankingMemberEntity("Examinador", "exam@teste.com", "Examinador Externo", "token-123", 1)
        {
            TokenExpiryDate = DateTime.UtcNow.AddHours(1)
        };
        var user = new UserEntity { Id = 10, Email = "exam@teste.com", Status = "ACTIVE" };

        _tccGateway.FindBankingMemberByToken("token-123").Returns(member);
        _userGateway.FindByEmail("exam@teste.com").Returns(user);
        _tokenGateway.CreateAccessToken(user).Returns("jwt-token-valido");

        var result = await _useCase.Execute("token-123");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("jwt-token-valido", result.Data.AccessToken);
    }
}

