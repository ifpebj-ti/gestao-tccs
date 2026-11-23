using System.Security.Claims;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Errors;
using gestaotcc.WebApi.Controllers;
using gestaotcc.WebApi.InputModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace gestaotcc.Test.Integration;

public class SignatureControllerTests
{
    private const long TccId = 12;
    private const long DocumentId = 34;
    private const long StudentId = 56;
    private const int CampusId = 78;
    private const string Html = "<p>documento de teste</p>";
    private static readonly byte[] Pdf = "%PDF-1.7 test"u8.ToArray();

    private readonly ITccGateway _tccs = Substitute.For<ITccGateway>();
    private readonly IUserGateway _users = Substitute.For<IUserGateway>();
    private readonly IStorageGateway _storage = Substitute.For<IStorageGateway>();
    private readonly IITextGateway _pdf = Substitute.For<IITextGateway>();

    [Theory]
    [InlineData(null, null, null, 0L)]
    [InlineData("null", "", null, 0L)]
    [InlineData("0", "78", 0L, 78L)]
    [InlineData("56", "78", 56L, 78L)]
    public async Task Download_ReturnsSignedFileAndNormalizesLegacyQueryParameters(
        string? student, string? campus, long? expectedStudent, long expectedCampus)
    {
        var controller = Controller(campus: campus);
        var find = FindDocument();
        find.Execute(TccId, DocumentId, expectedStudent, expectedCampus, true)
            .Returns(ResultPattern<FindDocumentDTO>.SuccessResult(new(Convert.ToBase64String(Pdf), false)));

        var response = await controller.DownloaDocument(TccId, DocumentId, student, DownloadDocument(find));

        var file = Assert.IsType<FileContentResult>(response);
        Assert.Equal(Pdf, file.FileContents);
        Assert.Equal("assinatura.pdf", file.FileDownloadName);
        Assert.Equal("application/octet-stream", file.ContentType);
        await find.Received(1).Execute(TccId, DocumentId, expectedStudent, expectedCampus, true);
    }

    [Theory]
    [InlineData(409, typeof(ConflictObjectResult))]
    [InlineData(404, typeof(NotFoundObjectResult))]
    public async Task Download_AndPreview_ExposeDomainFailureAtTheRequestedEndpoint(int status, Type resultType)
    {
        var controller = Controller();
        var find = FindDocument();
        find.Execute(TccId, DocumentId, Arg.Any<long?>(), CampusId, true)
            .Returns(ResultPattern<FindDocumentDTO>.FailureResult("Documento indisponivel", status));

        var download = await controller.DownloaDocument(TccId, DocumentId, "56", DownloadDocument(find));
        var preview = await controller.FindDocument(TccId, DocumentId, StudentId, find);

        foreach (var response in new IActionResult?[] { download, preview.Result })
        {
            Assert.IsType(resultType, response);
            var error = Assert.IsAssignableFrom<ObjectResult>(response);
            Assert.Equal(status, error.StatusCode);
            Assert.Equal("https://gradus.example.org/api/Signature/document", Assert.IsType<FormatDetails>(error.Value).Type);
        }
    }

    [Theory]
    [InlineData(null, 56L, 34L, null, true)]
    [InlineData("invalid", 56L, 34L, null, true)]
    [InlineData("56", 56L, 99L, null, true)]
    [InlineData("56", 99L, 34L, "99", true)]
    [InlineData("56", 56L, 34L, "invalid", true)]
    [InlineData("56", 56L, 34L, null, false)]
    public async Task Download_DeniesAccessBeforeReadingTheFile(
        string? requester, long studentInRequest, long documentInRequest, string? student, bool existingTcc)
    {
        var controller = Controller(requester, existingTcc: existingTcc);

        var file = await controller.DownloaDocument(TccId, documentInRequest, student, null!);
        Assert.IsType<NotFoundResult>(file);
        var html = await controller.DownloadDocumentHtml(new(TccId, documentInRequest, student, Html), null!);
        Assert.IsType<NotFoundResult>(html);
        if (student is not "invalid")
        {
            var preview = await controller.FindDocument(TccId, documentInRequest, studentInRequest, null!);
            Assert.IsType<NotFoundResult>(preview.Result);
        }
        await _storage.DidNotReceive().Download(Arg.Any<string>(), Arg.Any<bool>());
    }

    [Theory]
    [InlineData(null, 0L)]
    [InlineData("78", 78L)]
    public async Task Preview_ReturnsTheDocumentWithOrWithoutCampusClaim(string? campus, long expectedCampus)
    {
        var controller = Controller(campus: campus);
        var find = FindDocument();
        var document = new FindDocumentDTO(Html, true);
        find.Execute(TccId, DocumentId, StudentId, expectedCampus, true)
            .Returns(ResultPattern<FindDocumentDTO>.SuccessResult(document));

        var response = await controller.FindDocument(TccId, DocumentId, StudentId, find);

        Assert.Same(document, Assert.IsType<OkObjectResult>(response.Result).Value);
    }

    [Fact]
    public async Task DownloadHtml_RequiresCampusAndReturnsRenderedPdfForAnAccessibleDocument()
    {
        var input = new DownloadDocumentHtmlInputModel(TccId, DocumentId, "56", Html);
        _pdf.ConvertHtmlToPdf(Html).Returns(Pdf);

        var unauthorized = await Controller(campus: null).DownloadDocumentHtml(input, null!);
        Assert.IsType<UnauthorizedResult>(unauthorized);
        await _pdf.DidNotReceive().ConvertHtmlToPdf(Arg.Any<string>());
        var response = await Controller().DownloadDocumentHtml(input, DownloadDocument(FindDocument()));

        var file = Assert.IsType<FileContentResult>(response);
        Assert.Equal(Pdf, file.FileContents);
        Assert.Equal("assinatura.pdf", file.FileDownloadName);
        await _pdf.Received(1).ConvertHtmlToPdf(Html);
    }

    [Fact]
    public async Task DownloadHtml_ReturnsNotFoundWhenDocumentDisappearsBeforeRendering()
    {
        var controller = Controller();
        _tccs.FindTccById(TccId).Returns(Tcc(), (TccEntity?)null);

        var response = await controller.DownloadDocumentHtml(new(TccId, DocumentId, "", Html), DownloadDocument(FindDocument()));

        Assert.IsType<NotFoundObjectResult>(response);
        await _pdf.DidNotReceive().ConvertHtmlToPdf(Arg.Any<string>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("999")]
    public async Task Sign_RejectsRequesterMismatchBeforeReadingUpload(string? requester)
    {
        var controller = Controller(requester);
        var upload = Substitute.For<IFormFile>();

        var response = await controller.SignSignature(new(TccId, DocumentId, StudentId, upload), null!);

        Assert.IsType<ForbidResult>(response.Result);
        await upload.DidNotReceive().CopyToAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sign_ReturnsConflictForAnUnsupportedUpload()
    {
        var controller = Controller();
        using var stream = new MemoryStream("plain text"u8.ToArray());
        var file = new FormFile(stream, 0, stream.Length, "file", "document.txt") { Headers = new HeaderDictionary(), ContentType = "text/plain" };
        var sign = new SignSignatureUseCase(Substitute.For<IDocumentTypeGateway>(), _tccs, _storage,
            Substitute.For<IEmailGateway>(), _users, Substitute.For<IAppLoggerGateway<SignSignatureUseCase>>());

        var response = await controller.SignSignature(new(TccId, DocumentId, StudentId, file), sign);

        Assert.IsType<ConflictObjectResult>(response.Result);
        await _storage.DidNotReceive().Send(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    private SignatureController Controller(string? requester = "56", string? campus = "78", bool existingTcc = true)
    {
        var claims = new List<Claim>();
        if (requester is not null) claims.Add(new Claim("userId", requester));
        if (campus is not null) claims.Add(new Claim("campiCourseId", campus));
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test")) };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("gradus.example.org");
        context.Request.Path = "/api/Signature/document";
        _users.FindById(StudentId).Returns(new UserEntity
        {
            Id = StudentId, Status = "ACTIVE", Profile = [new ProfileEntity { Role = "STUDENT" }]
        });
        if (existingTcc) _tccs.FindTccById(TccId).Returns(Tcc());

        return new SignatureController(new TccFilesUseCase(_tccs, _users, _storage))
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static TccEntity Tcc() => new()
    {
        Id = TccId,
        UserTccs = [new UserTccEntity { UserId = StudentId, Profile = new ProfileEntity { Role = "STUDENT" }, User = new UserEntity() }],
        Documents = [new DocumentEntity { Id = DocumentId, FileName = "assinatura",
            DocumentType = new DocumentTypeEntity { Name = "Anexo" }, Signatures = [new SignatureEntity { UserId = StudentId }] }]
    };

    private FindDocumentUseCase FindDocument() =>
        Substitute.For<FindDocumentUseCase>(_tccs, _storage, _users, _pdf, Substitute.For<IAppLoggerGateway<FindDocumentUseCase>>());

    private DownloadDocumentUseCase DownloadDocument(FindDocumentUseCase find) =>
        new(_tccs, find, _pdf, Substitute.For<IAppLoggerGateway<DownloadDocumentUseCase>>());
}
