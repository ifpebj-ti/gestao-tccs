using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.TccBankingMember;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class FindDocumentUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IITextGateway _iTextGateway = Substitute.For<IITextGateway>();
    private readonly FindDocumentUseCase _useCase;
    private readonly IAppLoggerGateway<FindDocumentUseCase> _logger = Substitute.For<IAppLoggerGateway<FindDocumentUseCase>>();

    [Theory]
    [InlineData("legacy-participant", "Pessoa legada|Pessoa legada|Campus do orientador|Cidade do orientador|Curso do orientador")]
    [InlineData("no-student", "||Belo Jardim|Belo Jardim|")]
    [InlineData("student-with-sparse-campus", "||Belo Jardim|Belo Jardim|")]
    public async Task Execute_RendersLegacyTemplatesWithIncompleteParticipantData(string scenario, string expected)
    {
        var prefix = $"legacy{Guid.NewGuid():N}";
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents");
        Directory.CreateDirectory(directory);
        var templatePath = Path.Combine(directory, prefix + "-old.html");
        var advisor = new UserEntity { Name = null!, Email = null!, Phone = null, Registration = null, SIAPE = null, Titration = null };
        var advisorLink = new UserTccEntity { UserId = 10, User = advisor, Profile = new ProfileEntity { Role = "ADVISOR" } };
        var examinerLink = new UserTccEntity { UserId = 11, User = new UserEntity(), Profile = new ProfileEntity { Role = "BANKING" } };
        var student = new UserEntity
        {
            Name = null!, Email = null!, UserClass = null, Shift = null, Phone = null, Registration = null
        };
        var project = new TccEntity
        {
            Id = 1,
            CreationDate = new DateTime(2026, 4, 1),
            UserTccs = [advisorLink, examinerLink],
            AdvisorChanges = null!,
            Documents =
            [
                new DocumentEntity { Id = 3, DocumentType = new DocumentTypeEntity { Name = prefix + "-current" }, Signatures = [] }
            ]
        };
        if (scenario == "legacy-participant")
        {
            advisor.CampiCourse = new CampiCourseEntity
            {
                Campi = new CampiEntity { Name = "Campus do orientador", City = "Cidade do orientador" },
                Course = new CourseEntity { Name = "Curso do orientador" }
            };
            student.Name = "Pessoa legada";
            project.UserTccs.Add(new UserTccEntity { UserId = 12, User = student, Profile = new ProfileEntity { Role = "USER" } });
        }
        if (scenario == "student-with-sparse-campus")
        {
            student.CampiCourse = new CampiCourseEntity
            {
                Campi = new CampiEntity { Name = null!, City = null! },
                Course = new CourseEntity { Name = null! }
            };
            advisor.CampiCourse = student.CampiCourse;
            project.UserTccs.Add(new UserTccEntity { UserId = 12, User = student, Profile = new ProfileEntity { Role = "STUDENT" } });
            project.AdvisorChanges =
            [
                new TccAdvisorChangeEntity
                {
                    DocumentId = 3,
                    CurrentAdvisor = advisor,
                    SuggestedAdvisor = new UserEntity { Name = null!, SIAPE = null, Registration = null }
                }
            ];
            project.TccSchedule = new TccScheduleEntity { ScheduledDate = new DateTime(2026, 4, 7), Location = null! };
            project.BankingMembers = [new TccBankingMemberEntity("Bia", "bia@example.org", "Externo", "test-token", 1) { Grade = 7m }];
        }
        _tccGateway.FindTccById(1).Returns(project);
        _userGateway.FindAllByFilter(Arg.Any<UserFilterDTO>(), 1).Returns(new List<UserEntity>
        {
            new() { Profile = [new ProfileEntity { Role = "SUPERVISOR" }] }
        });

        await File.WriteAllTextAsync(templatePath,
            "{{nome_orientando}}|{{orientandos}}|{{campus_nome}}|{{campus_cidade}}|{{curso_nome}};" +
            "{{ano_orientando}};{{orientador_substituto_nome}};{{orientador_substituto_siape}};{{telefone_orientador}}");
        try
        {
            var result = await _useCase.Execute(1, 3, null, 1);

            Assert.True(result.IsSuccess);
            Assert.True(result.Data.IsHtml);
            Assert.Equal(expected + ";2026.1;A definir;A definir;", result.Data.Url);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }

    public FindDocumentUseCaseTests()
    {
        _useCase = new FindDocumentUseCase(_tccGateway, _storageGateway, _userGateway, _iTextGateway, _logger);
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
        _userGateway.FindAllByFilter(Arg.Any<UserFilterDTO>(), Arg.Any<long>()).Returns(new List<UserEntity>());
        _storageGateway.Download("signed-doc.pdf", true).Returns(Task.FromResult(new byte[] { 1, 2, 3 }));

        var result = await _useCase.Execute(1, 1, 1, 1, true);

        Assert.True(result.IsSuccess);
        Assert.Equal("AQID", result.Data.Url);
    }

    [Fact]
    public async Task Execute_ShouldReturnTemplateDocumentUrl_WhenDocumentIsNotSigned()
    {
        var documentType = new DocumentTypeEntity { Id = 1, Name = "Proposal" };

        var profile = new ProfileEntity
        {
            Id = 1,
            Role = RoleType.SUPERVISOR.ToString()
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

        // Cria o arquivo de template fake para o teste conseguir ler
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "default-template.html"), "<html>fake template</html>");

        _tccGateway.FindTccById(1).Returns(tcc);
        _userGateway.FindAllByFilter(Arg.Is<UserFilterDTO>(u => u.Profile == RoleType.SUPERVISOR.ToString()), Arg.Any<long>())
            .Returns(new List<UserEntity> { supervisorUser });
            
        // Novos mocks para iText e Download do Storage
        _iTextGateway.ConvertHtmlToPdf(Arg.Any<string>()).Returns(Task.FromResult(new byte[] { 4, 5, 6 }));
        _storageGateway.Download(Arg.Any<string>(), true).Returns(Task.FromResult(new byte[] { 4, 5, 6 }));

        var result = await _useCase.Execute(1, 1, 1, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("<html>fake template</html>", result.Data.Url);
        Assert.True(result.Data.IsHtml);
    }

    [Fact]
    public async Task Execute_ShouldProcessBankingMembersEvaluations_WhenDocumentNotSigned()
    {
        var documentType = new DocumentTypeEntity { Id = 2, Name = "ANEXO IV - FICHA AVALIATIVA" };

        var document = new DocumentEntity
        {
            Id = 2,
            DocumentType = documentType,
            DocumentTypeId = documentType.Id,
            FileName = "ficha-avaliativa",
            Signatures = new List<SignatureEntity>() // Não assinado
        };

        var studentProfile = new ProfileEntity { Id = 1, Role = RoleType.STUDENT.ToString() };
        var studentUser = new UserEntity { Id = 10, Name = "Aluno Teste", Profile = new List<ProfileEntity> { studentProfile } };

        var evalJson1 = "{\"oral\":{\"apresentacao\":\"4.5\",\"clareza\":\"4.0\"},\"textual\":{\"estrutura\":\"4.0\"}}";
        var evalJson2 = "{\"oral\":{\"apresentacao\":\"5.0\",\"clareza\":\"4.5\"},\"textual\":{\"estrutura\":\"4.5\"}}";

        var member1 = new gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity("Examinador 1", "ex1@test.com", "Examinador Interno", "tok1", 1)
        {
            Grade = 8.5m,
            EvaluationDetails = evalJson1
        };
        var member2 = new gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity("Examinador 2", "ex2@test.com", "Examinador Externo", "tok2", 1)
        {
            Grade = 9.5m,
            EvaluationDetails = evalJson2
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Avaliado",
            Documents = new List<DocumentEntity> { document },
            UserTccs = new List<UserTccEntity>
            {
                new() { User = studentUser, Profile = studentProfile, UserId = 10 }
            },
            BankingMembers = new List<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity> { member1, member2 }
        };

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "default-template.html"), "<html>{{oral_1}} {{textual_1}} {{oral_2}}</html>");

        _tccGateway.FindTccById(1).Returns(tcc);
        _userGateway.FindAllByFilter(Arg.Any<UserFilterDTO>(), Arg.Any<long>()).Returns(new List<UserEntity>());

        var result = await _useCase.Execute(1, 2, 10, 1, returnSignedPdfIfAvailable: true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data.IsHtml);
        Assert.NotNull(result.Data.Url);
    }

    [Theory]
    [InlineData(false, "<html>assinado</html>")]
    [InlineData(true, "<!DOCTYPE html><html>assinado</html>")]
    public async Task Execute_RecognizesSignedHtmlFromStorage(bool useLegacyFallback, string html)
    {
        var document = new DocumentEntity
        {
            Id = 4,
            DocumentType = new DocumentTypeEntity(),
            FileName = "signed-minutes",
            Signatures = [new SignatureEntity { DocumentId = 4 }]
        };
        _tccGateway.FindTccById(1).Returns(new TccEntity { Documents = [document] });
        var bytes = System.Text.Encoding.UTF8.GetBytes(html);
        if (useLegacyFallback)
        {
            _storageGateway.Download("signed-minutes.pdf", true)
                .Returns(Task.FromException<byte[]>(new FileNotFoundException()));
            _storageGateway.Download("signed-minutes.html", true).Returns(bytes);
        }
        else
        {
            _storageGateway.Download("signed-minutes.pdf", true).Returns(bytes);
        }

        var result = await _useCase.Execute(1, 4, null, 1, returnSignedPdfIfAvailable: true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data.IsHtml);
        Assert.Equal(html, System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result.Data.Url)));
        await _storageGateway.Received(useLegacyFallback ? 1 : 0).Download("signed-minutes.html", true);
    }

    [Theory]
    [InlineData(true, "older-document", "<p>Campus Jardim|2026.2|18/11/2026|Laboratorio|Selected &amp; Student|Original Advisor|Next Advisor|S123|ambio|Reprovado")]
    [InlineData(false, "newer-document", "<p>Campus Jardim|2026.2|18/11/2026|Laboratorio|Selected &amp; Student|Original Advisor|Next Advisor|R456|ambio|Reprovado")]
    public async Task Execute_RendersScheduleAndAdvisorChangeUsingExplicitOrLatestDocument(
        bool hasMatchingAdvisorChange, string documentReason, string expected)
    {
        var templateName = $"coverage-document-{Guid.NewGuid():N}";
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Documents");
        Directory.CreateDirectory(directory);
        var templatePath = Path.Combine(directory, templateName + ".html");
        var documentType = new DocumentTypeEntity { Name = templateName };
        var advisorProfile = new ProfileEntity { Role = RoleType.ADVISOR.ToString() };
        var studentProfile = new ProfileEntity { Role = RoleType.STUDENT.ToString() };
        var supervisorProfile = new ProfileEntity { Role = RoleType.SUPERVISOR.ToString() };
        var campusCourse = new CampiCourseEntity
        {
            Campi = new CampiEntity { Name = "Campus Jardim", City = "Belo Jardim" },
            Course = new CourseEntity { Name = "Computacao" }
        };
        var advisor = new UserEntity
        {
            Name = "Original Advisor", CampiCourse = campusCourse, Email = "advisor@example.org",
            Phone = "1111", Titration = "Doutor", Registration = "A123"
        };
        var student = new UserEntity
        {
            Name = "Selected & Student", CampiCourse = campusCourse, Email = "student@example.org",
            Phone = "2222", UserClass = "3A", Shift = "Manha", Registration = "2026001"
        };
        var substitute = new UserEntity
        {
            Name = "Next Advisor",
            SIAPE = hasMatchingAdvisorChange ? "S123" : null,
            Registration = "R456"
        };
        var tcc = new TccEntity
        {
            Title = new string('T', 78) + "ambio",
            CreationDate = new DateTime(2026, 8, 1),
            TccSchedule = new TccScheduleEntity { ScheduledDate = new DateTime(2026, 11, 18, 14, 30, 0), Location = "Laboratorio" },
            Documents = [new DocumentEntity { Id = 4, DocumentType = documentType, FileName = "minutes", Signatures = [] }],
            UserTccs =
            [
                new UserTccEntity { UserId = 10, User = advisor, Profile = advisorProfile },
                new UserTccEntity { UserId = 20, User = new UserEntity { Name = "Other Student" }, Profile = studentProfile },
                new UserTccEntity { UserId = 21, User = student, Profile = studentProfile }
            ],
            AdvisorChanges =
            [
                new TccAdvisorChangeEntity
                {
                    DocumentId = hasMatchingAdvisorChange ? 4 : 9, CreatedAt = new DateTime(2026, 9, 1),
                    CurrentAdvisor = advisor, SuggestedAdvisor = substitute, Reason = "older-document"
                },
                new TccAdvisorChangeEntity
                {
                    DocumentId = 8, CreatedAt = new DateTime(2026, 9, 2),
                    SuggestedAdvisor = substitute, Reason = "newer-document"
                }
            ],
            BankingMembers =
            [
                new TccBankingMemberEntity("Avaliador 1", "one@example.org", "Externo", "one", 1)
                {
                    Grade = 6m, EvaluationDetails = """{"oral":{"postura":"2,5","articulacao":"invalida"}}"""
                },
                new TccBankingMemberEntity("Avaliador 2", "two@example.org", "Externo", "two", 1)
                {
                    Grade = 5m, EvaluationDetails = """{"textual":{"coerencia":"invalida","conclusoes":null}}"""
                },
                new TccBankingMemberEntity("Avaliador 3", "three@example.org", "Externo", "three", 1)
                {
                    Grade = 6m, EvaluationDetails = "{"
                },
                new TccBankingMemberEntity("Pendente", "pending@example.org", "Externo", "pending", 1)
            ]
        };
        _tccGateway.FindTccById(1).Returns(tcc);
        _userGateway.FindAllByFilter(Arg.Any<UserFilterDTO>(), 1).Returns(new List<UserEntity>
        {
            new() { Profile = [new ProfileEntity { Role = RoleType.COORDINATOR.ToString() }] },
            new() { Profile = [new ProfileEntity { Role = RoleType.ADMIN.ToString() }] },
            new() { Profile = [studentProfile, supervisorProfile], CampiCourse = campusCourse }
        });

        await File.WriteAllTextAsync(templatePath,
            "<p>{{campus_nome}}|{{ano_orientando}}|{{data_apresentacao}}|{{local_apresentacao}}|{{nome_orientando}}|{{orientador_nome}}|{{orientador_substituto_nome}}|{{orientador_substituto_siape}}|{{titulo_tcc_2}}|{{parecer_final}}|{{motivo_substituicao}}|{{curso_supervisor}}</p>");
        try
        {
            var result = await _useCase.Execute(1, 4, 21, 1);

            Assert.True(result.IsSuccess);
            Assert.True(result.Data.IsHtml);
            Assert.StartsWith(expected, result.Data.Url);
            Assert.EndsWith($"|{documentReason}|Computacao</p>", result.Data.Url);
        }
        finally
        {
            File.Delete(templatePath);
        }
    }
}
