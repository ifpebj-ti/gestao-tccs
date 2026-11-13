using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Auth;

public class GoogleAccountEligibilityUseCaseTests
{
    private static readonly string[] AllowedDomains = ["ifpe.edu.br", "discente.ifpe.edu.br"];
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private GoogleAccountEligibilityUseCase CreateUseCase() => new(_userGateway);

    [Fact]
    public async Task Execute_ReturnsExistingActiveUser_WhenVerifiedEmailMatchesAllowedInstitutionalDomain()
    {
        var user = new UserEntity { Id = 10, Email = "aluno@discente.ifpe.edu.br", Status = "ACTIVE" };
        _userGateway.FindByEmail(user.Email).Returns(user);

        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO("  ALUNO@DISCENTE.IFPE.EDU.BR ", true), AllowedDomains);

        Assert.True(result.IsSuccess);
        Assert.Same(user, result.Data);
        await _userGateway.Received(1).FindByEmail("aluno@discente.ifpe.edu.br");
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
        await _userGateway.DidNotReceive().Update(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_RejectsUnverifiedEmail_WithoutLookingUpOrChangingLocalAccount()
    {
        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO("aluno@discente.ifpe.edu.br", false), AllowedDomains);

        AssertRejected(result);
        await _userGateway.DidNotReceive().FindByEmail(Arg.Any<string>());
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
        await _userGateway.DidNotReceive().Update(Arg.Any<UserEntity>());
    }

    [Theory]
    [InlineData("aluno@gmail.com")]
    [InlineData("aluno@sub.ifpe.edu.br")]
    [InlineData("aluno@ifpe.edu.br.attacker.com")]
    [InlineData("sem-email")]
    [InlineData("@ifpe.edu.br")]
    public async Task Execute_RejectsEmailOutsideExactAllowedDomains(string email)
    {
        var result = await CreateUseCase().Execute(new GoogleIdentityDTO(email, true), AllowedDomains);

        AssertRejected(result);
        await _userGateway.DidNotReceive().FindByEmail(Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_RejectsWhenAllowedDomainConfigurationIsMissing()
    {
        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO("admin@ifpe.edu.br", true), Array.Empty<string>());

        AssertRejected(result);
        await _userGateway.DidNotReceive().FindByEmail(Arg.Any<string>());
    }

    [Fact]
    public async Task Execute_RejectsWhenNoMatchingLocalAccountExists()
    {
        _userGateway.FindByEmail("desconhecido@ifpe.edu.br").Returns((UserEntity?)null);

        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO("desconhecido@ifpe.edu.br", true), AllowedDomains);

        AssertRejected(result);
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
    }

    [Theory]
    [InlineData("INACTIVE")]
    [InlineData("FIRST_ACCESS")]
    [InlineData("WAITINGFORCONFIRMATION")]
    [InlineData("PENDING")]
    public async Task Execute_RejectsLocalAccountThatIsNotActive(string status)
    {
        _userGateway.FindByEmail("aluno@discente.ifpe.edu.br").Returns(new UserEntity
        {
            Id = 10,
            Email = "aluno@discente.ifpe.edu.br",
            Status = status
        });

        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO("aluno@discente.ifpe.edu.br", true), AllowedDomains);

        AssertRejected(result);
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
        await _userGateway.DidNotReceive().Update(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_RejectsWhenGatewayReturnsDifferentEmailThanVerifiedIdentity()
    {
        _userGateway.FindByEmail("aluno@discente.ifpe.edu.br").Returns(new UserEntity
        {
            Id = 10,
            Email = "outra@ifpe.edu.br",
            Status = "ACTIVE"
        });

        var result = await CreateUseCase().Execute(
            new GoogleIdentityDTO("aluno@discente.ifpe.edu.br", true), AllowedDomains);

        AssertRejected(result);
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
        await _userGateway.DidNotReceive().Update(Arg.Any<UserEntity>());
    }

    private static void AssertRejected(gestaotcc.Domain.Errors.ResultPattern<UserEntity> result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(401, result.ErrorDetails?.Status);
        Assert.Equal("Não foi possível autenticar esta conta com o Google.", result.Message);
    }
}
