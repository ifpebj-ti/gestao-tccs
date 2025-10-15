using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Auth;

public class LoginUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IBcryptGateway _bcryptGateway = Substitute.For<IBcryptGateway>();
    private readonly ITokenGateway _tokenGateway = Substitute.For<ITokenGateway>();
    private readonly LoginUseCase _useCase;
    private readonly IAppLoggerGateway<LoginUseCase> _logger = Substitute.For<IAppLoggerGateway<LoginUseCase>>();

    public LoginUseCaseTests()
    {
        _useCase = new LoginUseCase(_userGateway, _bcryptGateway, _tokenGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        _userGateway.FindByCpf("000.000.000-00").Returns((UserEntity?)null);

        // Act
        var result = await _useCase.Execute("000.000.000-00");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserIsInactive()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "INACTIVE", CPF = "000.000.000-00" };
        _userGateway.FindByCpf(user.CPF).Returns(user);

        var result = await _useCase.Execute(user.CPF);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }


    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTokenIsNull()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "ACTIVE", CPF = "000.000.000-00" };
        _userGateway.FindByCpf(user.CPF).Returns(user);
        _tokenGateway.CreateAccessToken(user).Returns((string?)null);

        var result = await _useCase.Execute(user.CPF);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenLoginIsValid_ForActiveUser()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "ACTIVE", CPF = "000.000.000-00" };
        _userGateway.FindByCpf(user.CPF).Returns(user);
        _tokenGateway.CreateAccessToken(user).Returns("valid-token");

        var result = await _useCase.Execute(user.CPF);

        Assert.True(result.IsSuccess);
        Assert.Equal("valid-token", result.Data.AccessToken);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenLoginIsValid_ForFirstAccess()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "FIRST_ACCESS", Password = "firstpass", CPF = "000.000.000-00" };
        _userGateway.FindByCpf(user.CPF).Returns(user);
        _tokenGateway.CreateAccessToken(user).Returns("first-token");

        var result = await _useCase.Execute(user.CPF);

        Assert.True(result.IsSuccess);
        Assert.Equal("first-token", result.Data.AccessToken);
    }
}