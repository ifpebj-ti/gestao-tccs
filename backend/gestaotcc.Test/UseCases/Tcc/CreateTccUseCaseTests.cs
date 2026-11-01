using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Semester;
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
    private readonly ISemesterGateway _semesterGateway = Substitute.For<ISemesterGateway>();
    private readonly IAppLoggerGateway<CreateTccUseCase> _logger = Substitute.For<IAppLoggerGateway<CreateTccUseCase>>();

    private readonly CreateTccUseCase _useCase;

    public CreateTccUseCaseTests()
    {
        _useCase = new CreateTccUseCase(_userGateway, _tccGateway, _emailGateway, _documentTypeGateway, _semesterGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnSuccess_WhenTccIsCreated()
    {
        // Arrange
        var studentEmail = "student@example.com";
        var campiCourse = new CampiCourseEntityBuilder()
            .WithCampiId(1)
            .WithCourseId(1)
            .Build();
        
        var advisor = new UserEntity
        {
            Id = 100,
            Name = "Advisor",
            Email = "advisor@example.com",
            Profile = new List<ProfileEntity>
            {
                new ProfileEntity { Role = RoleType.ADVISOR.ToString() }
            },
            CampiCourse = campiCourse
        };

        var student = new UserEntity
        {
            Id = 1,
            Name = "Student",
            Email = studentEmail,
            Profile = new List<ProfileEntity>
            {
                new ProfileEntity { Role = RoleType.STUDENT.ToString() }
            },
            CampiCourse = campiCourse
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

        var activeSemester = new SemesterEntity { Id = 1, Name = "2026.2", IsActive = true };

        _userGateway.FindAllByEmail(Arg.Any<List<string>>()).Returns(new List<UserEntity> { student });
        _userGateway.FindById(Arg.Any<long>()).Returns(advisor);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity> { docType });
        _semesterGateway.FindActive().Returns(new List<SemesterEntity> { activeSemester });
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(ResultPattern<bool>.SuccessResult(true));

        var dto = new CreateTccDTO(new List<StudentsToCreateTccDTO> { new StudentsToCreateTccDTO(studentEmail, 1)  }, "Titulo TCC", "Resumo TCC", advisor.Id);

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

        var dto = new CreateTccDTO(new List<StudentsToCreateTccDTO> { new StudentsToCreateTccDTO("student@example.com", 1)  }, "Titulo", "Resumo", 999);

        var result = await _useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        Assert.Equal("Erro ao criar tcc", result.Message);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenNoActiveSemesterExists()
    {
        // Arrange
        var campiCourse = new CampiCourseEntityBuilder().WithCampiId(1).WithCourseId(1).Build();
        var advisor = new UserEntity
        {
            Id = 100,
            Name = "Advisor",
            Email = "advisor@example.com",
            Profile = new List<ProfileEntity> { new ProfileEntity { Role = RoleType.ADVISOR.ToString() } },
            CampiCourse = campiCourse
        };

        _userGateway.FindAllByEmail(Arg.Any<List<string>>()).Returns(new List<UserEntity>());
        _userGateway.FindById(Arg.Any<long>()).Returns(advisor);
        _documentTypeGateway.FindAll().Returns(new List<DocumentTypeEntity>());
        _semesterGateway.FindActive().Returns(new List<SemesterEntity>()); // Nenhum semestre ativo

        var dto = new CreateTccDTO(new List<StudentsToCreateTccDTO> { new StudentsToCreateTccDTO("student@example.com", 1) }, "Titulo", "Resumo", advisor.Id);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(422, result.ErrorDetails?.Status);
        await _tccGateway.DidNotReceive().Save(Arg.Any<TccEntity>());
    }
}