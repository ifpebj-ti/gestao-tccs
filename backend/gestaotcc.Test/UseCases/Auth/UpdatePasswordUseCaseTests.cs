using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Auth;

public class UpdatePasswordUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IBcryptGateway _bcryptGateway = Substitute.For<IBcryptGateway>();
    private readonly UpdatePasswordUseCase _useCase;

    public UpdatePasswordUseCaseTests()
    {
        _useCase = new UpdatePasswordUseCase(_userGateway, _bcryptGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserNotFound()
    {
        _userGateway.FindByEmail("notfound@example.com").Returns((UserEntity?)null);

        var dto = new UpdatePasswordDTO("notfound@example.com", "newpass");
        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenAccessCodeIsActive()
    {
        var user = new UserEntity
        {
            Email = "user@example.com",
            AccessCode = new()
            {
                IsActive = true,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5),
                IsUserUpdatePassword = false
            }
        };
        _userGateway.FindByEmail(user.Email).Returns(user);

        var dto = new UpdatePasswordDTO(user.Email, "newpass");
        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Contains("não validado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenAccessCodeExpired()
    {
        var user = new UserEntity
        {
            Email = "user@example.com",
            AccessCode = new()
            {
                IsActive = false,
                ExpirationDate = DateTime.UtcNow.AddMinutes(-1),
                IsUserUpdatePassword = false
            }
        };
        _userGateway.FindByEmail(user.Email).Returns(user);

        var dto = new UpdatePasswordDTO(user.Email, "newpass");
        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Contains("expirado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenAccessCodeAlreadyUsed()
    {
        var user = new UserEntity
        {
            Email = "user@example.com",
            AccessCode = new()
            {
                IsActive = false,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5),
                IsUserUpdatePassword = true
            }
        };
        _userGateway.FindByEmail(user.Email).Returns(user);

        var dto = new UpdatePasswordDTO(user.Email, "newpass");
        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Contains("já utilizado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenPasswordUpdated()
    {
        var user = new UserEntity
        {
            Email = "user@example.com",
            Status = "PENDING",
            AccessCode = new()
            {
                IsActive = false,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5),
                IsUserUpdatePassword = false
            }
        };
        _userGateway.FindByEmail(user.Email).Returns(user);
        _bcryptGateway.GenerateHashPassword("newpass").Returns("hashedpass");

        var dto = new UpdatePasswordDTO(user.Email, "newpass");
        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("hashedpass", user.Password);
        Assert.True(user.AccessCode.IsUserUpdatePassword);
        Assert.Equal("ACTIVE", user.Status); // Alterou porque status era diferente de "INACTIVE"
        await _userGateway.Received(1).Update(user);
    }

    [Fact]
    public async Task Execute_ShouldNotChangeStatus_WhenUserIsInactive()
    {
        var user = new UserEntity
        {
            Email = "user@example.com",
            Status = "INACTIVE",
            AccessCode = new()
            {
                IsActive = false,
                ExpirationDate = DateTime.UtcNow.AddMinutes(5),
                IsUserUpdatePassword = false
            }
        };
        _userGateway.FindByEmail(user.Email).Returns(user);
        _bcryptGateway.GenerateHashPassword("newpass").Returns("hashedpass");

        var dto = new UpdatePasswordDTO(user.Email, "newpass");
        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("hashedpass", user.Password);
        Assert.True(user.AccessCode.IsUserUpdatePassword);
        Assert.Equal("INACTIVE", user.Status); // Status mantém INACTIVE
        await _userGateway.Received(1).Update(user);
    }
}