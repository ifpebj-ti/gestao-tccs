using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class FindTccWorkflowUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly FindTccWorkflowUseCase _useCase;
    private readonly IAppLoggerGateway<FindTccWorkflowUseCase> _logger = Substitute.For<IAppLoggerGateway<FindTccWorkflowUseCase>>();

    [Theory]
    [InlineData("PENDING_SIGNATURES", true, true)]
    [InlineData("APPROVED", true, false)]
    [InlineData("PENDING_SIGNATURES", false, false)]
    public async Task Execute_OnlyListsTheAdvisorChangeDocumentForAnActiveRequestWithMatchingAttachment(
        string status, bool hasDocumentId, bool includeOptional)
    {
        var student = new UserEntity { Id = 11, Name = "Ana" };
        var advisor = new UserEntity { Id = 12, Name = "Bia" };
        var studentProfile = new ProfileEntity { Id = 1, Role = "STUDENT" };
        var advisorProfile = new ProfileEntity { Id = 2, Role = "ADVISOR" };
        var shared = new DocumentTypeEntity
        {
            Id = 10, Name = "Relatorio", SignatureOrder = 3, MethodSignature = "NOT_ONLY_DOCS",
            Profiles = [studentProfile, advisorProfile]
        };
        var change = new DocumentTypeEntity
        {
            Id = 11, Name = "ANEXO IX", SignatureOrder = 3, MethodSignature = "ONLY_DOCS",
            Profiles = [studentProfile, advisorProfile]
        };
        var excluded = new DocumentTypeEntity { Id = 12, Name = "ANEXO II", SignatureOrder = 3 };
        studentProfile.DocumentTypes = [shared, change];
        advisorProfile.DocumentTypes = [shared, change];
        var tcc = new TccEntity
        {
            Id = 79,
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            UserTccs =
            [
                new UserTccEntity { User = student, UserId = student.Id, Profile = studentProfile },
                new UserTccEntity { User = advisor, UserId = advisor.Id, Profile = advisorProfile }
            ],
            Documents =
            [
                new DocumentEntity { Id = 99, DocumentTypeId = shared.Id, DocumentType = shared, UserId = student.Id, User = student, Signatures = [] },
                new DocumentEntity { Id = 100, DocumentTypeId = change.Id, DocumentType = change, UserId = advisor.Id, User = advisor, Signatures = [] },
                new DocumentEntity { Id = 101, DocumentType = null! }
            ],
            AdvisorChanges =
            [
                new TccAdvisorChangeEntity { Id = 3, Status = status, DocumentId = hasDocumentId ? 100 : null }
            ]
        };
        _tccGateway.FindTccWorkflow(tcc.Id, student.Id).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { shared, change, excluded });

        var result = await _useCase.Execute(tcc.Id, student.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data.Step);
        Assert.Equal(includeOptional ? new[] { 10L, 11L } : new[] { 10L },
            result.Data.Signatures.Select(signature => signature.DocumentId));
        var sharedSignature = Assert.Single(result.Data.Signatures.Where(signature => signature.DocumentId == shared.Id));
        var studentDetail = Assert.Single(sharedSignature.DetailsNotOnlyDocs!);
        Assert.False(studentDetail.IsSigned);
        Assert.False(Assert.Single(studentDetail.OtherSignatures).IsSigned);
        if (includeOptional)
        {
            var optional = Assert.Single(result.Data.Signatures.Where(signature => signature.DocumentId == change.Id));
            var detail = Assert.Single(optional.DetailsOnlyDocs!);
            Assert.Equal(advisor.Id, detail.UserId);
            Assert.Equal("ADVISOR", detail.UserProfile);
            Assert.False(detail.IsSigned);
        }
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Theory]
    [InlineData("etapa-desconhecida")]
    [InlineData("999")]
    public async Task Execute_DoesNotBuildWorkflowForAnUnsupportedStep(string step)
    {
        var tcc = new TccEntity { Id = 79, Step = step };
        _tccGateway.FindTccWorkflow(79, 11).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity>());

        var result = await _useCase.Execute(79, 11);

        Assert.Null(result.Data);
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    public FindTccWorkflowUseCaseTests()
    {
        _useCase = new FindTccWorkflowUseCase(_tccGateway, _documentTypeGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        _tccGateway.FindTccWorkflow(Arg.Any<long?>(), Arg.Any<long>()).Returns(Task.FromResult<TccEntity?>(null));

        var result = await _useCase.Execute(1, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao buscar workflow", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task Execute_ShouldReturnWorkflowDto_ForProposalRegistrationStep()
    {
        // Arrange
        var step = StepTccType.PROPOSAL_REGISTRATION.ToString();

        var tccInvites = new List<TccInviteEntity>
        {
            new TccInviteEntity { Email = "student1@example.com", IsValidCode = true },
            new TccInviteEntity { Email = "student2@example.com", IsValidCode = true }
        };

        var user = new UserEntity { Email = "student1@example.com", Id = 10 };
        
        var userTccs = new List<UserTccEntity>
        {
            new UserTccEntity
            {
                User = user,
                Profile = new ProfileEntity
                {
                    Role = RoleType.STUDENT.ToString(),
                    DocumentTypes = new List<DocumentTypeEntity>()
                    {
                        new DocumentTypeEntity()
                        {
                            Id = 1,
                            SignatureOrder = 1
                        }
                    }
                },
                UserId = user.Id
            }
        };

        var tcc = new TccEntity
        {
            Id = 123,
            Step = step,
            TccInvites = tccInvites,
            UserTccs = userTccs
        };

        var documentTypes = new List<DocumentTypeEntity>(); // empty, not used in this step

        _tccGateway.FindTccWorkflow(Arg.Any<long?>(), Arg.Any<long>()).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(documentTypes);

        // Act
        var result = await _useCase.Execute(tcc.Id, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tcc.Id, result.Data.TccId);
        Assert.Equal(1, result.Data.Step); // order for PROPOSAL_REGISTRATION is 1
        var signature = result.Data.Signatures[0];
        Assert.Equal(1, signature.DocumentId);
        Assert.Equal("CADASTRO DE ESTUDANTES", signature.AttachmentName);

        var details = signature.DetailsNotOnlyDocs;
        Assert.NotNull(details);
        Assert.Equal(2, details.Count);

        var firstDetail = details[0];
        Assert.Equal(RoleType.STUDENT.ToString(), firstDetail.UserProfile);
        Assert.Equal("student1@example.com", firstDetail.UserName);
        Assert.True(firstDetail.IsSigned);
        Assert.Empty(firstDetail.OtherSignatures);

        var secondDetail = details[1];
        Assert.Equal(RoleType.STUDENT.ToString(), secondDetail.UserProfile);
        Assert.Equal("student2@example.com", secondDetail.UserName);
        Assert.False(secondDetail.IsSigned);
    }

    [Fact]
    public async Task Execute_ShouldAdvanceStep_WhenAllDevelopmentDocumentsSigned()
    {
        // Arrange — step DEVELOPMENT_AND_MONITORING com todos docs assinados → avança para PREPARATION_FOR_PRESENTATION
        var step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString();

        var studentProfile = new ProfileEntity { Role = RoleType.STUDENT.ToString(), DocumentTypes = new List<DocumentTypeEntity>() };
        var advisorProfile = new ProfileEntity { Role = RoleType.ADVISOR.ToString(), DocumentTypes = new List<DocumentTypeEntity>() };

        var userStudent = new UserEntity { Id = 1, Name = "Student", Email = "student@example.com" };
        var userAdvisor = new UserEntity { Id = 2, Name = "Advisor", Email = "advisor@example.com" };

        var docType = new DocumentTypeEntity
        {
            Id = 30, Name = "PROGRESSO",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        var signedDoc = new DocumentEntity
        {
            Id = 1,
            DocumentTypeId = docType.Id,
            DocumentType = docType,
            User = userStudent,
            Signatures = new List<SignatureEntity> { new SignatureEntity { User = userStudent, UserId = userStudent.Id } }
        };

        studentProfile.DocumentTypes.Add(docType);

        var tcc = new TccEntity
        {
            Id = 789,
            Step = step,
            Documents = new List<DocumentEntity> { signedDoc },
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { User = userStudent, Profile = studentProfile, UserId = userStudent.Id },
                new UserTccEntity { User = userAdvisor, Profile = advisorProfile, UserId = userAdvisor.Id }
            },
            TccInvites = new List<TccInviteEntity>()
        };

        _tccGateway.FindTccWorkflow(Arg.Any<long?>(), Arg.Any<long>()).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.Execute(tcc.Id, 1);

        // Assert
        Assert.True(result.IsSuccess);
        // O step deve ter avançado
        Assert.Equal(StepTccType.PREPARATION_FOR_PRESENTATION.ToString(), tcc.Step);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldNotAdvanceStep_WhenDevelopmentDocumentsNotFullySigned()
    {
        // Arrange — step DEVELOPMENT_AND_MONITORING com doc NÃO assinado → não avança
        var step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString();

        var studentProfile = new ProfileEntity { Role = RoleType.STUDENT.ToString(), DocumentTypes = new List<DocumentTypeEntity>() };
        var userStudent = new UserEntity { Id = 1, Name = "Student", Email = "student@example.com" };

        var docType = new DocumentTypeEntity
        {
            Id = 31, Name = "RELATÓRIO",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        var unsignedDoc = new DocumentEntity
        {
            Id = 2,
            DocumentTypeId = docType.Id,
            DocumentType = docType,
            User = userStudent,
            Signatures = new List<SignatureEntity>() // sem assinaturas
        };

        studentProfile.DocumentTypes.Add(docType);

        var tcc = new TccEntity
        {
            Id = 790,
            Step = step,
            Documents = new List<DocumentEntity> { unsignedDoc },
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { User = userStudent, Profile = studentProfile, UserId = userStudent.Id }
            },
            TccInvites = new List<TccInviteEntity>()
        };

        _tccGateway.FindTccWorkflow(Arg.Any<long?>(), Arg.Any<long>()).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });

        // Act
        var result = await _useCase.Execute(tcc.Id, 1);

        // Assert
        Assert.True(result.IsSuccess);
        // Step NÃO deve ter avançado
        Assert.Equal(StepTccType.DEVELOPMENT_AND_MONITORING.ToString(), tcc.Step);
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Fact]
    public async Task Execute_ShouldReturnWorkflowDto_ForNormalSteps_WithOnlyDocsAndNotOnlyDocs()
    {
        // Arrange
        var step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString();
        var signatureOrder = 3; // from the mapping

        var profileStudent = new ProfileEntity
        {
            Role = RoleType.STUDENT.ToString(),
            DocumentTypes = new List<DocumentTypeEntity>()
        };
        var profileAdvisor = new ProfileEntity
        {
            Role = RoleType.ADVISOR.ToString(),
            DocumentTypes = new List<DocumentTypeEntity>()
        };

        var userStudent = new UserEntity { Id = 1, Name = "Student User", Email = "student@example.com" };
        var userAdvisor = new UserEntity { Id = 2, Name = "Advisor User", Email = "advisor@example.com" };

        var userTccStudent = new UserTccEntity { User = userStudent, Profile = profileStudent };
        var userTccAdvisor = new UserTccEntity { User = userAdvisor, Profile = profileAdvisor };

        // DocumentType ONLY_DOCS with SignatureOrder 3
        var docTypeOnlyDocs = new DocumentTypeEntity
        {
            Id = 10,
            Name = "Doc Only Docs",
            SignatureOrder = signatureOrder,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { profileStudent }
        };

        // DocumentType NOT_ONLY_DOCS with SignatureOrder 3
        var docTypeNotOnlyDocs = new DocumentTypeEntity
        {
            Id = 11,
            Name = "Doc Not Only Docs",
            SignatureOrder = signatureOrder,
            MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { profileAdvisor }
        };

        profileStudent.DocumentTypes.Add(docTypeOnlyDocs);
        profileAdvisor.DocumentTypes.Add(docTypeNotOnlyDocs);

        var docOnlyDocs1 = new DocumentEntity
        {
            DocumentTypeId = docTypeOnlyDocs.Id,
            User = null,
            Signatures = new List<SignatureEntity>
            {
                new SignatureEntity { User = userStudent }
            }
        };

        var docNotOnlyDocs1 = new DocumentEntity
        {
            DocumentTypeId = docTypeNotOnlyDocs.Id,
            User = userStudent,
            Signatures = new List<SignatureEntity>
            {
                new SignatureEntity { User = userStudent },
                new SignatureEntity { User = userAdvisor }
            }
        };

        var tcc = new TccEntity
        {
            Id = 456,
            Step = step,
            Documents = new List<DocumentEntity> { docOnlyDocs1, docNotOnlyDocs1 },
            UserTccs = new List<UserTccEntity> { userTccStudent, userTccAdvisor },
            TccInvites = new List<TccInviteEntity>()
        };

        var documentTypes = new List<DocumentTypeEntity> { docTypeOnlyDocs, docTypeNotOnlyDocs };

        _tccGateway.FindTccWorkflow(Arg.Any<long?>(), Arg.Any<long>()).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(documentTypes);

        // Act
        var result = await _useCase.Execute(tcc.Id, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tcc.Id, result.Data.TccId);
        Assert.Equal(signatureOrder, result.Data.Step);

        Assert.Equal(2, result.Data.Signatures.Count);

        var onlyDocsSignature = result.Data.Signatures.First(s => s.DocumentId == docTypeOnlyDocs.Id);
        Assert.NotNull(onlyDocsSignature.DetailsOnlyDocs);
        Assert.Null(onlyDocsSignature.DetailsNotOnlyDocs);
        Assert.Contains(onlyDocsSignature.DetailsOnlyDocs, d => d.UserId == userStudent.Id && d.IsSigned);

        var notOnlyDocsSignature = result.Data.Signatures.First(s => s.DocumentId == docTypeNotOnlyDocs.Id);
        Assert.Null(notOnlyDocsSignature.DetailsOnlyDocs);
        Assert.NotNull(notOnlyDocsSignature.DetailsNotOnlyDocs);

        var mainUserDetail = notOnlyDocsSignature.DetailsNotOnlyDocs.FirstOrDefault(d => d.UserId == userStudent.Id);
        Assert.NotNull(mainUserDetail);
        Assert.True(mainUserDetail.IsSigned);

        var otherSignature = mainUserDetail.OtherSignatures.FirstOrDefault(d => d.UserId == userAdvisor.Id);
        Assert.NotNull(otherSignature);
        Assert.True(otherSignature.IsSigned);
    }
}
