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
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class SignSignatureUseCaseTests
{
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IMinioGateway _minioGateway = Substitute.For<IMinioGateway>();
    private readonly SignSignatureUseCase _useCase;
    private readonly IAppLoggerGateway<SignSignatureUseCase> _logger = Substitute.For<IAppLoggerGateway<SignSignatureUseCase>>();

    public SignSignatureUseCaseTests()
    {
        _useCase = new SignSignatureUseCase(_documentTypeGateway, _tccGateway, _minioGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnInvalid_WhenFileIsInvalid()
    {
        var dto = new SignSignatureDTO(1, 1, 1, new byte[0], 6, "application/pdf");

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnInvalid_WhenUserAlreadySigned()
    {
        var user = new UserEntity { Id = 1, Name = "User" };
        var signature = new SignatureEntity { UserId = 1, DocumentId = 1, User = user };

        var document = new DocumentEntity
        {
            Id = 1,
            DocumentType = new DocumentTypeEntity
            {
                Id = 1,
                Name = "Doc",
                SignatureOrder = 3,
                MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
                Profiles = new List<ProfileEntity>()
            },
            Signatures = new List<SignatureEntity> { signature }
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            UserTccs = new List<UserTccEntity>(),
            Documents = new List<DocumentEntity> { document }
        };

        var profile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        
        var userTcc = new UserTccEntity()
        {
            BindingDate = DateTime.UtcNow,
            Id = 1,
            Profile = profile,
            ProfileId = profile.Id,
            Tcc = null,
            TccId = 1,
            User = user,
            UserId = user.Id
        };
        tcc.UserTccs.Add(userTcc);

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { document.DocumentType });

        var dto = new SignSignatureDTO(1, 1, 1, new byte[] { 1 }, 1, "application/pdf");
        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails.Status);
    }

    [Fact]
    public async Task Execute_ShouldAddSignature_AndMoveStep_WhenAllDocumentsAreSigned()
    {
        var user = new UserEntity { Id = 1, Name = "User", Email = "email@email.com" };
        var profile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var docType = new DocumentTypeEntity
        {
            Id = 1,
            Name = "Doc",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { profile }
        };

        var document = new DocumentEntity
        {
            Id = 1,
            DocumentTypeId = 1,
            DocumentType = docType,
            FileName = "doc",
            Signatures = new List<SignatureEntity>(),
            User = user,
            UserId = user.Id
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Documents = new List<DocumentEntity> { document },
            UserTccs = new List<UserTccEntity>()
        };
        var userTcc = new UserTccEntity()
        {
            BindingDate = DateTime.UtcNow,
            Id = 1,
            Profile = profile,
            ProfileId = profile.Id,
            Tcc = null,
            TccId = 1,
            User = user,
            UserId = user.Id
        };
        
        tcc.UserTccs.Add(userTcc);
        
        tcc.UserTccs.First().Tcc = tcc;

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _minioGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var dto = new SignSignatureDTO(1, 1, 1, new byte[] { 1, 2, 3 }, 1, "application/pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);

        await _minioGateway.Received(1).Send(document.FileName, dto.File, dto.FileContentType);
    }
}