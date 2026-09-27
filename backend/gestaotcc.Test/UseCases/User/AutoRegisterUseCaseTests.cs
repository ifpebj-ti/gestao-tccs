using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.User;

public class AutoRegisterUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IProfileGateway _profileGateway = Substitute.For<IProfileGateway>();
    private readonly ICourseGateway _courseGateway = Substitute.For<ICourseGateway>();
    private readonly IEmailGateway _emailGateway = Substitute.For<IEmailGateway>();
    private readonly IDocumentTypeGateway _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
    private readonly IBcryptGateway _bcryptGateway = Substitute.For<IBcryptGateway>();
    private readonly IAppLoggerGateway<AutoRegisterUseCase> _logger = Substitute.For<IAppLoggerGateway<AutoRegisterUseCase>>();
    private readonly IAppLoggerGateway<CreateAccessCodeUseCase> _accessCodeLogger = Substitute.For<IAppLoggerGateway<CreateAccessCodeUseCase>>();
    private readonly CreateAccessCodeUseCase _createAccessCodeUseCase;
    private readonly AutoRegisterUseCase _useCase;

    public AutoRegisterUseCaseTests()
    {
        _createAccessCodeUseCase = new CreateAccessCodeUseCase(_accessCodeLogger);

        _useCase = new AutoRegisterUseCase(
            _userGateway,
            _tccGateway,
            _profileGateway,
            _courseGateway,
            _emailGateway,
            _documentTypeGateway,
            _createAccessCodeUseCase,
            _bcryptGateway,
            _logger);
    }

    [Fact]
    public async Task Execute_ShouldRegisterStudentSuccessfully_WhenNoInviteExists()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Maria Silva",
            "maria.silva@discente.ifpe.edu.br",
            "20241TADS001",
            "123.456.789-00",
            "81999999999",
            "2024.1",
            ShiftType.MORNING,
            CourseId: 1,
            CampiId: 1,
            Password: "SecurePassword123!");

        var validInvite = new TccInviteEntity
        {
            Id = 1,
            Email = dto.Email,
            Code = "123456",
            CampiId = 1,
            CourseId = 1,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(15)
        };
        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(validInvite));

        var course = new CourseEntity { Id = 1, Name = "TADS" };
        var campi = new CampiEntity { Id = 1, Name = "Belo Jardim" };
        var campiCourse = new CampiCourseEntity { Id = 1, CampiId = 1, CourseId = 1, Course = course, Campi = campi };
        _courseGateway.FindByCampiAndCourseId(1, 1).Returns(Task.FromResult(campiCourse));

        var studentProfile = new ProfileEntity { Id = 1, Role = "STUDENT" };
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { studentProfile }));
        _bcryptGateway.GenerateHashPassword(dto.Password!).Returns("hashed_password_xyz");
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.Execute(dto, "secret_salt");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(dto.Email, result.Data.Email);
        Assert.Equal("hashed_password_xyz", result.Data.Password);
        await _userGateway.Received(1).Save(Arg.Is<UserEntity>(u => u.Email == dto.Email && u.Password == "hashed_password_xyz"));
    }

    [Fact]
    public async Task Execute_ShouldRegisterStudentAndLinkTcc_WhenInviteExists()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "João Santos",
            "joao.santos@discente.ifpe.edu.br",
            "20241TADS002",
            "987.654.321-11",
            "81988888888",
            "2024.1",
            ShiftType.MORNING,
            Password: "AnotherPassword123!");

        var invite = new TccInviteEntity
        {
            Id = 10,
            Email = dto.Email,
            Code = "ABC123",
            CampiId = 1,
            CourseId = 1,
            TccId = 42,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(15)
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(invite));

        var course = new CourseEntity { Id = 1, Name = "TADS" };
        var campi = new CampiEntity { Id = 1, Name = "Belo Jardim" };
        var campiCourse = new CampiCourseEntity { Id = 1, CampiId = 1, CourseId = 1, Course = course, Campi = campi };
        _courseGateway.FindByCampiAndCourseId(1, 1).Returns(Task.FromResult(campiCourse));

        var studentProfile = new ProfileEntity { Id = 1, Role = "STUDENT" };
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { studentProfile }));
        _profileGateway.FindByRole("STUDENT").Returns(Task.FromResult<ProfileEntity?>(studentProfile));
        _bcryptGateway.GenerateHashPassword(dto.Password!).Returns("hashed_password_abc");
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);

        var tcc = new TccEntity
        {
            Id = 42,
            Title = "Sistema de TCC",
            TccInvites = new List<TccInviteEntity> { invite },
            UserTccs = new List<gestaotcc.Domain.Entities.UserTcc.UserTccEntity>()
        };
        _tccGateway.FindTccById(42).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<gestaotcc.Domain.Dtos.Email.SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        // Act
        var result = await _useCase.Execute(dto, "secret_salt");

        // Assert
        Assert.True(result.IsSuccess);
        await _userGateway.Received(1).Save(Arg.Any<UserEntity>());
        await _tccGateway.Received(1).Update(Arg.Is<TccEntity>(t => t.Id == 42));
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenStudentAlreadyRegistered()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Maria Silva",
            "maria.silva@discente.ifpe.edu.br",
            "20241TADS001",
            "123.456.789-00",
            "81999999999",
            "2024.1",
            ShiftType.MORNING);

        var existingUser = new UserEntity { Id = 99, Email = dto.Email };
        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(existingUser));

        // Act
        var result = await _useCase.Execute(dto, "secret_salt");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.ErrorDetails?.Status);
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
    }
}
