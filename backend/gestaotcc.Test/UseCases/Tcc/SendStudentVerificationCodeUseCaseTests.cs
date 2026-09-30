using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class SendStudentVerificationCodeUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<SendStudentVerificationCodeUseCase> _logger = Substitute.For<IAppLoggerGateway<SendStudentVerificationCodeUseCase>>();
    private readonly SendStudentVerificationCodeUseCase _useCase;

    public SendStudentVerificationCodeUseCaseTests()
    {
        _useCase = new SendStudentVerificationCodeUseCase(_userGateway, _tccGateway, _emailGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturn409_WhenUserAlreadyExists()
    {
        // Arrange
        var dto = new SendStudentVerificationCodeDTO("aluno@discente.ifpe.edu.br");
        _userGateway.FindByEmail(dto.UserEmail).Returns(new UserEntity());

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails.Status);
        Assert.Contains("já possui cadastro", result.Message);
        await _emailGateway.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task Execute_ShouldUpdateExistingInviteAndSendEmail_WhenInviteAlreadyExists()
    {
        // Arrange
        var dto = new SendStudentVerificationCodeDTO("novoaluno@discente.ifpe.edu.br");
        _userGateway.FindByEmail(dto.UserEmail).Returns((UserEntity?)null);

        var existingInvite = new TccInviteEntity
        {
            Id = 1,
            Email = dto.UserEmail,
            Code = "OLD123",
            IsValidCode = false
        };

        _tccGateway.FindInviteTccByEmail(dto.UserEmail).Returns(existingInvite);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(existingInvite.IsValidCode);
        Assert.Equal(6, existingInvite.Code.Length);
        Assert.True(existingInvite.ExpirationDate > DateTime.UtcNow);
        await _tccGateway.Received(1).UpdateTccInvite(existingInvite);
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(e => e.Recipient == dto.UserEmail && e.TypeTemplate == "INVITE-USER"));
    }

    [Fact]
    public async Task Execute_ShouldCreateNewInviteAndSendEmail_WhenNoInviteExists()
    {
        // Arrange
        var dto = new SendStudentVerificationCodeDTO("primeiroaluno@discente.ifpe.edu.br");
        _userGateway.FindByEmail(dto.UserEmail).Returns((UserEntity?)null);
        _tccGateway.FindInviteTccByEmail(dto.UserEmail).Returns((TccInviteEntity?)null);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).SaveTccInvite(Arg.Is<TccInviteEntity>(invite =>
            invite.Email == dto.UserEmail &&
            invite.IsValidCode &&
            invite.Code.Length == 6 &&
            invite.ExpirationDate > DateTime.UtcNow));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(e => e.Recipient == dto.UserEmail && e.TypeTemplate == "INVITE-USER"));
    }
}
