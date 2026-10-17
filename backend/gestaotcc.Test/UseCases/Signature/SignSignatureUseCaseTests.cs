using System.IO;
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

    public SignSignatureUseCaseTests()
    {
        _useCase = new SignSignatureUseCase(_documentTypeGateway, _tccGateway, _storageGateway, _emailGateway, _logger);
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
