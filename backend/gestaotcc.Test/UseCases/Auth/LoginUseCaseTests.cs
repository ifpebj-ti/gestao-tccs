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

    public LoginUseCaseTests()
    {
        _useCase = new LoginUseCase(_userGateway, _bcryptGateway, _tokenGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        _userGateway.FindByEmail("notfound@example.com").Returns((UserEntity?)null);

        // Act
        var result = await _useCase.Execute("notfound@example.com", "any");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserIsInactive()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "INACTIVE" };
        _userGateway.FindByEmail(user.Email).Returns(user);

        var result = await _useCase.Execute(user.Email, "password");

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenPasswordInvalid_ForActiveUser()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "ACTIVE" };
        _userGateway.FindByEmail(user.Email).Returns(user);
        _bcryptGateway.VerifyHashPassword(user, "wrongpassword").Returns(false);

        var result = await _useCase.Execute(user.Email, "wrongpassword");

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenPasswordInvalid_ForFirstAccess()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "FIRST_ACCESS", Password = "123456" };
        _userGateway.FindByEmail(user.Email).Returns(user);

        var result = await _useCase.Execute(user.Email, "wrongpass");

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTokenIsNull()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "ACTIVE" };
        _userGateway.FindByEmail(user.Email).Returns(user);
        _bcryptGateway.VerifyHashPassword(user, "correctpass").Returns(true);
        _tokenGateway.CreateAccessToken(user).Returns((string?)null);

        var result = await _useCase.Execute(user.Email, "correctpass");

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenLoginIsValid_ForActiveUser()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "ACTIVE" };
        _userGateway.FindByEmail(user.Email).Returns(user);
        _bcryptGateway.VerifyHashPassword(user, "correctpass").Returns(true);
        _tokenGateway.CreateAccessToken(user).Returns("valid-token");

        var result = await _useCase.Execute(user.Email, "correctpass");

        Assert.True(result.IsSuccess);
        Assert.Equal("valid-token", result.Data.AccessToken);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenLoginIsValid_ForFirstAccess()
    {
        var user = new UserEntity { Email = "user@example.com", Status = "FIRST_ACCESS", Password = "firstpass" };
        _userGateway.FindByEmail(user.Email).Returns(user);
        _tokenGateway.CreateAccessToken(user).Returns("first-token");

        var result = await _useCase.Execute(user.Email, "firstpass");

        Assert.True(result.IsSuccess);
        Assert.Equal("first-token", result.Data.AccessToken);
    }
}