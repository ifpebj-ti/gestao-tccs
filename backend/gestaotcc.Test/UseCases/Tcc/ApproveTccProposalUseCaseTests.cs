using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class ApproveTccProposalUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IAppLoggerGateway<ApproveTccProposalUseCase> _logger = Substitute.For<IAppLoggerGateway<ApproveTccProposalUseCase>>();

    private readonly ApproveTccProposalUseCase _useCase;

    public ApproveTccProposalUseCaseTests()
    {
        _useCase = new ApproveTccProposalUseCase(_tccGateway, _documentTypeGateway, _emailGateway, _logger);
    }

    // ─────────────────────────── helpers ────────────────────────────

    private static TccEntity BuildBasicTcc(int tccId = 5, int advisorUserId = 10, int studentUserId = 1)
    {
        var advisor  = new UserEntity { Id = advisorUserId, Name = "Advisor", Email = "adv@ifpe.edu.br", Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.ADVISOR.ToString() } } };
        var student  = new UserEntity { Id = studentUserId, Name = "Student", Email = "stu@ifpe.edu.br", Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.STUDENT.ToString() } } };

        var tcc = new TccEntity
        {
            Id = tccId,
            Title = "Proposta TCC",
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            Step   = StepTccType.PROPOSAL_REGISTRATION.ToString(),
            Documents = new List<DocumentEntity>(),
            ParticipantInvitations = new List<ProposalParticipantEntity>()
        };

        tcc.UserTccs = new List<UserTccEntity>
        {
            new UserTccEntity { TccId = tccId, UserId = advisorUserId, User = advisor,
                Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } },
            new UserTccEntity { TccId = tccId, UserId = studentUserId, User = student,
                Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
        };

        return tcc;
    }

    // ─────────────────────────── testes básicos (já existentes) ─────

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenAdvisorApprovesProposal()
    {
        var tcc = BuildBasicTcc();

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        var result = await _useCase.Execute(5, 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusTccType.IN_PROGRESS.ToString(), tcc.Status);
        Assert.Equal(StepTccType.START_AND_ORGANIZATION.ToString(), tcc.Step);
        await _tccGateway.Received(1).Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccNotFound()
    {
        _tccGateway.FindTccById(99).Returns(Task.FromResult<TccEntity?>(null));

        var result = await _useCase.Execute(99, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserIsNotAssignedAdvisor()
    {
        var tcc = new TccEntity
        {
            Id = 5,
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 20, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } }
            }
        };
        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));

        var result = await _useCase.Execute(5, 99);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenTccIsNotPendingApproval()
    {
        var tcc = new TccEntity
        {
            Id = 5,
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 10, Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } }
            }
        };
        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));

        var result = await _useCase.Execute(5, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    // ─────────────────────────── testes novos (Lote 4) ─────────────

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenParticipantInvitationNotAccepted()
    {
        // ParticipantInvitations contém item com Status != "ACCEPTED"
        var tcc = BuildBasicTcc();
        tcc.ParticipantInvitations = new List<ProposalParticipantEntity>
        {
            new ProposalParticipantEntity { Email = "other@ifpe.edu.br", Status = "PENDING" }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));

        var result = await _useCase.Execute(5, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenStudentAccountIsIneligible()
    {
        // ParticipantInvitations todos ACCEPTED, mas estudante com conta inativa
        var tcc = BuildBasicTcc();
        tcc.ParticipantInvitations = new List<ProposalParticipantEntity>
        {
            new ProposalParticipantEntity { Email = "other@ifpe.edu.br", Status = "ACCEPTED" }
        };
        // Tornar o estudante inelegível
        var studentUserTcc = tcc.UserTccs.First(u => u.Profile.Role == RoleType.STUDENT.ToString());
        studentUserTcc.User!.Status = "INACTIVE";

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));

        var result = await _useCase.Execute(5, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenStudentsAreInDifferentCampusCourse()
    {
        // Dois estudantes com CampiCourseId diferentes
        var advisor  = new UserEntity { Id = 10, Name = "Advisor", Email = "adv@ifpe.edu.br", Status = "ACTIVE",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.ADVISOR.ToString() } } };
        var student1 = new UserEntity { Id = 1, Name = "Student1", Email = "s1@ifpe.edu.br", Status = "ACTIVE",
            CampiCourseId = 1,
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.STUDENT.ToString() } } };
        var student2 = new UserEntity { Id = 2, Name = "Student2", Email = "s2@ifpe.edu.br", Status = "ACTIVE",
            CampiCourseId = 2, // campus diferente
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.STUDENT.ToString() } } };

        var tcc = new TccEntity
        {
            Id = 5,
            Title = "TCC Multi",
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            Step   = StepTccType.PROPOSAL_REGISTRATION.ToString(),
            Documents = new List<DocumentEntity>(),
            ParticipantInvitations = new List<ProposalParticipantEntity>
            {
                new ProposalParticipantEntity { Status = "ACCEPTED" }
            }
        };
        tcc.UserTccs = new List<UserTccEntity>
        {
            new UserTccEntity { UserId = 10, User = advisor,  Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } },
            new UserTccEntity { UserId = 1,  User = student1, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } },
            new UserTccEntity { UserId = 2,  User = student2, Profile = new ProfileEntity { Role = RoleType.STUDENT.ToString() } }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));

        var result = await _useCase.Execute(5, 10);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldNotCallDocumentTypeGateway_WhenDocumentsAlreadyExist()
    {
        var tcc = BuildBasicTcc();
        // Já existe um documento — não deve re-gerar
        tcc.Documents = new List<DocumentEntity>
        {
            new DocumentEntity { Id = 99 }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        var result = await _useCase.Execute(5, 10);

        Assert.True(result.IsSuccess);
        await _documentTypeGateway.DidNotReceive().FindAll();
    }

    [Fact]
    public async Task Execute_ShouldGenerateDocuments_WhenNoDocumentsExistYet()
    {
        var tcc = BuildBasicTcc();
        tcc.Documents = new List<DocumentEntity>();

        var studentProfile = new ProfileEntity { Id = 1, Role = RoleType.STUDENT.ToString() };
        var advisorProfile = new ProfileEntity { Id = 2, Role = RoleType.ADVISOR.ToString() };

        // docType ONLY_DOCS com 1 perfil (gera 1 documento por usuário com aquele perfil)
        var docTypeOnlyDocsSingle = new DocumentTypeEntity
        {
            Id = 10, Name = "TERMO DE COMPROMISSO",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        // docType ONLY_DOCS com múltiplos perfis (gera 1 doc sem usuário)
        var docTypeOnlyDocsMulti = new DocumentTypeEntity
        {
            Id = 11, Name = "DECLARAÇÃO CONJUNTA",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile, advisorProfile }
        };

        // docType NOT_ONLY_DOCS — STUDENT assina por si (gera doc por estudante)
        var docTypeNotOnlyDocs = new DocumentTypeEntity
        {
            Id = 12, Name = "FICHA AVALIATIVA",
            MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        // docType ANEXO II → deve ser ignorado
        var docTypeAnexoII = new DocumentTypeEntity
        {
            Id = 13, Name = "ANEXO II - TERMO DE COMPROMISSO DE ORIENTAÇÃO VOLUNTÁRIA",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile }
        };

        // docType ANEXO V → deve ser ignorado
        var docTypeAnexoV = new DocumentTypeEntity
        {
            Id = 14, Name = "ANEXO V - TROCA DE ORIENTADOR",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile }
        };

        var documentTypes = new List<DocumentTypeEntity>
        {
            docTypeOnlyDocsSingle, docTypeOnlyDocsMulti, docTypeNotOnlyDocs,
            docTypeAnexoII, docTypeAnexoV
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(documentTypes));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        var result = await _useCase.Execute(5, 10);

        Assert.True(result.IsSuccess);
        await _documentTypeGateway.Received(1).FindAll();
        // Deve ter gerado documentos (pelo menos o ONLY_DOCS single e o multi)
        Assert.True(tcc.Documents.Count >= 2);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenEmailGatewayThrowsException()
    {
        // Email falha mas o use case deve continuar com sucesso (try/catch interno)
        var tcc = BuildBasicTcc();
        tcc.Documents = new List<DocumentEntity>();

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns<Task<ResultPattern<bool>>>(_ => throw new Exception("SMTP error"));

        var result = await _useCase.Execute(5, 10);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusTccType.IN_PROGRESS.ToString(), tcc.Status);
    }

    [Fact]
    public async Task Execute_ShouldNotSendEmail_WhenNoStudentUserTccFound()
    {
        // TCC sem nenhum UserTcc com role STUDENT → email não enviado
        var advisor = new UserEntity { Id = 10, Name = "Advisor", Email = "adv@ifpe.edu.br" };
        var tcc = new TccEntity
        {
            Id = 5,
            Title = "TCC Sem Estudante",
            Status = StatusTccType.PENDING_APPROVAL.ToString(),
            Step   = StepTccType.PROPOSAL_REGISTRATION.ToString(),
            Documents = new List<DocumentEntity>(),
            ParticipantInvitations = new List<ProposalParticipantEntity>(),
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { UserId = 10, User = advisor,
                    Profile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() } }
            }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);

        var result = await _useCase.Execute(5, 10);

        Assert.True(result.IsSuccess);
        await _emailGateway.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task Execute_ShouldNotGenerateDocs_ForOnlyDocsWithSingleProfileWhenNoMatchingUser()
    {
        // docType ONLY_DOCS com 1 perfil (COORDINATOR) e TCC sem usuário com esse perfil → não gera documento
        var tcc = BuildBasicTcc();
        tcc.Documents = new List<DocumentEntity>();

        var coordinatorProfile = new ProfileEntity { Id = 99, Role = "COORDINATOR" };
        var docTypeSingleNoMatch = new DocumentTypeEntity
        {
            Id = 20, Name = "DOCUMENTO COORDENADOR",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { coordinatorProfile }
        };

        _tccGateway.FindTccById(5).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity> { docTypeSingleNoMatch }));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        var result = await _useCase.Execute(5, 10);

        Assert.True(result.IsSuccess);
        // Nenhum documento deve ter sido gerado (usuário sem o perfil COORDINATOR)
        Assert.Empty(tcc.Documents);
    }
}
