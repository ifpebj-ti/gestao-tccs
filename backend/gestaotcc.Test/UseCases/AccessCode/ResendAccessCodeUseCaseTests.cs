using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Domain.Dtos.AccessCode;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.AccessCode;

public class ResendAccessCodeUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly ResendAccessCodeUseCase _useCase;

    public ResendAccessCodeUseCaseTests()
    {
        _useCase = new ResendAccessCodeUseCase(_userGateway, _emailGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var data = new ResendAccessCodeDTO("notfound@example.com");
        _userGateway.FindByEmail(data.UserEmail).Returns((UserEntity?)null);

        // Act
        var result = await _useCase.Execute(data, "ABC123");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao reenviar código de acesso. Por favor verifique as informações e tente novamente.", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenEmailFails()
    {
        // Arrange
        var data = new ResendAccessCodeDTO("user@example.com") ;
        var user = new UserEntity
        {
            Name = "Gabriel",
            Email = data.UserEmail,
            AccessCode = new AccessCodeEntity()
        };
        _userGateway.FindByEmail(data.UserEmail).Returns(user);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.FailureResult("Falha ao enviar email", 500));

        // Act
        var result = await _useCase.Execute(data, "ABC123");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(500, result.ErrorDetails?.Status);
        Assert.Equal("Falha ao enviar email", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenEmailIsSent()
    {
        // Arrange
        var data = new ResendAccessCodeDTO("user@example.com") ;
        var user = new UserEntity
        {
            Name = "Gabriel",
            Email = data.UserEmail,
            AccessCode = new AccessCodeEntity()
        };

        _userGateway.FindByEmail(data.UserEmail).Returns(user);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        // Act
        var result = await _useCase.Execute(data, "ABC123");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(user.AccessCode.IsActive);
        Assert.False(user.AccessCode.IsUserUpdatePassword);
        Assert.NotNull(user.AccessCode.Code);
        Assert.Equal(6, user.AccessCode.Code.Length);
        Assert.True(user.AccessCode.ExpirationDate > DateTime.UtcNow);
    }
}