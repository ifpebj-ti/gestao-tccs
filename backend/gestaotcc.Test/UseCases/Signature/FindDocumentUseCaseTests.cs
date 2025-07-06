using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class FindDocumentUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IMinioGateway _minioGateway = Substitute.For<IMinioGateway>();
    private readonly FindDocumentUseCase _useCase;

    public FindDocumentUseCaseTests()
    {
        _useCase = new FindDocumentUseCase(_tccGateway, _minioGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        _tccGateway.FindTccById(Arg.Any<long>()).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1, 1);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Contains("Erro ao realizar download", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnSignedDocumentUrl_WhenDocumentIsSigned()
    {
        var documentType = new DocumentTypeEntity { Id = 1, Name = "Proposal" };

        var signature = new SignatureEntity { DocumentId = 1, UserId = 100 };

        var document = new DocumentEntity
        {
            Id = 1,
            DocumentType = documentType,
            DocumentTypeId = documentType.Id,
            FileName = "signed-doc",
            Signatures = new List<SignatureEntity> { signature }
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Documents = new List<DocumentEntity> { document }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _minioGateway.GetPresignedUrl("signed-doc.pdf", true).Returns("https://signed-doc-url");

        var result = await _useCase.Execute(1, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://signed-doc-url", result.Data.Url);
    }

    [Fact]
    public async Task Execute_ShouldReturnTemplateDocumentUrl_WhenDocumentIsNotSigned()
    {
        var documentType = new DocumentTypeEntity { Id = 1, Name = "Proposal" };

        var document = new DocumentEntity
        {
            Id = 1,
            DocumentType = documentType,
            DocumentTypeId = documentType.Id,
            FileName = "original-doc",
            Signatures = new List<SignatureEntity>() // vazio = não assinado
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Documents = new List<DocumentEntity> { document }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _minioGateway.GetPresignedUrl("Proposal.pdf", false).Returns("https://template-doc-url");

        var result = await _useCase.Execute(1, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://template-doc-url", result.Data.Url);
    }
}