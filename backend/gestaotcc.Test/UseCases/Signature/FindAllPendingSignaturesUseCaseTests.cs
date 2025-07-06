using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class FindAllPendingSignaturesUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly FindAllPendingSignaturesUseCase _useCase;

    public FindAllPendingSignaturesUseCaseTests()
    {
        _useCase = new FindAllPendingSignaturesUseCase(_tccGateway, _documentTypeGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnEmptyList_WhenNoTccsInProgress()
    {
        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>())
            .Returns(Task.FromResult(new List<TccEntity>()));

        var result = await _useCase.Execute(null);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Execute_ShouldSkipTccWithInvalidStep()
    {
        var tccWithInvalidStep = new TccEntity
        {
            Id = 1,
            Step = "InvalidStep",
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity>(),
            Documents = new List<DocumentEntity>(),
            TccInvites = new List<TccInviteEntity>()
        };

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>())
            .Returns(new List<TccEntity> { tccWithInvalidStep });

        _documentTypeGateway.FindAll()
            .Returns(new List<DocumentTypeEntity>());

        var result = await _useCase.Execute(null);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }

    [Fact]
public async Task Execute_ShouldReturnPendingSignatures_WhenThereAreDocumentsToSign()
{
    // Perfis
    var profileAdvisor = new Domain.Entities.Profile.ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
    var profileStudent = new Domain.Entities.Profile.ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

    // Usuários
    var userAdvisor = new UserEntity { Id = 100, Name = "Advisor User" };
    var userStudent = new UserEntity { Id = 200, Name = "Student User" };

    // TCC
    var tcc = new TccEntity
    {
        Id = 1,
        Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
        Status = StatusTccType.IN_PROGRESS.ToString(),
        UserTccs = new List<UserTccEntity>(),
        TccInvites = new List<TccInviteEntity>(),
        Documents = new List<DocumentEntity>()
    };

    // UserTccs com referência ao TCC
    var userTccAdvisor = new UserTccEntity(userAdvisor, tcc, profileAdvisor, DateTime.UtcNow);
    var userTccStudent = new UserTccEntity(userStudent, tcc, profileStudent, DateTime.UtcNow);
    tcc.UserTccs.Add(userTccAdvisor);
    tcc.UserTccs.Add(userTccStudent);

    // DocumentType compatível com o passo do TCC
    var docType = new DocumentTypeEntity
    {
        Id = 10,
        Name = "DocType 1",
        SignatureOrder = 3, // Compatível com StepTccType.DEVELOPMENT_AND_MONITORING
        MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
        Profiles = new List<Domain.Entities.Profile.ProfileEntity> { profileAdvisor, profileStudent }
    };

    // Documento compartilhado (sem UserId), sem assinaturas
    var document = new DocumentEntity
    {
        Id = 101,
        DocumentType = docType,
        DocumentTypeId = docType.Id,
        FileName = "filename",
        Signatures = new List<Domain.Entities.Signature.SignatureEntity>(),
        Tcc = tcc,
        TccId = tcc.Id,
        User = null,
        UserId = null
    };

    tcc.Documents.Add(document);

    // Mocks
    _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>())
        .Returns(new List<TccEntity> { tcc });

    _documentTypeGateway.FindAll()
        .Returns(new List<DocumentTypeEntity> { docType });

    // Executa o UseCase
    var result = await _useCase.Execute(null);

    // Verificações
    Assert.True(result.IsSuccess);
    Assert.Single(result.Data); // Um TCC retornado

    var pendingSignature = result.Data[0];
    Assert.Equal(tcc.Id, pendingSignature.TccId);
    Assert.Contains("Student User", pendingSignature.StudentNames);

    var pendingDetails = pendingSignature.PendingDetails;
    Assert.NotEmpty(pendingDetails);

    var firstDetail = pendingDetails[0];
    Assert.Equal(document.Id, firstDetail.DocumentId);
    Assert.Equal(docType.Name, firstDetail.DocumentName);
    Assert.NotEmpty(firstDetail.UserDetails);

    var firstUserDetail = firstDetail.UserDetails[0];
    Assert.Equal(userAdvisor.Id, firstUserDetail.UserId);
    Assert.Equal(userAdvisor.Name, firstUserDetail.UserName);
    Assert.Equal(profileAdvisor.Role, firstUserDetail.UserProfile);
}

    [Fact]
    public async Task Execute_ShouldFilterPendingSignatures_ByUserId()
    {
        // Perfis e usuários
        var profileAdvisor = new Domain.Entities.Profile.ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var userAdvisor = new UserEntity { Id = 100, Name = "Advisor User" };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity>(),
            TccInvites = new List<TccInviteEntity>(),
            Documents = new List<DocumentEntity>()
        };

        var userTccAdvisor = new UserTccEntity(userAdvisor, tcc, profileAdvisor, DateTime.UtcNow);
        tcc.UserTccs.Add(userTccAdvisor);

        var docType = new DocumentTypeEntity
        {
            Id = 10,
            Name = "DocType 1",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<Domain.Entities.Profile.ProfileEntity> { profileAdvisor }
        };

        var document = new DocumentEntity
        {
            Id = 101,
            DocumentType = docType,
            DocumentTypeId = docType.Id,
            FileName = "filename",
            Signatures = new List<SignatureEntity>(),
            Tcc = tcc,
            TccId = tcc.Id,
            User = userAdvisor,
            UserId = userAdvisor.Id
        };
        
        tcc.Documents.Add(document);

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>())
            .Returns(new List<TccEntity> { tcc });

        _documentTypeGateway.FindAll()
            .Returns(new List<DocumentTypeEntity> { docType });

        var result = await _useCase.Execute(userAdvisor.Id);

        Assert.True(result.IsSuccess);

        Assert.Single(result.Data);

        foreach (var pending in result.Data)
        {
            foreach (var detail in pending.PendingDetails)
            {
                Assert.All(detail.UserDetails, ud => Assert.Equal(userAdvisor.Id, ud.UserId));
            }
        }
    }
}