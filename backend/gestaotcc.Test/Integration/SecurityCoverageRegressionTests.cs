using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Application.UseCases.Auth;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.AccessCode;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Exceptions;
using gestaotcc.Infra.Gateways;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class SecurityCoverageRegressionTests
{
    private static UserEntity User() => new()
    {
        Id = 1, Email = "synthetic@example.com", Name = "Synthetic", Status = "ACTIVE", Password = "old-hash",
        Profile = [new() { Role = "STUDENT" }],
        AccessCode = new() { Id = 1, UserId = 1, Code = "ABC234", IsActive = false, ExpirationDate = DateTime.UtcNow.AddMinutes(5) }
    };

    public static TheoryData<string?> InvalidPasswords => new()
    { null, "", "short", new string('a', 73), new string('á', 37) };

    [Theory]
    [MemberData(nameof(InvalidPasswords))]
    public async Task RecoveryRejectsWeakOrOversizedPasswordsBeforePersistence(string? password)
    {
        var user = User(); var users = Substitute.For<IUserGateway>(); users.FindByEmail(user.Email).Returns(user);
        var result = await new UpdatePasswordUseCase(users, Substitute.For<IBcryptGateway>(),
            Substitute.For<IAppLoggerGateway<UpdatePasswordUseCase>>()).Execute(new(user.Email, password!));
        Assert.Equal(400, result.ErrorDetails?.Status);
        await users.DidNotReceive().ResetPassword(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RecoveryWithNoAccessCodeCannotModifyPassword()
    {
        var user = User(); user.AccessCode = null!;
        var users = Substitute.For<IUserGateway>(); users.FindByEmail(user.Email).Returns(user);
        var result = await new UpdatePasswordUseCase(users, Substitute.For<IBcryptGateway>(),
            Substitute.For<IAppLoggerGateway<UpdatePasswordUseCase>>()).Execute(new(user.Email, "Newpass!123"));
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task RecoveryReportsConcurrentConsumptionWithoutUpdatingTheWholeAccount()
    {
        var user = User(); var users = Substitute.For<IUserGateway>(); users.FindByEmail(user.Email).Returns(user);
        var bcrypt = Substitute.For<IBcryptGateway>(); bcrypt.GenerateHashPassword("Newpass!123").Returns("new-hash");
        users.ResetPassword(user.Id, user.Password, user.AccessCode.Id, user.AccessCode.Code, "new-hash").Returns(false);
        var result = await new UpdatePasswordUseCase(users, bcrypt, Substitute.For<IAppLoggerGateway<UpdatePasswordUseCase>>())
            .Execute(new(user.Email, "Newpass!123"));
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("old-hash", user.Password);
        await users.DidNotReceive().Update(Arg.Any<UserEntity>());
    }

    private static (NewPasswordUseCase useCase, IUserGateway users, IEmailGateway emails, TccInviteEntity invite, UserEntity user) FirstAccess()
    {
        var user = User(); user.Status = "PENDING";
        var invite = new TccInviteEntity { Id = 1, Email = user.Email, Code = "INV234", IsValidCode = false, ExpirationDate = DateTime.UtcNow.AddHours(1) };
        var users = Substitute.For<IUserGateway>(); users.FindByEmail(user.Email).Returns(user);
        var tccs = Substitute.For<ITccGateway>(); tccs.FindInviteTccByEmail(user.Email).Returns(invite);
        var emails = Substitute.For<IEmailGateway>();
        return (new(users, Substitute.For<IBcryptGateway>(), tccs, emails, Substitute.For<IAppLoggerGateway<NewPasswordUseCase>>()), users, emails, invite, user);
    }

    [Theory]
    [MemberData(nameof(InvalidPasswords))]
    public async Task FirstAccessRejectsInvalidPasswordsWithoutConsumingInvitation(string? password)
    {
        var data = FirstAccess();
        var result = await data.useCase.Execute(new(data.user.Email, password!, data.invite.Code));
        Assert.Equal(400, result.ErrorDetails?.Status);
        await data.users.DidNotReceive().CompleteFirstAccess(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>());
        await data.emails.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task FirstAccessRejectsExpiredInvitationAndInactiveAccount()
    {
        var data = FirstAccess(); data.invite.ExpirationDate = DateTime.UtcNow.AddSeconds(-1);
        Assert.Equal(409, (await data.useCase.Execute(new(data.user.Email, "Newpass!123", data.invite.Code))).ErrorDetails?.Status);
        data.invite.ExpirationDate = DateTime.UtcNow.AddHours(1); data.user.Status = "INACTIVE";
        Assert.Equal(403, (await data.useCase.Execute(new(data.user.Email, "Newpass!123", data.invite.Code))).ErrorDetails?.Status);
        await data.users.DidNotReceive().CompleteFirstAccess(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task FirstAccessConcurrentFailureDoesNotSendSuccessEmail()
    {
        var data = FirstAccess();
        var result = await data.useCase.Execute(new(data.user.Email, "Newpass!123", data.invite.Code));
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Equal("old-hash", data.user.Password);
        Assert.Equal("PENDING", data.user.Status);
        await data.emails.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task ResendCreatesAnAccessCodeForALegacyAccountWithoutOne()
    {
        var user = User(); user.AccessCode = null!;
        var users = Substitute.For<IUserGateway>(); users.FindByEmail(user.Email).Returns(user);
        var emails = Substitute.For<IEmailGateway>(); emails.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));
        var result = await new ResendAccessCodeUseCase(users, emails, Substitute.For<IAppLoggerGateway<ResendAccessCodeUseCase>>())
            .Execute(new ResendAccessCodeDTO(user.Email), "legacy-alphabet");
        Assert.True(result.IsSuccess); Assert.NotNull(user.AccessCode); Assert.True(user.AccessCode.IsActive);
        Assert.False(user.AccessCode.IsUserUpdatePassword); Assert.Equal(6, user.AccessCode.Code.Length);
        await users.Received(1).Update(user);
    }

    [Fact]
    public async Task FinalUploadRejectsAFileMasqueradingAsPdfWithoutWritingStorage()
    {
        var user = User(); var users = Substitute.For<IUserGateway>(); users.FindById(user.Id).Returns(user);
        var tccs = Substitute.For<ITccGateway>();
        tccs.FindTccById(1).Returns(new TccEntity { Id = 1, Step = "FINALIZATION_AND_PUBLICATION",
            UserTccs = [new() { UserId = user.Id, Profile = new() { Role = "STUDENT" } }] });
        var storage = Substitute.For<IStorageGateway>();
        var result = await new UploadFinalTccFileUseCase(tccs, storage, users).Execute(1, user.Id,
            Encoding.UTF8.GetBytes("<html>not a PDF</html>"), "document.pdf", "application/pdf");
        Assert.Equal(400, result.ErrorDetails?.Status);
        await storage.DidNotReceive().Send(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<bool>());
        await tccs.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PdfConversionRejectsMissingHtml(string? html)
    {
        await Assert.ThrowsAsync<DomainException>(() => new ITextGateway().ConvertHtmlToPdf(html!));
    }

    [Fact]
    public async Task PdfConversionRejectsOversizedHtml()
    {
        await Assert.ThrowsAsync<DomainException>(() => new ITextGateway().ConvertHtmlToPdf(new string('a', 2 * 1024 * 1024 + 1)));
    }
}
