using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.Helpers;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.WebApi.Config;
using gestaotcc.WebApi.Controllers;
using gestaotcc.Domain.Dtos.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class ApplicationSecurityTests
{
    [Fact]
    public async Task InvalidInputFromFluentValidationIsAClientError()
    {
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider() };
        context.Response.Body = new MemoryStream();
        await new GlobalExceptionHandler(_ => throw new FluentValidation.ValidationException("E-mail inválido."),
            Substitute.For<ILogger<GlobalExceptionHandler>>()).InvokeAsync(context);
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task OversizedUploadIsRejectedBeforeReadingFileIntoMemory()
    {
        var context = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("userId", "1")], "test")) };
        var controller = new TccController { ControllerContext = new ControllerContext { HttpContext = context } };
        var file = Substitute.For<IFormFile>(); file.Length.Returns((long)PdfUploadPolicy.MaxBytes + 1);
        var result = await controller.UploadFinalTccFile(1, file, null!);
        Assert.Equal(413, Assert.IsType<ObjectResult>(result.Result).StatusCode);
        await file.DidNotReceive().CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkedStudentCanStillUploadAValidPdf()
    {
        var user = Account(); var users = Substitute.For<IUserGateway>(); users.FindById(user.Id).Returns(user);
        var tccs = Substitute.For<ITccGateway>();
        var tcc = new TccEntity { Id = 2, UserTccs = [new UserTccEntity
            { UserId = user.Id, Profile = new ProfileEntity { Role = "STUDENT" } }] };
        tccs.FindTccById(tcc.Id).Returns(tcc);
        var storage = Substitute.For<IStorageGateway>();
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF");
        var result = await new UploadTccFileUseCase(tccs, storage, users).Execute(tcc.Id, user.Id, pdf, "tcc.pdf", "application/pdf");
        Assert.True(result.IsSuccess);
        await storage.Received(1).Send(Arg.Any<string>(), pdf, "application/pdf", true);
        await tccs.Received(1).Update(tcc);
    }

    private static UserEntity Account(long id = 1) => new()
    {
        Id = id, Email = $"user{id}@example.com", Status = "ACTIVE", Password = "previous-hash",
        Profile = [new ProfileEntity { Role = "STUDENT" }],
        AccessCode = new() { Id = id, Code = "secret-code", IsActive = false,
            IsUserUpdatePassword = false, ExpirationDate = DateTime.UtcNow.AddMinutes(5) }
    };

    private static (PasswordResetSession session, IUserGateway users) ResetSession(UserEntity user)
    {
        var users = Substitute.For<IUserGateway>();
        users.FindByEmail(user.Email).Returns(user);
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName = "Production";
        return (new(new EphemeralDataProtectionProvider(), users, environment), users);
    }

    private static async Task<DefaultHttpContext> Issue(PasswordResetSession session, string email)
    {
        var context = new DefaultHttpContext();
        await session.Issue(context, email);
        return context;
    }

    private static DefaultHttpContext Present(DefaultHttpContext issued)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = issued.Response.Headers.SetCookie.ToString().Split(';')[0];
        return context;
    }

    [Fact]
    public async Task ResetProofIsHttpOnlySecureStrictAndCannotBeForgedOrUsedForAnotherAccount()
    {
        var user = Account();
        var (session, users) = ResetSession(user);
        var issued = await Issue(session, user.Email);
        var cookie = issued.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        Assert.Contains("httponly", cookie); Assert.Contains("secure", cookie); Assert.Contains("samesite=strict", cookie);
        Assert.True(await session.Validate(Present(issued), user.Email));
        Assert.False(await session.Validate(new DefaultHttpContext(), user.Email));
        var forged = new DefaultHttpContext(); forged.Request.Headers.Cookie = PasswordResetSession.CookieName + "=verified_user";
        Assert.False(await session.Validate(forged, user.Email));
        var other = Account(2); users.FindByEmail(other.Email).Returns(other);
        Assert.False(await session.Validate(Present(issued), other.Email));
    }

    [Fact]
    public async Task ResetProofStopsWorkingAfterResendPasswordChangeExpiryOrConsumption()
    {
        var user = Account(); var (session, _) = ResetSession(user);
        var issued = await Issue(session, user.Email);
        user.AccessCode.Code = "replacement";
        Assert.False(await session.Validate(Present(issued), user.Email));
        user.AccessCode.Code = "secret-code"; user.Password = "new-hash";
        Assert.False(await session.Validate(Present(issued), user.Email));
        user.Password = "previous-hash"; user.AccessCode.IsUserUpdatePassword = true;
        Assert.False(await session.Validate(Present(issued), user.Email));
        user.AccessCode.IsUserUpdatePassword = false; user.AccessCode.ExpirationDate = DateTime.UtcNow.AddSeconds(-1);
        Assert.False(await session.Validate(Present(issued), user.Email));
    }

    [Fact]
    public async Task KnowingEmailAndVerifiedDatabaseStateDoesNotAuthorizePasswordReset()
    {
        var user = Account(); var (session, _) = ResetSession(user);
        var controller = new AuthController(Substitute.For<ILogger<AuthController>>())
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var response = await controller.Update(new UpdatePasswordDTO(user.Email, "Newpass!1"), null!, session);
        Assert.IsType<UnauthorizedObjectResult>(response.Result);
    }

    [Fact]
    public async Task InternalExceptionsNeverExposeInfrastructureMessages()
    {
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider() };
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionHandler(_ => throw new InvalidOperationException("Password=secret; SQL internal-detail"),
            Substitute.For<ILogger<GlobalExceptionHandler>>());
        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(500, context.Response.StatusCode);
        Assert.DoesNotContain("secret", body); Assert.DoesNotContain("InvalidOperationException", body);
        Assert.Contains("traceId", body);
    }

    [Fact]
    public void PdfPolicyRejectsRenamedHtmlWrongMimeAndTruncatedFiles()
    {
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF");
        Assert.True(PdfUploadPolicy.IsValid(pdf, "tcc.pdf", "application/pdf"));
        Assert.False(PdfUploadPolicy.IsValid(Encoding.ASCII.GetBytes("<html>fake</html>"), "tcc.pdf", "application/pdf"));
        Assert.False(PdfUploadPolicy.IsValid(pdf, "tcc.html", "application/pdf"));
        Assert.False(PdfUploadPolicy.IsValid(pdf, "tcc.pdf", "text/html"));
        Assert.False(PdfUploadPolicy.IsValid(Encoding.ASCII.GetBytes("%PDF-1.7"), "tcc.pdf", "application/pdf"));
    }

    [Fact]
    public async Task StudentCannotOverwriteAnotherStudentsTcc()
    {
        var user = Account();
        var users = Substitute.For<IUserGateway>(); users.FindById(user.Id).Returns(user);
        var tccs = Substitute.For<ITccGateway>();
        tccs.FindTccById(2).Returns(new TccEntity { Id = 2, UserTccs = [new UserTccEntity
            { UserId = 2, Profile = new ProfileEntity { Role = "STUDENT" } }] });
        var storage = Substitute.For<IStorageGateway>();
        var result = await new UploadTccFileUseCase(tccs, storage, users).Execute(2, user.Id,
            Encoding.ASCII.GetBytes("%PDF-1.7\n%%EOF"), "tcc.pdf", "application/pdf");
        Assert.Equal(403, result.ErrorDetails?.Status);
        await storage.DidNotReceive().Send(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<bool>());
        await tccs.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Fact]
    public void CoordinatorCannotWriteOutsideTheirCourseButAdminCan()
    {
        var manager = Account(); manager.CampiCourseId = 1;
        manager.Profile = [new ProfileEntity { Role = "COORDINATOR" }];
        var tcc = new TccEntity { UserTccs = [new UserTccEntity { UserId = 2,
            User = new UserEntity { CampiCourseId = 2 }, Profile = new ProfileEntity { Role = "STUDENT" } }] };
        Assert.False(TccUploadAuthorization.Allows(manager, tcc));
        manager.CampiCourseId = 2; Assert.True(TccUploadAuthorization.Allows(manager, tcc));
        manager.Profile = [new ProfileEntity { Role = "ADMIN" }]; manager.CampiCourseId = null;
        Assert.True(TccUploadAuthorization.Allows(manager, tcc));
    }
}
