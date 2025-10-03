using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Auth;

public class NewPasswordUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IBcryptGateway _bcryptGateway = Substitute.For<IBcryptGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly NewPasswordUseCase _useCase;
    private readonly IAppLoggerGateway<NewPasswordUseCase> _logger = Substitute.For<IAppLoggerGateway<NewPasswordUseCase>>();

    public NewPasswordUseCaseTests()
    {
        _useCase = new NewPasswordUseCase(_userGateway, _bcryptGateway, _tccGateway, _emailGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserOrTccInviteIsNull()
    {
        var dto = new NewPasswordDTO("user@example.com", "newpass", "code123");

        _userGateway.FindByEmail(dto.Email).Returns((UserEntity?)null);
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(new TccInviteEntity());

        var result = await _useCase.Execute(dto);
        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);

        // Caso o TccInvite seja null
        _userGateway.FindByEmail(dto.Email).Returns(new UserEntity());
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns((TccInviteEntity?)null);

        result = await _useCase.Execute(dto);
        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenInviteDataIsInvalid()
    {
        var dto = new NewPasswordDTO("user@example.com", "newpass", "code123");

        var user = new UserEntity { Email = dto.Email, Status = "PENDING" };
        var tccInvite = new TccInviteEntity
        {
            Email = "other@example.com",  // Email diferente
            Code = "code123",
            IsValidCode = false
        };

        _userGateway.FindByEmail(dto.Email).Returns(user);
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(tccInvite);

        var result = await _useCase.Execute(dto);
        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);

        // Teste com código diferente
        tccInvite.Email = dto.Email;
        tccInvite.Code = "wrongcode";

        result = await _useCase.Execute(dto);
        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);

        // Teste com código já validado
        tccInvite.Code = dto.InviteCode;
        tccInvite.IsValidCode = true;

        result = await _useCase.Execute(dto);
        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenEmailSendingFails()
    {
        var dto = new NewPasswordDTO("user@example.com", "newpass", "code123");
        var user = new UserEntity { Email = dto.Email, Status = "PENDING" };
        var tccInvite = new TccInviteEntity
        {
            Email = dto.Email,
            Code = dto.InviteCode,
            IsValidCode = false
        };

        _userGateway.FindByEmail(dto.Email).Returns(user);
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(tccInvite);
        _bcryptGateway.GenerateHashPassword(dto.Password).Returns("hashedpass");
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.FailureResult("Falha no envio", 500)));

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(500, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenPasswordIsUpdatedAndEmailSent()
    {
        var dto = new NewPasswordDTO("user@example.com", "newpass", "code123");
        var user = new UserEntity { Email = dto.Email, Status = "PENDING" };
        var tccInvite = new TccInviteEntity
        {
            Email = dto.Email,
            Code = dto.InviteCode,
            IsValidCode = false
        };

        _userGateway.FindByEmail(dto.Email).Returns(user);
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(tccInvite);
        _bcryptGateway.GenerateHashPassword(dto.Password).Returns("hashedpass");
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("hashedpass", user.Password);
        Assert.True(user.Status == "PENDING" || user.Status == "ACTIVE");
        await _userGateway.Received(1).Update(user);
        await _emailGateway.Received(1).Send(Arg.Any<SendEmailDTO>());
    }
}