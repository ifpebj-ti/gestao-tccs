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
    private readonly IAppLoggerGateway<FindAllPendingSignaturesUseCase> _logger = Substitute.For<IAppLoggerGateway<FindAllPendingSignaturesUseCase>>();

    public FindAllPendingSignaturesUseCaseTests()
    {
        _useCase = new FindAllPendingSignaturesUseCase(_tccGateway, _documentTypeGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnEmptyList_WhenNoTccsInProgress()
    {
        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(),Arg.Any<long>())
            .Returns(Task.FromResult(new List<TccEntity>()));

        var result = await _useCase.Execute(null, 1);

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

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), Arg.Any<long>())
            .Returns(new List<TccEntity> { tccWithInvalidStep });

        _documentTypeGateway.FindAll()
            .Returns(new List<DocumentTypeEntity>());

        var result = await _useCase.Execute(null, 1);

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
    _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), Arg.Any<long>())
        .Returns(new List<TccEntity> { tcc });

    _documentTypeGateway.FindAll()
        .Returns(new List<DocumentTypeEntity> { docType });

    // Executa o UseCase
    var result = await _useCase.Execute(null, 1);

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

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), Arg.Any<long>())
            .Returns(new List<TccEntity> { tcc });

        _documentTypeGateway.FindAll()
            .Returns(new List<DocumentTypeEntity> { docType });

        var result = await _useCase.Execute(userAdvisor.Id, 1);

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

    [Fact]
    public async Task Execute_AnexoV_ShouldReturnOnlyAdvisor_WhenAdvisorHasNotSigned()
    {
        var advisorUser = new UserEntity { Id = 10, Name = "Prof Orientador" };
        var studentUser = new UserEntity { Id = 20, Name = "Aluno Orientando" };
        var advisorProfile = new Domain.Entities.Profile.ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new Domain.Entities.Profile.ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Teste",
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new() { User = advisorUser, UserId = advisorUser.Id, Profile = advisorProfile },
                new() { User = studentUser, UserId = studentUser.Id, Profile = studentProfile }
            },
            Documents = new List<DocumentEntity>(),
            TccInvites = new List<TccInviteEntity>()
        };

        var anexoVDocType = new DocumentTypeEntity
        {
            Id = 50,
            Name = "ANEXO V - TERMO DE AFASTAMENTO DE ORIENTAÇÃO DE TRABALHO DE CONCLUSÃO DE CURSO",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<Domain.Entities.Profile.ProfileEntity> { advisorProfile, studentProfile }
        };

        var anexoVDoc = new DocumentEntity
        {
            Id = 100,
            DocumentType = anexoVDocType,
            DocumentTypeId = anexoVDocType.Id,
            FileName = "ANEXO V - AFASTAMENTO",
            Signatures = new List<SignatureEntity>() // Ninguém assinou ainda
        };
        tcc.Documents.Add(anexoVDoc);

        var advisorChange = new gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity
        {
            Id = 1,
            TccId = tcc.Id,
            DocumentId = anexoVDoc.Id,
            CurrentAdvisorId = advisorUser.Id,
            SuggestedAdvisorId = 30,
            RequesterUserId = advisorUser.Id,
            RequesterRole = "ADVISOR",
            Status = "PENDING_SIGNATURES"
        };
        tcc.AdvisorChanges = new List<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity> { advisorChange };

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), Arg.Any<long>())
            .Returns(new List<TccEntity> { tcc });
        _documentTypeGateway.FindAll()
            .Returns(new List<DocumentTypeEntity> { anexoVDocType });

        // Quando buscamos para o professor (deve retornar a pendência do Anexo V)
        var resultAdvisor = await _useCase.Execute(advisorUser.Id, 1);
        Assert.True(resultAdvisor.IsSuccess);
        Assert.NotEmpty(resultAdvisor.Data);
        var advisorDetails = resultAdvisor.Data.SelectMany(d => d.PendingDetails).Where(pd => pd.DocumentId == anexoVDoc.Id).ToList();
        Assert.Single(advisorDetails);
        Assert.Equal(advisorUser.Id, advisorDetails[0].UserDetails[0].UserId);

        // Quando buscamos para o aluno (NÃO deve ter pendência do Anexo V ainda, pois professor não assinou)
        var resultStudent = await _useCase.Execute(studentUser.Id, 1);
        Assert.True(resultStudent.IsSuccess);
        var studentAnexoVDetails = resultStudent.Data.SelectMany(d => d.PendingDetails).Where(pd => pd.DocumentId == anexoVDoc.Id).ToList();
        Assert.Empty(studentAnexoVDetails);
    }

    [Fact]
    public async Task Execute_AnexoV_ShouldReturnStudent_WhenAdvisorHasAlreadySigned()
    {
        var advisorUser = new UserEntity { Id = 10, Name = "Prof Orientador" };
        var studentUser = new UserEntity { Id = 20, Name = "Aluno Orientando" };
        var advisorProfile = new Domain.Entities.Profile.ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new Domain.Entities.Profile.ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Teste",
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new() { User = advisorUser, UserId = advisorUser.Id, Profile = advisorProfile },
                new() { User = studentUser, UserId = studentUser.Id, Profile = studentProfile }
            },
            Documents = new List<DocumentEntity>(),
            TccInvites = new List<TccInviteEntity>()
        };

        var anexoVDocType = new DocumentTypeEntity
        {
            Id = 50,
            Name = "ANEXO V - TERMO DE AFASTAMENTO DE ORIENTAÇÃO DE TRABALHO DE CONCLUSÃO DE CURSO",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<Domain.Entities.Profile.ProfileEntity> { advisorProfile, studentProfile }
        };

        var anexoVDoc = new DocumentEntity
        {
            Id = 100,
            DocumentType = anexoVDocType,
            DocumentTypeId = anexoVDocType.Id,
            FileName = "ANEXO V - AFASTAMENTO",
            Signatures = new List<SignatureEntity>
            {
                new() { UserId = advisorUser.Id, DocumentId = 100, User = advisorUser } // Professor já assinou!
            }
        };
        tcc.Documents.Add(anexoVDoc);

        var advisorChange = new gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity
        {
            Id = 1,
            TccId = tcc.Id,
            DocumentId = anexoVDoc.Id,
            CurrentAdvisorId = advisorUser.Id,
            SuggestedAdvisorId = 30,
            RequesterUserId = advisorUser.Id,
            RequesterRole = "ADVISOR",
            Status = "PENDING_SIGNATURES"
        };
        tcc.AdvisorChanges = new List<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity> { advisorChange };

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>(), Arg.Any<long>())
            .Returns(new List<TccEntity> { tcc });
        _documentTypeGateway.FindAll()
            .Returns(new List<DocumentTypeEntity> { anexoVDocType });

        // Quando o professor já assinou, para o professor não deve mais haver pendência
        var resultAdvisor = await _useCase.Execute(advisorUser.Id, 1);
        Assert.True(resultAdvisor.IsSuccess);
        var advisorDetails = resultAdvisor.Data.SelectMany(d => d.PendingDetails).Where(pd => pd.DocumentId == anexoVDoc.Id).ToList();
        Assert.Empty(advisorDetails);

        // Para o aluno, agora SIM deve aparecer a pendência
        var resultStudent = await _useCase.Execute(studentUser.Id, 1);
        Assert.True(resultStudent.IsSuccess);
        var studentDetails = resultStudent.Data.SelectMany(d => d.PendingDetails).Where(pd => pd.DocumentId == anexoVDoc.Id).ToList();
        Assert.Single(studentDetails);
        Assert.Equal(studentUser.Id, studentDetails[0].UserDetails[0].UserId);
    }
}