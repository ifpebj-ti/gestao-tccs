using gestaotcc.Application.UseCases.AccessCode;

namespace gestaotcc.Test.UseCases.AccessCode;

public class CreateAccessCodeUseCaseTests
{
    [Fact]
    public void Execute_ShouldReturnValidAccessCode()
    {
        // Arrange
        var combination = "ABCDEFG123456";
        var useCase = new CreateAccessCodeUseCase();

        // Act
        var result = useCase.Execute(combination);

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