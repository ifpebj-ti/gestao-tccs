using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class CreateTccUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();

    private readonly CreateTccUseCase _useCase;

    public CreateTccUseCaseTests()
    {
        _useCase = new CreateTccUseCase(_userGateway, _tccGateway, _emailGateway, _documentTypeGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenTccIsCreated()
    {
        // Arrange
        var studentEmail = "student@example.com";
        var advisor = new UserEntity
        {
            Id = 100,
            Name = "Advisor",
            Email = "advisor@example.com",
            Profile = new List<ProfileEntity>
            {
                new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
            }
        };

        var student = new UserEntity
        {
            Id = 1,
            Name = "Student",
            Email = studentEmail,
            Profile = new List<ProfileEntity>
            {
                new ProfileEntity { Role = RoleType.STUDENT.ToString() }
            }
        };

        var docType = new DocumentTypeEntity
        {
            Id = 1,
            Name = "Document",
            MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity>
            {
                new ProfileEntity { Role = RoleType.STUDENT.ToString() },
                new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
            }
        };

        _userGateway.FindAllByEmail(Arg.Any<List<string>>()).Returns(new List<UserEntity> { student });
        _userGateway.FindById(Arg.Any<long>()).Returns(advisor);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });

        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        var dto = new CreateTccDTO(new List<string> { studentEmail }, "Titulo TCC", "Resumo TCC", advisor.Id);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.True(result.IsSuccess);
        await _tccGateway.Received(1).Save(Arg.Any<TccEntity>());
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(x => x.Recipient == studentEmail));
        await _emailGateway.Received().Send(Arg.Is<SendEmailDTO>(x => x.Recipient == advisor.Email));
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenAdvisorNotFound()
    {
        _userGateway.FindById(Arg.Any<long>()).Returns((UserEntity?)null);

        var dto = new CreateTccDTO(new List<string> { "student@example.com" }, "Titulo", "Resumo", 999);

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao criar tcc", result.Message);
    }
}