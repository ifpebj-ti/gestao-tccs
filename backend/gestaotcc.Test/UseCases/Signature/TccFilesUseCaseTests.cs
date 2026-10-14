using System.IO.Compression;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class TccFilesUseCaseTests
{
    private readonly ITccGateway _tccs = Substitute.For<ITccGateway>();
    private readonly IUserGateway _users = Substitute.For<IUserGateway>();
    private readonly IStorageGateway _storage = Substitute.For<IStorageGateway>();
    private TccFilesUseCase Create() => new(_tccs, _users, _storage);
    private static UserEntity User(string role, long course = 7) => new() { Id = 21, Status = "ACTIVE", CampiCourseId = course, Profile = [new() { Role = role }] };
    private static TccEntity Tcc(string role = "STUDENT", long userId = 21) => new()
    {
        Id = 42, Title = "Título compartilhado",
        UserTccs = [new() { UserId = userId, User = new() { Id = userId, Name = "Estudante", CampiCourseId = 7 }, Profile = new() { Role = role } }],
        Documents = [Document(10, true), Document(11, false)]
    };
    private static DocumentEntity Document(long id, bool signed) => new()
    {
        Id = id, TccId = 42, FileName = $"chave-{id}", DocumentType = new() { Name = $"Anexo {id}" },
        Signatures = signed ? [new() { UserId = 21, SignatureDate = new DateTime(2026, 10, 13) }] : []
    };

    [Theory]
    [InlineData("STUDENT")]
    [InlineData("ADVISOR")]
    [InlineData("BANKING")]
    public async Task LinkedActiveUserCanListOnlySignedDocuments(string role)
    {
        _users.FindById(21).Returns(User(role)); _tccs.FindTccById(42).Returns(Tcc(role));
        var result = await Create().List(42, 21);
        Assert.True(result.IsSuccess); Assert.Equal(10, Assert.Single(result.Data).Id);
        Assert.Equal(1, result.Data[0].SignatureCount);
        await _storage.DidNotReceiveWithAnyArgs().Download(default!, default);
    }

    [Theory]
    [InlineData("COORDINATOR", 7, true)]
    [InlineData("COORDINATOR", 8, false)]
    [InlineData("SUPERVISOR", 7, true)]
    [InlineData("LIBRARY", 7, true)]
    [InlineData("ADVISOR", 7, false)]
    [InlineData("STUDENT", 7, false)]
    [InlineData("BANKING", 7, false)]
    [InlineData("ADMIN", 8, true)]
    public async Task ManagementScopeDoesNotGrantOtherUsersAccess(string role, long course, bool allowed)
    {
        _users.FindById(21).Returns(User(role, course)); _tccs.FindTccById(42).Returns(Tcc(userId: 22));
        Assert.Equal(allowed, (await Create().List(42, 21)).IsSuccess);
    }

    [Fact]
    public async Task InactiveUserAndRemovedRoleCannotUseOldLink()
    {
        var user = User("STUDENT"); user.Status = "INACTIVE";
        _users.FindById(21).Returns(user); _tccs.FindTccById(42).Returns(Tcc());
        Assert.Null(await Create().FindAccessible(42, 21));
        user.Status = "ACTIVE"; user.Profile = [new() { Role = "ADVISOR" }];
        Assert.Null(await Create().FindAccessible(42, 21));
    }

    [Theory]
    [InlineData(43, 10)]
    [InlineData(42, 11)]
    [InlineData(42, 99)]
    public async Task AnotherTccUnsignedAndMissingDocumentsNeverReachStorage(long tccId, long documentId)
    {
        _users.FindById(21).Returns(User("STUDENT")); _tccs.FindTccById(42).Returns(Tcc());
        var result = await Create().Download(tccId, 21, documentId);
        Assert.Equal(404, result.ErrorDetails?.Status);
        await _storage.DidNotReceiveWithAnyArgs().Download(default!, default);
    }

    [Fact]
    public async Task ArchiveUsesExactDatabaseKeysAndExcludesUnsignedFiles()
    {
        _users.FindById(21).Returns(User("STUDENT")); _tccs.FindTccById(42).Returns(Tcc());
        _storage.Download("chave-10.pdf", true).Returns(new byte[] { 1, 2, 3 });
        var result = await Create().DownloadAll(42, 21);
        Assert.True(result.IsSuccess);
        using var zip = new ZipArchive(new MemoryStream(result.Data.File));
        Assert.Equal("Documento_10.pdf", Assert.Single(zip.Entries).FullName);
        await _storage.DidNotReceiveWithAnyArgs().DownloadFolderAsZip(default!);
        await _storage.DidNotReceive().Download("chave-11.pdf", true);
    }

    [Fact]
    public async Task HtmlFallbackReturnsStoredArtifactWithoutGeneratingTemplate()
    {
        _users.FindById(21).Returns(User("STUDENT")); _tccs.FindTccById(42).Returns(Tcc());
        _storage.Download("chave-10.pdf", true).Returns(Array.Empty<byte>());
        _storage.Download("chave-10.html", true).Returns(new byte[] { 60, 104 });
        var result = await Create().Download(42, 21, 10);
        Assert.True(result.IsSuccess); Assert.EndsWith(".html", result.Data.FileName);
    }

    [Fact]
    public async Task MissingStorageArtifactIsAnErrorInsteadOfEmptySuccess()
    {
        _users.FindById(21).Returns(User("STUDENT")); _tccs.FindTccById(42).Returns(Tcc());
        _storage.Download(Arg.Any<string>(), true).Returns(Array.Empty<byte>());
        Assert.Equal(404, (await Create().DownloadAll(42, 21)).ErrorDetails?.Status);
    }

    [Theory]
    [InlineData(43, 10, "0")]
    [InlineData(42, 99, "0")]
    [InlineData(42, 10, "99")]
    public async Task LegacyDocumentRoutesRejectDifferentTccDocumentOrStudent(long tccId, long documentId, string studentId)
    {
        _users.FindById(21).Returns(User("STUDENT")); _tccs.FindTccById(42).Returns(Tcc());
        var controller = new gestaotcc.WebApi.Controllers.SignatureController(Create());
        controller.ControllerContext = new() { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };
        controller.User.AddIdentity(new System.Security.Claims.ClaimsIdentity([new("userId", "21")], "Test"));
        Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundResult>(await controller.DownloaDocument(tccId, documentId, studentId, null!));
        Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundResult>(await controller.DownloadDocumentHtml(new(tccId, documentId, studentId, "teste"), null!));
        Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundResult>((await controller.FindDocument(tccId, documentId, long.Parse(studentId), null!)).Result);
        await _storage.DidNotReceiveWithAnyArgs().Download(default!, default);
    }
}
