using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
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

    public FindTccWorkflowUseCaseTests()
    {
        _useCase = new FindTccWorkflowUseCase(_tccGateway, _documentTypeGateway);
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
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString()
        };

        // DocumentType NOT_ONLY_DOCS with SignatureOrder 3
        var docTypeNotOnlyDocs = new DocumentTypeEntity
        {
            Id = 11,
            Name = "Doc Not Only Docs",
            SignatureOrder = signatureOrder,
            MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString()
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