using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.User;

public class FindUserByEmailUseCaseTests
{
    private readonly IUserGateway _userGateway;
    private readonly FindUserByEmailUseCase _useCase;

    public FindUserByEmailUseCaseTests()
    {
        _userGateway = Substitute.For<IUserGateway>();
        _useCase = new FindUserByEmailUseCase(_userGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = "test@example.com";
        var user = new UserEntity
        {
            Id = 1,
            Name = "Test User",
            Email = email.ToLower(),
            CPF = "00000000000",
            Password = "hashedpassword",
            Status = "Ativo",
            Profile = new List<Domain.Entities.Profile.ProfileEntity>(),
            Course = new Domain.Entities.Course.CourseEntity(),
            AccessCode = new Domain.Entities.AccessCode.AccessCodeEntity(),
            UserTccs = new List<Domain.Entities.UserTcc.UserTccEntity>(),
            Signatures = new List<Domain.Entities.Signature.SignatureEntity>(),
            Documents = new List<Domain.Entities.Document.DocumentEntity>()
        };

        _userGateway.FindByEmail(email.ToLower()).Returns(user);

        // Act
        var result = await _useCase.Execute(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(user, result.Data);
        Assert.Null(result.ErrorDetails);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "notfound@example.com";
        _userGateway.FindByEmail(email.ToLower()).Returns((UserEntity?)null);

        // Act
        var result = await _useCase.Execute(email);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Null(result.Data);
        Assert.NotNull(result.ErrorDetails);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao buscar o usuário. Por favor verifique as informações e tente novamente", result.Message);
    }
}