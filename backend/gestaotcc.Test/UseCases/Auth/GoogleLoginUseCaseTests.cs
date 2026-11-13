using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Auth;

public class GoogleLoginUseCaseTests
{
    private static readonly string[] AllowedDomains = ["ifpe.edu.br", "discente.ifpe.edu.br"];
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITokenGateway _tokenGateway = Substitute.For<ITokenGateway>();
    private GoogleLoginUseCase CreateUseCase() =>
        new(new GoogleAccountEligibilityUseCase(_userGateway), _tokenGateway);

    [Fact]
    public async Task Execute_ReturnsLocalJwtForEligibleGoogleIdentity()
    {
        var user = new UserEntity
        {
            Id = 10,
            Name = "Aluno",
            Email = "aluno@discente.ifpe.edu.br",
            Status = "ACTIVE",
            Profile = [new() { Role = "STUDENT" }]
        };
        _userGateway.FindByEmail(user.Email).Returns(user);
        _tokenGateway.CreateAccessToken(user).Returns("local-jwt");

        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO(user.Email, true), AllowedDomains);

        Assert.True(result.IsSuccess);
        Assert.Equal("local-jwt", result.Data.AccessToken);
        Assert.False(result.Data.IsDevTest);
        Assert.False(result.Data.IsTempDevTestPassword);
        _tokenGateway.Received(1).CreateAccessToken(user);
    }

    [Fact]
    public async Task Execute_DoesNotIssueJwtForRejectedIdentity()
    {
        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO("aluno@gmail.com", true), AllowedDomains);

        AssertRejected(result);
        _tokenGateway.DidNotReceive().CreateAccessToken(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_ReturnsGenericFailureWhenLocalJwtCannotBeCreated()
    {
        var user = new UserEntity
        {
            Id = 10,
            Email = "aluno@ifpe.edu.br",
            Status = "ACTIVE",
            Profile = [new() { Role = "STUDENT" }]
        };
        _userGateway.FindByEmail(user.Email).Returns(user);
        _tokenGateway.CreateAccessToken(user).Returns((string?)null);

        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO(user.Email, true), AllowedDomains);

        AssertRejected(result);
    }

    private static void AssertRejected(gestaotcc.Domain.Errors.ResultPattern<TokenDTO> result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(401, result.ErrorDetails?.Status);
        Assert.Equal("Não foi possível autenticar esta conta com o Google.", result.Message);
    }
}