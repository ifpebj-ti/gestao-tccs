using System.IO;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using iText.Kernel.Pdf;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class SignSignatureUseCaseTests
{
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly SignSignatureUseCase _useCase;
    private readonly IAppLoggerGateway<SignSignatureUseCase> _logger = Substitute.For<IAppLoggerGateway<SignSignatureUseCase>>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();

    public SignSignatureUseCaseTests()
    {
        _useCase = new SignSignatureUseCase(_documentTypeGateway, _tccGateway, _storageGateway, _emailGateway, _userGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldMoveAdvisorChangeToCoordination_AndNotifyActiveCoordinatorAndSupervisor_WhenLastSignatureIsAdded()
    {
        var (tcc, document, advisor) = CreatePendingAdvisorChange(suggestedAdvisorId: 6);
        var coordinator = new UserEntity { Id = 3, Name = "Coordenação", Email = "coord@test.com", Status = "ACTIVE" };
        var supervisor = new UserEntity { Id = 4, Name = "Supervisão", Email = "supervisor@test.com", Status = "ACTIVE" };
        var inactiveCoordinator = new UserEntity { Id = 5, Name = "Inativo", Email = "inactive@test.com", Status = "INACTIVE" };

        _tccGateway.FindTccById(tcc.Id).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { document.DocumentType });
        _userGateway.FindAllByFilter(Arg.Is<UserFilterDTO>(f => f.Profile == RoleType.COORDINATOR.ToString()), 7)
            .Returns(new List<UserEntity> { coordinator, inactiveCoordinator });
        _userGateway.FindAllByFilter(Arg.Is<UserFilterDTO>(f => f.Profile == RoleType.SUPERVISOR.ToString()), 7)
            .Returns(new List<UserEntity> { supervisor });
        _storageGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        var result = await _useCase.Execute(new SignSignatureDTO(
            tcc.Id, document.Id, advisor.Id, CreateSignedPdfBytes("ANEXO IX"), 1, "application/pdf", "ANEXO IX.pdf"));

        Assert.True(result.IsSuccess);
        Assert.Equal("PENDING_COORDINATION", tcc.AdvisorChanges.Single().Status);
        await _tccGateway.Received(1).UpdateAdvisorChange(Arg.Is<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity>(c => c.Status == "PENDING_COORDINATION"));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(e => e.Recipient == coordinator.Email && e.TypeTemplate == "ADVISOR-CHANGE-COORDINATION"));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(e => e.Recipient == supervisor.Email && e.TypeTemplate == "ADVISOR-CHANGE-COORDINATION"));
        await _emailGateway.DidNotReceive().Send(Arg.Is<SendEmailDTO>(e => e.Recipient == inactiveCoordinator.Email));
    }

    [Fact]
    public async Task Execute_ShouldNotMoveAdvisorChangeOrNotifyCoordination_WhileStudentSignatureIsMissing()
    {
        var (tcc, document, advisor) = CreatePendingAdvisorChange(studentAlreadySigned: false);
        _tccGateway.FindTccById(tcc.Id).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { document.DocumentType });
        _storageGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var result = await _useCase.Execute(new SignSignatureDTO(
            tcc.Id, document.Id, advisor.Id, CreateSignedPdfBytes("ANEXO IX"), 1, "application/pdf", "ANEXO IX.pdf"));

        Assert.True(result.IsSuccess);
        Assert.Equal("PENDING_SIGNATURES", tcc.AdvisorChanges.Single().Status);
        await _tccGateway.DidNotReceive().UpdateAdvisorChange(Arg.Any<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity>());
        await _emailGateway.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    private static (TccEntity Tcc, DocumentEntity Document, UserEntity Advisor) CreatePendingAdvisorChange(long? suggestedAdvisorId = null, bool studentAlreadySigned = true)
    {
        var advisor = new UserEntity { Id = 1, Name = "Orientador", Email = "advisor@test.com", Status = "ACTIVE" };
        var student = new UserEntity { Id = 2, Name = "Estudante", Email = "student@test.com", Status = "ACTIVE", CampiCourseId = 7 };
        var advisorProfile = new ProfileEntity { Id = 11, Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new ProfileEntity { Id = 12, Role = RoleType.STUDENT.ToString() };
        var docType = new DocumentTypeEntity
        {
            Id = 90,
            Name = "ANEXO IX - TERMO DE SUBSTITUIÇÃO DE ORIENTAÇÃO",
            SignatureOrder = 0,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile, studentProfile }
        };
        var document = new DocumentEntity
        {
            Id = 91,
            DocumentTypeId = docType.Id,
            DocumentType = docType,
            FileName = "anexo-ix",
            Signatures = studentAlreadySigned
                ? new List<SignatureEntity> { new() { Id = 92, UserId = student.Id, DocumentId = 91, User = student } }
                : new List<SignatureEntity>()
        };
        var tcc = new TccEntity
        {
            Id = 93,
            Title = "TCC de Teste",
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Documents = new List<DocumentEntity> { document },
            UserTccs = new List<UserTccEntity>
            {
                new() { Id = 94, UserId = advisor.Id, User = advisor, Profile = advisorProfile },
                new() { Id = 95, UserId = student.Id, User = student, Profile = studentProfile }
            },
            AdvisorChanges = new List<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity>
            {
                new()
                {
                    Id = 96,
                    TccId = 93,
                    DocumentId = document.Id,
                    RequesterUserId = student.Id,
                    RequesterRole = RoleType.STUDENT.ToString(),
                    CurrentAdvisorId = advisor.Id,
                    SuggestedAdvisorId = suggestedAdvisorId,
                    Status = "PENDING_SIGNATURES",
                    CurrentAdvisor = advisor
                }
            }
        };
        return (tcc, document, advisor);
    }

    [Fact]
    public async Task Execute_ShouldReturnInvalid_WhenFileIsInvalid()
    {
        var dto = new SignSignatureDTO(1, 1, 1, new byte[0], 6, "application/pdf", "Doc.pdf");

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

        var validPdf = CreateSignedPdfBytes();
        var dto = new SignSignatureDTO(1, 1, 1, validPdf, 1, "application/pdf", "Doc.pdf");
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
        _storageGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var validPdf = CreateSignedPdfBytes();
        var dto = new SignSignatureDTO(1, 1, 1, validPdf, 1, "application/pdf", "Doc.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);

        await _storageGateway.Received(1).Send(document.FileName, dto.File, dto.FileContentType);
    }

    [Fact]
    public async Task Should_Advance_To_Finalization_With_30_Day_Deadline_When_Presentation_Approved()
    {
        var advisor = new UserEntity { Id = 1, Name = "Prof", Email = "prof@test.com" };
        var student = new UserEntity { Id = 2, Name = "Aluno", Email = "aluno@test.com" };
        var advisorProfile = new ProfileEntity { Id = 4, Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new ProfileEntity { Id = 5, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 50,
            Name = "ANEXO IV - FICHA AVALIATIVA",
            SignatureOrder = 5,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile, studentProfile }
        };

        var document = new DocumentEntity
        {
            Id = 1,
            DocumentTypeId = 50,
            DocumentType = docType,
            FileName = "AnexoIV.pdf",
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 10,
            Title = "TCC de Teste",
            Step = StepTccType.PRESENTATION_AND_EVALUATION.ToString(),
            Status = StatusTccType.IN_PROGRESS.ToString(),
            Documents = new List<DocumentEntity> { document },
            BankingMembers = new List<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity>
            {
                new("Banca 1", "banca1@test.com", "Membro", "tok1", 10) { Grade = 8.5m },
                new("Banca 2", "banca2@test.com", "Membro", "tok2", 10) { Grade = 9.0m }
            },
            UserTccs = new List<UserTccEntity>
            {
                new() { Id = 1, UserId = 1, User = advisor, Profile = advisorProfile },
                new() { Id = 2, UserId = 2, User = student, Profile = studentProfile }
            }
        };

        // Simula que o orientador já assinou, falta apenas o estudante
        document.Signatures.Add(new SignatureEntity { Id = 1, UserId = 1, User = advisor, DocumentId = 1 });

        _tccGateway.FindTccById(10).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var validPdf = CreateSignedPdfBytes("ANEXO IV");
        var dto = new SignSignatureDTO(10, 1, 2, validPdf, 1, "application/pdf", "ANEXO IV.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(StepTccType.FINALIZATION_AND_PUBLICATION.ToString(), tcc.Step);
        Assert.NotEqual(StatusTccType.COMPLETED.ToString(), tcc.Status);
        Assert.NotNull(tcc.FinalSubmissionDeadline);
        Assert.True(tcc.FinalSubmissionDeadline > DateTime.UtcNow.AddDays(29));
    }

    [Fact]
    public async Task Should_Mark_Status_As_Rejected_When_Presentation_Grade_Below_7()
    {
        var advisor = new UserEntity { Id = 1, Name = "Prof", Email = "prof@test.com" };
        var student = new UserEntity { Id = 2, Name = "Aluno", Email = "aluno@test.com" };
        var advisorProfile = new ProfileEntity { Id = 4, Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new ProfileEntity { Id = 5, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 50,
            Name = "ANEXO IV - FICHA AVALIATIVA",
            SignatureOrder = 5,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile, studentProfile }
        };

        var document = new DocumentEntity
        {
            Id = 1,
            DocumentTypeId = 50,
            DocumentType = docType,
            FileName = "AnexoIV.pdf",
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 10,
            Title = "TCC de Teste",
            Step = StepTccType.PRESENTATION_AND_EVALUATION.ToString(),
            Status = StatusTccType.IN_PROGRESS.ToString(),
            Documents = new List<DocumentEntity> { document },
            BankingMembers = new List<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity>
            {
                new("Banca 1", "banca1@test.com", "Membro", "tok1", 10) { Grade = 5.0m },
                new("Banca 2", "banca2@test.com", "Membro", "tok2", 10) { Grade = 6.0m }
            },
            UserTccs = new List<UserTccEntity>
            {
                new() { Id = 1, UserId = 1, User = advisor, Profile = advisorProfile },
                new() { Id = 2, UserId = 2, User = student, Profile = studentProfile }
            }
        };

        // Simula que o orientador já assinou, falta apenas o estudante
        document.Signatures.Add(new SignatureEntity { Id = 1, UserId = 1, User = advisor, DocumentId = 1 });

        _tccGateway.FindTccById(10).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var validPdf = CreateSignedPdfBytes("ANEXO IV");
        var dto = new SignSignatureDTO(10, 1, 2, validPdf, 1, "application/pdf", "ANEXO IV.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusTccType.REJECTED.ToString(), tcc.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenFileIsEmptyOrTooLarge()
    {
        var dtoEmpty = new SignSignatureDTO(1, 1, 1, Array.Empty<byte>(), 1, "application/pdf", "doc.pdf");
        var resultEmpty = await _useCase.Execute(dtoEmpty);

        Assert.True(resultEmpty.IsFailure);
        Assert.Equal(409, resultEmpty.ErrorDetails?.Status);

        var dtoTooLarge = new SignSignatureDTO(1, 1, 1, new byte[11 * 1024 * 1024], 1, "application/pdf", "doc.pdf");
        var resultTooLarge = await _useCase.Execute(dtoTooLarge);

        Assert.True(resultTooLarge.IsFailure);
        Assert.Equal(409, resultTooLarge.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenFileIsNotValidPdf()
    {
        var corruptedBytes = new byte[] { 1, 2, 3, 4, 5 };
        var dto = new SignSignatureDTO(1, 1, 1, corruptedBytes, 1, "application/pdf", "corrupted.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenUserAlreadySignedDocument()
    {
        var user = new UserEntity { Id = 10, Name = "Aluno" };
        var doc = new DocumentEntity
        {
            Id = 1,
            DocumentType = new DocumentTypeEntity { Id = 1, Name = "Doc", SignatureOrder = 2 },
            FileName = "doc",
            Signatures = new List<SignatureEntity> { new() { Id = 1, UserId = 10, DocumentId = 1 } }
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity> { new() { UserId = 10, User = user, Profile = new ProfileEntity { Role = "STUDENT" } } }
        };

        _tccGateway.FindTccById(1).Returns(tcc);

        var validPdf = CreateSignedPdfBytes("DOC");
        var dto = new SignSignatureDTO(1, 1, 10, validPdf, 1, "application/pdf", "doc.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Contains("Erro ao realizar upload", result.Message);
    }

    [Fact]
    public async Task Execute_AnexoVIII_ShouldUpdateTccSchedule_WhenScheduleDataProvided()
    {
        var advisor = new UserEntity { Id = 10, Name = "Prof Orientador", Email = "adv@test.com" };
        var advisorProfile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 8,
            Name = "ANEXO VIII - REQUERIMENTO DE BANCA EXAMINADORA",
            SignatureOrder = 4,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 80,
            DocumentType = docType,
            DocumentTypeId = 8,
            FileName = "anexo_viii",
            Signatures = new List<SignatureEntity>()
        };

        var docPending = new DocumentEntity
        {
            Id = 81,
            DocumentType = docType,
            DocumentTypeId = 8,
            FileName = "anexo_viii_outro",
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC com Agendamento",
            Step = StepTccType.PREPARATION_FOR_PRESENTATION.ToString(),
            Documents = new List<DocumentEntity> { doc, docPending },
            UserTccs = new List<UserTccEntity> { new() { UserId = 10, User = advisor, Profile = advisorProfile } }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(doc.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var validPdf = CreateSignedPdfBytes("ANEXO VIII");
        var dto = new SignSignatureDTO(1, 80, 10, validPdf, 1, "application/pdf", "anexo_viii.pdf",
            ScheduleDate: "2026-11-20", ScheduleTime: "14:00", ScheduleLocation: "Auditório Principal");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.NotNull(tcc.TccSchedule);
        Assert.Equal("Auditório Principal", tcc.TccSchedule.Location);
        await _tccGateway.Received().Update(tcc);
    }

    [Fact]
    public async Task Execute_AnexoV_ShouldNotifyStudents_WhenCurrentAdvisorSigns()
    {
        var advisor = new UserEntity { Id = 10, Name = "Prof Orientador", Email = "adv@test.com" };
        var student = new UserEntity { Id = 20, Name = "Aluno", Email = "student@test.com" };
        var advisorProfile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 5,
            Name = "ANEXO V - TERMO DE AFASTAMENTO",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile, studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 50,
            DocumentType = docType,
            DocumentTypeId = 5,
            FileName = "anexo_v",
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Afastamento",
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 10, User = advisor, Profile = advisorProfile },
                new() { UserId = 20, User = student, Profile = studentProfile }
            }
        };

        var advisorChange = new gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity
        {
            Id = 1,
            TccId = 1,
            DocumentId = 50,
            CurrentAdvisorId = 10,
            RequesterUserId = 10,
            RequesterRole = "ADVISOR",
            Status = "PENDING_SIGNATURES"
        };
        tcc.AdvisorChanges = new List<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity> { advisorChange };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(doc.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        var validPdf = CreateSignedPdfBytes("ANEXO V");
        var dto = new SignSignatureDTO(1, 50, 10, validPdf, 1, "application/pdf", "anexo_v.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(e => e.Recipient == "student@test.com"));
    }

    [Fact]
    public async Task Execute_ShouldNotifyNextUserInQueue_WhenSharedDocumentIsSigned()
    {
        var student1 = new UserEntity { Id = 20, Name = "Aluno 1", Email = "aluno1@test.com" };
        var student2 = new UserEntity { Id = 21, Name = "Aluno 2", Email = "aluno2@test.com" };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 1,
            Name = "ANEXO I - TERMO DE COMPROMISSO",
            SignatureOrder = 2,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 10,
            DocumentType = docType,
            DocumentTypeId = 1,
            FileName = "anexo_i",
            UserId = null, // Compartilhado
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Dupla",
            Step = StepTccType.START_AND_ORGANIZATION.ToString(),
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity>
            {
                new() { Id = 1, UserId = 20, User = student1, Profile = studentProfile },
                new() { Id = 2, UserId = 21, User = student2, Profile = studentProfile }
            }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(doc.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        var validPdf = CreateSignedPdfBytes("ANEXO I");
        // Aluno 1 assina o documento compartilhado
        var dto = new SignSignatureDTO(1, 10, 20, validPdf, 1, "application/pdf", "anexo_i.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        // O próximo aluno da fila (Aluno 2) deve receber a notificação
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(e => e.Recipient == "aluno2@test.com"));
    }

    [Fact]
    public async Task Execute_ShouldAdvanceToFinalizationAndNotifyStudent_WhenPresentationSignaturesCompleteWithApproval()
    {
        var advisor = new UserEntity { Id = 1, Name = "Prof Orientador", Email = "adv@test.com" };
        var student = new UserEntity { Id = 2, Name = "Estudante Aprovado", Email = "student@test.com" };
        var advisorProfile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 4,
            Name = "ANEXO IV - ATA DE DEFESA",
            SignatureOrder = 5,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile, studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 40,
            DocumentType = docType,
            DocumentTypeId = 4,
            FileName = "ata_aprovada",
            Signatures = new List<SignatureEntity>
            {
                new() { Id = 1, UserId = 1, User = advisor, DocumentId = 40 } // Orientador já assinou
            }
        };

        var tcc = new TccEntity
        {
            Id = 10,
            Title = "TCC Defendido",
            Step = StepTccType.PRESENTATION_AND_EVALUATION.ToString(),
            Status = StatusTccType.IN_PROGRESS.ToString(),
            Documents = new List<DocumentEntity> { doc },
            BankingMembers = new List<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity>
            {
                new("Membro 1", "m1@test.com", "Examinador", "tok1", 10) { Grade = 8.5m },
                new("Membro 2", "m2@test.com", "Examinador", "tok2", 10) { Grade = 9.0m }
            },
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 1, User = advisor, Profile = advisorProfile },
                new() { UserId = 2, User = student, Profile = studentProfile }
            }
        };

        _tccGateway.FindTccById(10).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(doc.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        var validPdf = CreateSignedPdfBytes("ANEXO IV");
        var dto = new SignSignatureDTO(10, 40, 2, validPdf, 1, "application/pdf", "ANEXO IV.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(StepTccType.FINALIZATION_AND_PUBLICATION.ToString(), tcc.Step);
        Assert.NotNull(tcc.FinalSubmissionDeadline);
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(e =>
            e.Recipient == student.Email &&
            e.TypeTemplate == "FINAL-SUBMISSION-NOTICE"));
    }

    [Fact]
    public async Task Execute_ShouldReturnInvalidFile_WhenTccStepIsInvalidOrNotMapped()
    {
        var student = new UserEntity { Id = 20, Name = "Aluno", Email = "student@test.com" };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 1,
            Name = "ANEXO I - TERMO",
            SignatureOrder = 2,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 10,
            DocumentType = docType,
            DocumentTypeId = 1,
            FileName = "anexo_i",
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = "ETAPA_INVALIDA",
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity> { new() { UserId = 20, User = student, Profile = studentProfile } }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });

        var validPdf = CreateSignedPdfBytes("ANEXO I");
        var dto = new SignSignatureDTO(1, 10, 20, validPdf, 1, "application/pdf", "anexo_i.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_NotifyCoordination_ShouldHandleMissingCourseAndGatewayException()
    {
        var (tcc, document, advisor) = CreatePendingAdvisorChange(suggestedAdvisorId: 6);
        // Aluno sem CampiCourseId
        foreach (var ut in tcc.UserTccs)
        {
            ut.User.CampiCourseId = null;
        }

        _tccGateway.FindTccById(tcc.Id).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { document.DocumentType });
        _storageGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var validPdf = CreateSignedPdfBytes("ANEXO IX");
        var dto = new SignSignatureDTO(tcc.Id, document.Id, advisor.Id, validPdf, 1, "application/pdf", "ANEXO IX.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        // Não envia e-mails para coordenação quando não tem curso associado
        await _emailGateway.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task Execute_NotifyCoordination_ShouldLogWarning_WhenNoActiveCoordinatorsFound()
    {
        var (tcc, document, advisor) = CreatePendingAdvisorChange(suggestedAdvisorId: 6);
        // Aluno com curso associado
        foreach (var ut in tcc.UserTccs)
        {
            ut.User.CampiCourseId = 1;
        }

        _tccGateway.FindTccById(tcc.Id).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { document.DocumentType });
        _storageGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);
        // Retorna lista vazia de coordenadores e supervisores
        _userGateway.FindAllByFilter(Arg.Any<UserFilterDTO>(), Arg.Any<long>()).Returns(new List<UserEntity>());

        var validPdf = CreateSignedPdfBytes("ANEXO IX");
        var dto = new SignSignatureDTO(tcc.Id, document.Id, advisor.Id, validPdf, 1, "application/pdf", "ANEXO IX.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        await _emailGateway.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task Execute_NotifyCoordination_ShouldHandleExceptionsGracefully()
    {
        var (tcc, document, advisor) = CreatePendingAdvisorChange(suggestedAdvisorId: 6);
        foreach (var ut in tcc.UserTccs)
        {
            ut.User.CampiCourseId = 1;
        }

        var coordinator = new UserEntity { Id = 3, Name = "Coordenação", Email = "coord@test.com", Status = "ACTIVE" };

        _tccGateway.FindTccById(tcc.Id).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { document.DocumentType });
        _storageGateway.Send(document.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        _userGateway.FindAllByFilter(Arg.Any<UserFilterDTO>(), Arg.Any<long>())
            .Returns(new List<UserEntity> { coordinator });

        // Simula falha ao disparar e-mail para a coordenação
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.FailureResult("Falha SMTP", 500));

        var validPdf = CreateSignedPdfBytes("ANEXO IX");
        var dto = new SignSignatureDTO(tcc.Id, document.Id, advisor.Id, validPdf, 1, "application/pdf", "ANEXO IX.pdf");

        var result = await _useCase.Execute(dto);

        // Deve capturar a falha de e-mail e ainda assim concluir a assinatura
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Execute_ShouldNotSendEmail_WhenNextUserInQueueIsBanking()
    {
        var advisor = new UserEntity { Id = 10, Name = "Orientador", Email = "advisor@test.com" };
        var bankingUser = new UserEntity { Id = 30, Name = "Membro Banca", Email = "banca@test.com" };
        var student = new UserEntity { Id = 20, Name = "Aluno", Email = "aluno@test.com" };

        var advisorProfile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var bankingProfile = new ProfileEntity { Id = 5, Role = RoleType.BANKING.ToString() };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 4,
            Name = "ANEXO IV - ATA DE DEFESA",
            SignatureOrder = 5,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile, bankingProfile, studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 40,
            DocumentType = docType,
            DocumentTypeId = 4,
            FileName = "anexo_iv",
            UserId = null,
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.PRESENTATION_AND_EVALUATION.ToString(),
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity>
            {
                new() { Id = 1, UserId = 10, User = advisor, Profile = advisorProfile },
                new() { Id = 2, UserId = 30, User = bankingUser, Profile = bankingProfile },
                new() { Id = 3, UserId = 20, User = student, Profile = studentProfile }
            },
            BankingMembers = new List<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity>
            {
                new("Banca", "banca@test.com", "Membro", "token", 1) { Grade = 8.0m }
            }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(doc.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var validPdf = CreateSignedPdfBytes("ANEXO IV");
        // O orientador (primeiro da fila) assina
        var dto = new SignSignatureDTO(1, 40, 10, validPdf, 1, "application/pdf", "ANEXO IV.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        // Não deve enviar e-mail pois o próximo da fila é BANKING (eles assinam na tela de avaliação)
        await _emailGateway.DidNotReceive().Send(Arg.Is<SendEmailDTO>(e => e.Recipient == "banca@test.com"));
    }

    [Fact]
    public async Task Execute_ShouldGetAdvisorFromAdvisorChanges_WhenAdvisorNotInUserTccs()
    {
        var currentAdvisor = new UserEntity { Id = 10, Name = "Prof Anterior", Email = "ant@test.com" };
        var student = new UserEntity { Id = 20, Name = "Aluno", Email = "aluno@test.com" };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 9,
            Name = "ANEXO IX - SUBSTITUIÇÃO",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { new() { Id = 1, Role = "ADVISOR" }, studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 90,
            DocumentType = docType,
            DocumentTypeId = 9,
            FileName = "anexo_ix",
            Signatures = new List<SignatureEntity>()
        };

        var advisorChange = new gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity
        {
            Id = 1,
            TccId = 1,
            DocumentId = 90,
            CurrentAdvisorId = 10,
            CurrentAdvisor = currentAdvisor,
            RequesterUserId = 20,
            RequesterRole = "STUDENT",
            Status = "PENDING_SIGNATURES"
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Documents = new List<DocumentEntity> { doc },
            AdvisorChanges = new List<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity> { advisorChange },
            UserTccs = new List<UserTccEntity>
            {
                // Note que o professor 10 NÃO está em UserTccs, está apenas em AdvisorChanges
                new() { Id = 1, UserId = 20, User = student, Profile = studentProfile }
            }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(doc.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var validPdf = CreateSignedPdfBytes("ANEXO IX");
        var dto = new SignSignatureDTO(1, 90, 10, validPdf, 1, "application/pdf", "ANEXO IX.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Single(doc.Signatures);
        Assert.Equal(10, doc.Signatures.First().UserId);
    }

    [Fact]
    public async Task Execute_ShouldValidateAndSign_WhenDocumentMethodIsNotOnlyDocs()
    {
        var student = new UserEntity { Id = 20, Name = "Aluno", Email = "student@test.com" };
        var advisor = new UserEntity { Id = 10, Name = "Orientador", Email = "advisor@test.com" };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };
        var advisorProfile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 3,
            Name = "ANEXO III - PROPOSTA",
            SignatureOrder = 2,
            MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile, advisorProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 30,
            DocumentType = docType,
            DocumentTypeId = 3,
            FileName = "anexo_iii",
            UserId = 20,
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.START_AND_ORGANIZATION.ToString(),
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 20, User = student, Profile = studentProfile },
                new() { UserId = 10, User = advisor, Profile = advisorProfile }
            }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _storageGateway.Send(doc.FileName, Arg.Any<byte[]>(), "application/pdf").Returns(Task.CompletedTask);

        var validPdf = CreateSignedPdfBytes("ANEXO III");
        var dto = new SignSignatureDTO(1, 30, 20, validPdf, 1, "application/pdf", "anexo_iii.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Single(doc.Signatures);
        await _tccGateway.Received().Update(tcc);
    }

    [Fact]
    public async Task Execute_ShouldReturnConflict_WhenAnotherStudentTriesToSignNotOnlyDocsDocument()
    {
        var student1 = new UserEntity { Id = 20, Name = "Aluno 1", Email = "student1@test.com" };
        var student2 = new UserEntity { Id = 21, Name = "Aluno 2", Email = "student2@test.com" };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 3,
            Name = "ANEXO III - PROPOSTA",
            SignatureOrder = 2,
            MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 30,
            DocumentType = docType,
            DocumentTypeId = 3,
            FileName = "anexo_iii",
            UserId = 20, // Pertence ao Aluno 1
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.START_AND_ORGANIZATION.ToString(),
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 20, User = student1, Profile = studentProfile },
                new() { UserId = 21, User = student2, Profile = studentProfile }
            }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });

        var validPdf = CreateSignedPdfBytes("ANEXO III");
        // Aluno 2 tenta assinar o documento do Aluno 1
        var dto = new SignSignatureDTO(1, 30, 21, validPdf, 1, "application/pdf", "anexo_iii.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnInvalidFile_WhenStudentAttemptsToSignAnexoVBeforeAdvisor()
    {
        var advisor = new UserEntity { Id = 10, Name = "Prof Orientador", Email = "adv@test.com" };
        var student = new UserEntity { Id = 20, Name = "Aluno", Email = "student@test.com" };
        var advisorProfile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 5,
            Name = "ANEXO V - TERMO DE AFASTAMENTO",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { advisorProfile, studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 50,
            DocumentType = docType,
            DocumentTypeId = 5,
            FileName = "anexo_v",
            Signatures = new List<SignatureEntity>() // Ninguém assinou ainda
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Afastamento",
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity>
            {
                new() { UserId = 10, User = advisor, Profile = advisorProfile },
                new() { UserId = 20, User = student, Profile = studentProfile }
            }
        };

        var advisorChange = new gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity
        {
            Id = 1,
            TccId = 1,
            DocumentId = 50,
            CurrentAdvisorId = 10,
            RequesterUserId = 10,
            RequesterRole = "ADVISOR",
            Status = "PENDING_SIGNATURES"
        };
        tcc.AdvisorChanges = new List<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity> { advisorChange };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });

        var validPdf = CreateSignedPdfBytes("ANEXO V");
        // Aluno tenta assinar antes do professor
        var dto = new SignSignatureDTO(1, 50, 20, validPdf, 1, "application/pdf", "anexo_v.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
        Assert.Contains("Erro ao realizar upload", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnInvalidFile_WhenSharedDocumentUserSignsOutOfOrder()
    {
        var student1 = new UserEntity { Id = 20, Name = "Aluno 1", Email = "aluno1@test.com" };
        var student2 = new UserEntity { Id = 21, Name = "Aluno 2", Email = "aluno2@test.com" };
        var studentProfile = new ProfileEntity { Id = 2, Role = RoleType.STUDENT.ToString() };

        var docType = new DocumentTypeEntity
        {
            Id = 1,
            Name = "ANEXO I - TERMO DE COMPROMISSO",
            SignatureOrder = 2,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        var doc = new DocumentEntity
        {
            Id = 10,
            DocumentType = docType,
            DocumentTypeId = 1,
            FileName = "anexo_i",
            UserId = null, // Compartilhado
            Signatures = new List<SignatureEntity>()
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Step = StepTccType.START_AND_ORGANIZATION.ToString(),
            Documents = new List<DocumentEntity> { doc },
            UserTccs = new List<UserTccEntity>
            {
                new() { Id = 1, UserId = 20, User = student1, Profile = studentProfile },
                new() { Id = 2, UserId = 21, User = student2, Profile = studentProfile }
            }
        };

        _tccGateway.FindTccById(1).Returns(tcc);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });

        var validPdf = CreateSignedPdfBytes("ANEXO I");
        // Aluno 2 tenta assinar antes do Aluno 1
        var dto = new SignSignatureDTO(1, 10, 21, validPdf, 1, "application/pdf", "anexo_i.pdf");

        var result = await _useCase.Execute(dto);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.ErrorDetails?.Status);
    }

    private static byte[] CreateSignedPdfBytes(string text = "DOC")
    {
        using var ms = new MemoryStream();
        using (var writer = new PdfWriter(ms))
        using (var pdfDoc = new PdfDocument(writer))
        {
            var page = pdfDoc.AddNewPage();
            
            var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(page);
            canvas.BeginText()
                  .SetFontAndSize(iText.Kernel.Font.PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA), 12)
                  .MoveText(100, 700)
                  .ShowText(text)
                  .EndText();

            var sigDict = new PdfDictionary();
            sigDict.Put(PdfName.Type, PdfName.Sig);
            sigDict.Put(PdfName.Filter, PdfName.Adobe_PPKLite);
            sigDict.Put(PdfName.SubFilter, PdfName.Adbe_pkcs7_detached);
            sigDict.Put(PdfName.ByteRange, new PdfArray(new int[] { 0, 100, 200, 100 }));
            sigDict.Put(PdfName.Contents, new PdfString("0000000000").SetHexWriting(true));
            sigDict.Put(PdfName.Name, new PdfString("Assinador"));
            sigDict.MakeIndirect(pdfDoc);

            var fieldDict = new PdfDictionary();
            fieldDict.Put(PdfName.FT, PdfName.Sig);
            fieldDict.Put(PdfName.T, new PdfString("SignatureField1"));
            fieldDict.Put(PdfName.V, sigDict);
            fieldDict.MakeIndirect(pdfDoc);

            var form = iText.Forms.PdfAcroForm.GetAcroForm(pdfDoc, true);
            form.GetPdfObject().GetAsArray(PdfName.Fields).Add(fieldDict);
        }
        return ms.ToArray();
    }
}
