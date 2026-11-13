using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Dtos.Signature;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gestaotcc.Test.UseCases.Signature;

public class DownloadDocumentUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly FindDocumentUseCase _findDocumentUseCase = Substitute.For<FindDocumentUseCase>(Substitute.For<ITccGateway>(), Substitute.For<IStorageGateway>(), Substitute.For<IUserGateway>(), Substitute.For<IITextGateway>(), Substitute.For<IAppLoggerGateway<FindDocumentUseCase>>());
    private readonly IITextGateway _iTextGateway = Substitute.For<IITextGateway>();
    private readonly IAppLoggerGateway<DownloadDocumentUseCase> _logger = Substitute.For<IAppLoggerGateway<DownloadDocumentUseCase>>();
    private readonly DownloadDocumentUseCase _useCase;

    public DownloadDocumentUseCaseTests()
    {
        _useCase = new DownloadDocumentUseCase(_tccGateway, _findDocumentUseCase, _iTextGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        _tccGateway.FindTccById(Arg.Any<long>()).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1, 10, null, 1);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Contains("Erro ao realizar download", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldDownloadSignedDocument_WhenSignatureExists()
    {
        // Arrange
        var documentId = 10L;
        var signedDocumentName = "documento_final.html";
        var docTypeName = "RelatórioFinal";
        var documentBytes = new byte[] { 1, 2, 3 };

        var document = new DocumentEntity
        {
            Id = documentId,
            FileName = "documento_final",
            DocumentType = new Domain.Entities.DocumentType.DocumentTypeEntity
            {
                Name = docTypeName
            },
            Signatures = new List<SignatureEntity>
            {
                new SignatureEntity { DocumentId = documentId, UserId = 1 }
            }
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Documents = new List<DocumentEntity> { document }
        };

        _tccGateway.FindTccById(tcc.Id).Returns(tcc);
        _findDocumentUseCase.Execute(tcc.Id, documentId, null, 1, true).Returns(ResultPattern<FindDocumentDTO>.SuccessResult(new FindDocumentDTO(System.Convert.ToBase64String(documentBytes))));

        // Act
        var result = await _useCase.Execute(tcc.Id, documentId, null, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("documento_final.pdf", result.Data.FileName);
        Assert.Equal(documentBytes, result.Data.File);
    }

    [Fact]
    public async Task Execute_ShouldDownloadTemplate_WhenNoSignatureExists()
    {
        // Arrange
        var documentId = 20L;
        var docTypeName = "TemplateModelo";
        var documentBytes = new byte[] { 5, 6, 7 };

        var document = new DocumentEntity
        {
            Id = documentId,
            FileName = "template_vazio",
            DocumentType = new Domain.Entities.DocumentType.DocumentTypeEntity
            {
                Name = docTypeName
            },
            Signatures = new List<SignatureEntity>() // nenhuma assinatura
        };

        var tcc = new TccEntity
        {
            Id = 2,
            Documents = new List<DocumentEntity> { document }
        };

        _tccGateway.FindTccById(tcc.Id).Returns(tcc);
        _findDocumentUseCase.Execute(tcc.Id, documentId, null, 1, true).Returns(ResultPattern<FindDocumentDTO>.SuccessResult(new FindDocumentDTO(System.Convert.ToBase64String(documentBytes))));

        // Act
        var result = await _useCase.Execute(tcc.Id, documentId, null, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("TemplateModelo.pdf", result.Data.FileName);
        Assert.Equal(documentBytes, result.Data.File);
    }

    [Fact]
    public async Task Execute_ShouldReturnNotFound_WhenDocumentNotInTcc()
    {
        var tcc = new TccEntity { Id = 1, Documents = new List<DocumentEntity>() };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 99, null, 1);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Documento não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenFindDocumentUseCaseFails()
    {
        var document = new DocumentEntity
        {
            Id = 10,
            FileName = "doc",
            DocumentType = new Domain.Entities.DocumentType.DocumentTypeEntity { Name = "Termo" },
            Signatures = new List<SignatureEntity>()
        };
        var tcc = new TccEntity { Id = 1, Documents = new List<DocumentEntity> { document } };
        _tccGateway.FindTccById(1).Returns(tcc);

        _findDocumentUseCase.Execute(1, 10, null, 1, true)
            .Returns(ResultPattern<FindDocumentDTO>.FailureResult("Falha na geração do documento", 500));

        var result = await _useCase.Execute(1, 10, null, 1);

        Assert.True(result.IsFailure);
        Assert.Equal(500, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_HtmlOverload_ShouldReturnNotFound_WhenTccNotFound()
    {
        _tccGateway.FindTccById(1).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1, 10, "<html></html>");

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Contains("Erro ao realizar download", result.Message);
    }

    [Fact]
    public async Task Execute_HtmlOverload_ShouldReturnNotFound_WhenDocumentNotFound()
    {
        var tcc = new TccEntity { Id = 1, Documents = new List<DocumentEntity>() };
        _tccGateway.FindTccById(1).Returns(tcc);

        var result = await _useCase.Execute(1, 10, "<html></html>");

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Documento não encontrado", result.Message);
    }

    [Fact]
    public async Task Execute_HtmlOverload_ShouldConvertHtmlToPdf_AndReturnSuccess()
    {
        var document = new DocumentEntity
        {
            Id = 10,
            FileName = "anexo_assinado",
            DocumentType = new Domain.Entities.DocumentType.DocumentTypeEntity { Name = "Anexo" },
            Signatures = new List<SignatureEntity> { new() { Id = 1, UserId = 1 } }
        };
        var tcc = new TccEntity { Id = 1, Documents = new List<DocumentEntity> { document } };
        _tccGateway.FindTccById(1).Returns(tcc);

        var pdfBytes = new byte[] { 9, 8, 7 };
        _iTextGateway.ConvertHtmlToPdf("<html>conteudo</html>").Returns(pdfBytes);

        var result = await _useCase.Execute(1, 10, "<html>conteudo</html>");

        Assert.True(result.IsSuccess);
        Assert.Equal("anexo_assinado.pdf", result.Data.FileName);
        Assert.Equal(pdfBytes, result.Data.File);
    }
}