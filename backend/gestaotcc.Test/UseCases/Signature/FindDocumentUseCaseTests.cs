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
    private readonly IStorageGateway _storageGateway = Substitute.For<IStorageGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly IITextGateway _iTextGateway = Substitute.For<IITextGateway>();
    private readonly FindDocumentUseCase _useCase;
    private readonly IAppLoggerGateway<FindDocumentUseCase> _logger = Substitute.For<IAppLoggerGateway<FindDocumentUseCase>>();

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
}