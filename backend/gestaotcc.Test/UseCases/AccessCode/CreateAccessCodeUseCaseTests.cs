using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using NSubstitute;

namespace gestaotcc.Test.UseCases.AccessCode;

public class CreateAccessCodeUseCaseTests
{
    private readonly CreateAccessCodeUseCase _useCase;
    private readonly IAppLoggerGateway<CreateAccessCodeUseCase> _logger;

    public CreateAccessCodeUseCaseTests()
    {
        _logger = Substitute.For<IAppLoggerGateway<CreateAccessCodeUseCase>>();
        _useCase = new CreateAccessCodeUseCase(_logger);
    }
    
    [Fact]
    public void Execute_ShouldReturnValidAccessCode()
    {
        // Arrange
        var combination = "ABCDEFG123456";

        // Act
        var result = _useCase.Execute(combination);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        var accessCode = result.Data;

        Assert.Equal(6, accessCode.Code.Length);
        Assert.True(accessCode.IsActive);
        Assert.False(accessCode.IsUserUpdatePassword);

        var now = DateTime.UtcNow;
        var expectedExpiration = now.AddMinutes(5);

        Assert.True(accessCode.ExpirationDate > now);
        Assert.True(accessCode.ExpirationDate <= expectedExpiration.AddSeconds(1));
    }
}