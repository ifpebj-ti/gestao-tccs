using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Infra.Gateways;
using NSubstitute;

namespace gestaotcc.Test.UseCases.User;

public class ChangeOwnPasswordUseCaseTests
{
    private readonly IUserGateway _users = Substitute.For<IUserGateway>();
    private readonly IBcryptGateway _bcrypt = Substitute.For<IBcryptGateway>();
    private static ChangeOwnPasswordDTO Data => new("atual-de-teste", "nova-de-teste");

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidIdentity_DoesNotLoadUser(long id)
    {
        Assert.Equal(401, (await new ChangeOwnPasswordUseCase(_users, _bcrypt).Execute(id, Data)).ErrorDetails?.Status);
        await _users.DidNotReceiveWithAnyArgs().FindById(default);
    }

    [Theory]
    [InlineData("", "nova-de-teste")]
    [InlineData("atual-de-teste", "curta")]
    [InlineData("mesma-de-teste", "mesma-de-teste")]
    public async Task InvalidPasswords_DoNotSave(string current, string next)
    {
        Assert.Equal(400, (await new ChangeOwnPasswordUseCase(_users, _bcrypt).Execute(21, new(current, next))).ErrorDetails?.Status);
        await _users.DidNotReceiveWithAnyArgs().UpdateOwnPassword(default, default!, default!);
    }

    [Fact]
    public async Task Utf8Limit_IsCheckedBeforeHashing()
    {
        var result = await new ChangeOwnPasswordUseCase(_users, _bcrypt).Execute(21, new(Data.CurrentPassword, new string('á', 37)));
        Assert.Equal(400, result.ErrorDetails?.Status);
        _bcrypt.DidNotReceiveWithAnyArgs().GenerateHashPassword(default!);
    }

    [Theory]
    [InlineData("INACTIVE")]
    [InlineData("FIRST_ACCESS")]
    public async Task UnavailableAccount_IsNotUpdated(string status)
    {
        _users.FindById(21).Returns(new UserEntity { Id = 21, Status = status });
        Assert.Equal(403, (await new ChangeOwnPasswordUseCase(_users, _bcrypt).Execute(21, Data)).ErrorDetails?.Status);
        await _users.DidNotReceiveWithAnyArgs().UpdateOwnPassword(default, default!, default!);
    }

    [Fact]
    public async Task WrongCurrentPassword_DoesNotGenerateOrSaveHash()
    {
        _users.FindById(21).Returns(new UserEntity { Id = 21, Status = "ACTIVE", Password = "hash" });
        Assert.Equal(400, (await new ChangeOwnPasswordUseCase(_users, _bcrypt).Execute(21, Data)).ErrorDetails?.Status);
        _bcrypt.DidNotReceiveWithAnyArgs().GenerateHashPassword(default!);
        await _users.DidNotReceiveWithAnyArgs().UpdateOwnPassword(default, default!, default!);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeUsesRealBcryptAndConditionalUpdate(bool persisted)
    {
        var bcrypt = new BcryptGateway();
        var user = new UserEntity { Id = 21, Status = "ACTIVE", Name = "Teste", Email = "teste@example.org",
            Password = bcrypt.GenerateHashPassword(Data.CurrentPassword) };
        var previous = user.Password;
        _users.FindById(21).Returns(user);
        string? savedHash = null;
        _users.UpdateOwnPassword(21, previous, Arg.Any<string>()).Returns(call => { savedHash = call.ArgAt<string>(2); return persisted; });
        var result = await new ChangeOwnPasswordUseCase(_users, bcrypt).Execute(21, Data);
        Assert.Equal(persisted, result.IsSuccess);
        if (!persisted) Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.NotEqual(Data.NewPassword, savedHash);
        Assert.True(bcrypt.VerifyHashPassword(new UserEntity { Password = savedHash! }, Data.NewPassword));
        Assert.False(bcrypt.VerifyHashPassword(new UserEntity { Password = savedHash! }, Data.CurrentPassword));
        Assert.Equal(previous, user.Password);
        Assert.Equal("teste@example.org", user.Email);
        await _users.DidNotReceiveWithAnyArgs().Update(default!);
    }
}
