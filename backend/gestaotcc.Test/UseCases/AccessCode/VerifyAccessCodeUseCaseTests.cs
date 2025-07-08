using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Domain.Dtos.AccessCode;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.AccessCode;

public class VerifyAccessCodeUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly VerifyAccessCodeUseCase _useCase;

    public VerifyAccessCodeUseCaseTests()
    {
        _useCase = new VerifyAccessCodeUseCase(_userGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var data = new VerifyAccessCodeDTO("notfound@example.com", "ABC123");
        _userGateway.FindByEmail(data.UserEmail).Returns((UserEntity?)null);

        // Act
        var result = await _useCase.Execute(data);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenAccessCodeIsNotActive()
    {
        // Arrange
        var data = new VerifyAccessCodeDTO("user@example.com", "ABC123");
        var user = new UserEntity
        {
            Email = data.UserEmail,
            AccessCode = new AccessCodeEntity
            {
                Code = "ABC123",
                IsActive = false,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5)
            }
        };
        _userGateway.FindByEmail(data.UserEmail).Returns(user);

        // Act
        var result = await _useCase.Execute(data);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("Código de acesso já validado. Por favor gere outro e tente novamente.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenAccessCodeDoesNotMatch()
    {
        // Arrange
        var data = new VerifyAccessCodeDTO("user@example.com", "WRONG123");
        var user = new UserEntity
        {
            Email = data.UserEmail,
            AccessCode = new AccessCodeEntity
            {
                Code = "ABC123",
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5)
            }
        };
        _userGateway.FindByEmail(data.UserEmail).Returns(user);

        // Act
        var result = await _useCase.Execute(data);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao verificar código de acesso. Por favor verifique as informações e tente novamente.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenAccessCodeIsExpired()
    {
        // Arrange
        var data = new VerifyAccessCodeDTO("user@example.com", "ABC123");
        var user = new UserEntity
        {
            Email = data.UserEmail,
            AccessCode = new AccessCodeEntity
            {
                Code = "ABC123",
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddMinutes(-1) // Expirado
            }
        };
        _userGateway.FindByEmail(data.UserEmail).Returns(user);

        // Act
        var result = await _useCase.Execute(data);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("Código de acesso expirado. Por favor gere outro e tente novamente.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenAccessCodeIsValid()
    {
        // Arrange
        var data = new VerifyAccessCodeDTO("user@example.com", "ABC123");
        var user = new UserEntity
        {
            Email = data.UserEmail,
            AccessCode = new AccessCodeEntity
            {
                Code = "ABC123",
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5)
            }
        };
        _userGateway.FindByEmail(data.UserEmail).Returns(user);

        // Act
        var result = await _useCase.Execute(data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(user.AccessCode.IsActive); // Deve ser desativado
        await _userGateway.Received(1).Update(user); // Update foi chamado
    }    
}