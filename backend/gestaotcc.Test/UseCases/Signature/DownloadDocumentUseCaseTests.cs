using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class DownloadDocumentUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IMinioGateway _minioGateway = Substitute.For<IMinioGateway>();
    private readonly DownloadDocumentUseCase _useCase;
    private readonly IAppLoggerGateway<DownloadDocumentUseCase> _logger = Substitute.For<IAppLoggerGateway<DownloadDocumentUseCase>>();

    public DownloadDocumentUseCaseTests()
    {
        _useCase = new DownloadDocumentUseCase(_tccGateway, _minioGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        _tccGateway.FindTccById(Arg.Any<long>()).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1, 10);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Contains("Erro ao realizar download", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldDownloadSignedDocument_WhenSignatureExists()
    {
        // Arrange
        var documentId = 10L;
        var signedDocumentName = "documento_final.pdf";
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
        _minioGateway.Download("documento_final.pdf", true).Returns(documentBytes);

        // Act
        var result = await _useCase.Execute(tcc.Id, documentId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("RelatórioFinal.pdf", result.Data.FileName);
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
        _minioGateway.Download("TemplateModelo.pdf", false).Returns(documentBytes);

        // Act
        var result = await _useCase.Execute(tcc.Id, documentId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("TemplateModelo.pdf", result.Data.FileName);
        Assert.Equal(documentBytes, result.Data.File);
    }
}