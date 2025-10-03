using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class VerifyCodeInviteTccUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly VerifyCodeInviteTccUseCase _useCase;
    private readonly IAppLoggerGateway<VerifyCodeInviteTccUseCase> _logger = Substitute.For<IAppLoggerGateway<VerifyCodeInviteTccUseCase>>();

    public VerifyCodeInviteTccUseCaseTests()
    {
        _useCase = new VerifyCodeInviteTccUseCase(_userGateway, _tccGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturn404_WhenUserAlreadyExists()
    {
        // Arrange
        var dto = new VerifyCodeInviteTccDTO("email@teste.com", "123456");
        _userGateway.FindByEmail(dto.UserEmail).Returns(new UserEntity());

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails.Status);
        Assert.Equal("Erro ao verificar código", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturn409_WhenInviteIsInvalid()
    {
        // Arrange
        var dto = new VerifyCodeInviteTccDTO("email@teste.com", "123456");
        _userGateway.FindByEmail(dto.UserEmail).Returns((UserEntity?)null);

        var invite = new TccInviteEntity
        {
            Email = dto.UserEmail,
            Code = "123456",
            IsValidCode = false
        };

        _tccGateway.FindInviteTccByEmail(dto.UserEmail).Returns(invite);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails.Status);
        Assert.Equal("Erro ao verificar código", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturn409_WhenCodeDoesNotMatch()
    {
        // Arrange
        var dto = new VerifyCodeInviteTccDTO("email@teste.com", "WRONGCODE");
        _userGateway.FindByEmail(dto.UserEmail).Returns((UserEntity?)null);

        var invite = new TccInviteEntity
        {
            Email = dto.UserEmail,
            Code = "123456",
            IsValidCode = true
        };

        _tccGateway.FindInviteTccByEmail(dto.UserEmail).Returns(invite);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails.Status);
        Assert.Equal("Erro ao verificar código", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenCodeIsValid()
    {
        // Arrange
        var dto = new VerifyCodeInviteTccDTO("email@teste.com", "123456");
        _userGateway.FindByEmail(dto.UserEmail).Returns((UserEntity?)null);

        var invite = new TccInviteEntity
        {
            Email = dto.UserEmail,
            Code = "123456",
            IsValidCode = true
        };

        _tccGateway.FindInviteTccByEmail(dto.UserEmail).Returns(invite);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Operação realizada com sucesso.", result.Message);

        Assert.False(invite.IsValidCode);
        await _tccGateway.Received(1).UpdateTccInvite(invite);
    }
}