using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Signature;

public class SendPendingSignatureUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly SendPendingSignatureUseCase _useCase;

    public SendPendingSignatureUseCaseTests()
    {
        _useCase = new SendPendingSignatureUseCase(_tccGateway, _emailGateway, _documentTypeGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenNoTccsFound()
    {
        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>()).Returns(new List<TccEntity>());

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        await _emailGateway.DidNotReceive().Send(Arg.Any<SendEmailDTO>());
    }

    [Fact]
    public async Task Execute_ShouldNotifyUser_WhenDocumentIsPendingSignature_ONLY_DOCS()
    {
        // Arrange
        var profile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var user = new UserEntity { Id = 1, Name = "Advisor", Email = "advisor@email.com" };

        var userTcc = new UserTccEntity
        {
            User = user,
            Profile = profile
        };

        var docType = new DocumentTypeEntity
        {
            Id = 10,
            Name = "DocType1",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { profile }
        };

        var document = new DocumentEntity
        {
            Id = 101,
            FileName = "file",
            DocumentType = docType,
            DocumentTypeId = docType.Id,
            Signatures = new List<SignatureEntity>(),
            User = user,
            UserId = user.Id
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Teste",
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity> { userTcc },
            Documents = new List<DocumentEntity> { document }
        };
        userTcc.Tcc = tcc;

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>()).Returns(new List<TccEntity> { tcc });
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        // Act
        var result = await _useCase.Execute();

        // Assert
        Assert.True(result.IsSuccess);
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(dto =>
            dto.Recipient == user.Email &&
            dto.TypeTemplate == "SEND-PENDING-SIGNATURE" &&
            dto.Variables.ContainsKey("details")
        ));
    }

    [Fact]
    public async Task Execute_ShouldNotifyCorrectUser_WhenDocumentIsShared_ONLY_DOCS()
    {
        var profile = new ProfileEntity { Id = 1, Role = RoleType.ADVISOR.ToString() };
        var user = new UserEntity { Id = 1, Name = "Advisor", Email = "advisor@email.com" };
        var userTcc = new UserTccEntity { User = user, Profile = profile };

        var docType = new DocumentTypeEntity
        {
            Id = 10,
            Name = "DocType1",
            SignatureOrder = 3,
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { profile }
        };

        var document = new DocumentEntity
        {
            Id = 101,
            DocumentType = docType,
            DocumentTypeId = docType.Id,
            FileName = "doc-shared",
            Signatures = new List<SignatureEntity>(),
            UserId = null // Documento compartilhado
        };

        var tcc = new TccEntity
        {
            Id = 1,
            Title = "TCC Compartilhado",
            Step = StepTccType.DEVELOPMENT_AND_MONITORING.ToString(),
            Status = StatusTccType.IN_PROGRESS.ToString(),
            UserTccs = new List<UserTccEntity> { userTcc },
            Documents = new List<DocumentEntity> { document }
        };
        userTcc.Tcc = tcc;

        _tccGateway.FindAllTccByFilter(Arg.Any<TccFilterDTO>()).Returns(new List<TccEntity> { tcc });
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        var result = await _useCase.Execute();

        Assert.True(result.IsSuccess);
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(dto =>
            dto.Recipient == user.Email &&
            dto.Variables.ContainsKey("details") &&
            ((List<SendPendingSignatureDetailsDTO>)dto.Variables["details"]!).Any(d => d.DocumentName == "DocType1")
        ));
    }
}