using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class FindDocumentUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IMinioGateway _minioGateway = Substitute.For<IMinioGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly FindDocumentUseCase _useCase;
    private readonly IAppLoggerGateway<FindDocumentUseCase> _logger = Substitute.For<IAppLoggerGateway<FindDocumentUseCase>>();

    public FindDocumentUseCaseTests()
    {
        _useCase = new FindDocumentUseCase(_tccGateway, _minioGateway, _userGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        _tccGateway.FindTccById(Arg.Any<long>()).Returns((TccEntity?)null);

        var result = await _useCase.Execute(1, 1, 1, 1);

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
        _minioGateway.GetDocumentAsBase64("signed-doc.pdf", Arg.Any<Dictionary<string, string>>(), true).Returns("https://signed-doc-url");

        var result = await _useCase.Execute(1, 1, 1, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://signed-doc-url", result.Data.Url);
    }

    [Fact]
    public async Task Execute_ShouldReturnTemplateDocumentUrl_WhenDocumentIsNotSigned()
    {
        var documentType = new DocumentTypeEntity { Id = 1, Name = "Proposal" };

        var profile = new ProfileEntity
        {
            Id = 1,
            Role = RoleType.ADVISOR.ToString()
        };
        
        var supervisorUser = new UserEntity
        {
            Id = 1,
            Profile = new List<ProfileEntity>() { profile }
        };

        var document = new DocumentEntity
        {
            Id = 1,
            DocumentType = documentType,
            DocumentTypeId = documentType.Id,
            FileName = "original-doc",
            Signatures = new List<SignatureEntity>() // vazio = não assinado
        };

        var profileStudent = new ProfileEntity
        {
            Id = 1,
            Role = RoleType.STUDENT.ToString()
        };

        var userStudent = new UserEntity
        {
            Id = 1,
            Profile = new List<ProfileEntity> { profileStudent }
        };

        var userTcc = new UserTccEntity
        {
            Id = 1,
            User = userStudent,
            Profile = profileStudent
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Documents = new List<DocumentEntity> { document },
            UserTccs = new List<UserTccEntity> { userTcc }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _userGateway.FindAllByFilter(Arg.Is<UserFilterDTO>(u => u.Profile == RoleType.ADVISOR.ToString()), Arg.Any<long>())
            .Returns(new List<UserEntity> { supervisorUser });
        _minioGateway.GetDocumentAsBase64("Proposal.pdf", Arg.Any<System.Collections.Generic.Dictionary<string, string>>(), false)
            .Returns("https://template-doc-url");

        var result = await _useCase.Execute(1, 1, 1, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://template-doc-url", result.Data.Url);
    }
}