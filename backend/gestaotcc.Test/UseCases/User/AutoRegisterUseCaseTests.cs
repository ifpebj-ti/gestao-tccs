using System;
using System.Collections.Generic;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Document;
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

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenInviteIsNull()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Carlos Nogueira",
            "carlos@discente.ifpe.edu.br",
            "20241TADS003",
            "111.222.333-44",
            "81977776666",
            "2024.1",
            ShiftType.NIGHT);

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(null));

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenInviteIsAlreadyValidated()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Ana Lima",
            "ana@discente.ifpe.edu.br",
            "20241TADS004",
            "222.333.444-55",
            "81966665555",
            "2024.1",
            ShiftType.MORNING);

        // IsValidCode = true significa que o código JÁ foi usado -> inválido para novo cadastro
        var alreadyUsedInvite = new TccInviteEntity
        {
            Email = dto.Email,
            IsValidCode = true,
            ExpirationDate = DateTime.UtcNow.AddMinutes(10)
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(alreadyUsedInvite));

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenInviteIsExpired()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Pedro Costa",
            "pedro@discente.ifpe.edu.br",
            "20241TADS005",
            "333.444.555-66",
            "81955554444",
            "2024.1",
            ShiftType.AFTERNOON);

        var expiredInvite = new TccInviteEntity
        {
            Email = dto.Email,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(-30) // Expirado
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(expiredInvite));

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenCampiCourseNotFoundAndNoCampisExist()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Fernanda Rocha",
            "fernanda@discente.ifpe.edu.br",
            "20241TADS006",
            "444.555.666-77",
            "81944443333",
            "2024.1",
            ShiftType.MORNING,
            CourseId: 99,
            CampiId: 99);

        var validInvite = new TccInviteEntity
        {
            Email = dto.Email,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(10),
            CampiId = 99,
            CourseId = 99
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(validInvite));
        // Curso/campi específico não encontrado
        _courseGateway.FindByCampiAndCourseId(99, 99).Returns(Task.FromResult<CampiCourseEntity>(null!));
        // Fallback: lista de campi também sem cursos
        _courseGateway.FindAllCampis().Returns(Task.FromResult(new List<gestaotcc.Domain.Entities.Campi.CampiEntity>()));

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_ShouldRegisterWithFallbackCampiCourse_WhenSpecificCourseNotFound()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Luiza Peixoto",
            "luiza@discente.ifpe.edu.br",
            "20241TADS007",
            "555.666.777-88",
            "81933332222",
            "2024.1",
            ShiftType.MORNING,
            CourseId: 50,
            CampiId: 50);

        var validInvite = new TccInviteEntity
        {
            Email = dto.Email,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(10),
            CampiId = 50,
            CourseId = 50
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(validInvite));

        // Curso específico não encontrado
        _courseGateway.FindByCampiAndCourseId(50, 50).Returns(Task.FromResult<CampiCourseEntity>(null!));

        // Fallback: retorna um campi com um curso disponível
        var fallbackCampiCourse = new CampiCourseEntity { Id = 1, CampiId = 1, CourseId = 1 };
        var fallbackCampi = new gestaotcc.Domain.Entities.Campi.CampiEntity
        {
            Id = 1, Name = "Campus Padrão",
            CampiCourses = new List<CampiCourseEntity> { fallbackCampiCourse }
        };
        _courseGateway.FindAllCampis().Returns(Task.FromResult(new List<gestaotcc.Domain.Entities.Campi.CampiEntity> { fallbackCampi }));

        var studentProfile = new ProfileEntity { Id = 1, Role = "STUDENT" };
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { studentProfile }));
        _bcryptGateway.GenerateHashPassword(Arg.Any<string>()).Returns("hashed");
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.True(result.IsSuccess);
        await _userGateway.Received(1).Save(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_ShouldRegisterWithoutPassword_WhenPasswordIsNull()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Rafael Mendes",
            "rafael@discente.ifpe.edu.br",
            "20241TADS008",
            "666.777.888-99",
            "81922221111",
            "2024.1",
            ShiftType.MORNING,
            CourseId: 1,
            CampiId: 1,
            Password: null); // Sem senha

        var validInvite = new TccInviteEntity
        {
            Email = dto.Email,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(10),
            CampiId = 1,
            CourseId = 1
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(validInvite));

        var campiCourse = new CampiCourseEntity { Id = 1, CampiId = 1, CourseId = 1 };
        _courseGateway.FindByCampiAndCourseId(1, 1).Returns(Task.FromResult(campiCourse));

        var studentProfile = new ProfileEntity { Id = 1, Role = "STUDENT" };
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { studentProfile }));
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.True(result.IsSuccess);
        // Sem senha informada, o UserFactory gera uma senha temporária automática
        _bcryptGateway.DidNotReceive().GenerateHashPassword(Arg.Any<string>());
        Assert.False(string.IsNullOrWhiteSpace(result.Data?.Password));
    }

    [Fact]
    public async Task Execute_ShouldAdvanceTccStep_WhenAllInvitesAccepted()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Carla Nunes",
            "carla@discente.ifpe.edu.br",
            "20241TADS009",
            "777.888.999-00",
            "81911110000",
            "2024.1",
            ShiftType.MORNING,
            Password: "SenhaSegura@456");

        var invite = new TccInviteEntity
        {
            Id = 20,
            Email = dto.Email,
            Code = "XYZ789",
            CampiId = 1,
            CourseId = 1,
            TccId = 55,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(15)
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(invite));

        var campiCourse = new CampiCourseEntity { Id = 1, CampiId = 1, CourseId = 1 };
        _courseGateway.FindByCampiAndCourseId(1, 1).Returns(Task.FromResult(campiCourse));

        var studentProfile = new ProfileEntity { Id = 1, Role = "STUDENT" };
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { studentProfile }));
        _profileGateway.FindByRole("STUDENT").Returns(Task.FromResult<ProfileEntity?>(studentProfile));
        _bcryptGateway.GenerateHashPassword(dto.Password!).Returns("hashed_carla");
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);

        // Todos os convites do TCC já foram aceitos (nenhum IsValidCode=false restando)
        // O próprio convite da Carla também já foi marcado como false (aceito), então nenhum pendente
        var tcc = new TccEntity
        {
            Id = 55,
            Title = "TCC da Turma 2024.1",
            TccInvites = new List<TccInviteEntity>
            {
                invite // Único convite, IsValidCode=false => allIAlreadyAdded=false => step avança
            },
            UserTccs = new List<gestaotcc.Domain.Entities.UserTcc.UserTccEntity>()
        };

        _tccGateway.FindTccById(55).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<gestaotcc.Domain.Dtos.Email.SendEmailDTO>())
            .Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.True(result.IsSuccess);
        // O TCC deve ter avançado para START_AND_ORGANIZATION
        Assert.Equal(gestaotcc.Domain.Enums.StepTccType.START_AND_ORGANIZATION.ToString(), tcc.Step);
        await _tccGateway.Received(1).Update(Arg.Is<TccEntity>(t => t.Id == 55));
    }

    [Fact]
    public async Task Execute_ShouldNotAdvanceTccStep_WhenOtherInvitesPending()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Bruno Farias",
            "bruno@discente.ifpe.edu.br",
            "20241TADS010",
            "888.999.000-11",
            "81900001111",
            "2024.1",
            ShiftType.MORNING,
            Password: "BrunoSenha@789");

        var inviteBruno = new TccInviteEntity
        {
            Id = 30,
            Email = dto.Email,
            Code = "BRUN00",
            CampiId = 1,
            CourseId = 1,
            TccId = 60,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(15)
        };

        // Outro convite ainda pendente (IsValidCode=true significa código ainda válido e não resgatado)
        var inviteOutroAluno = new TccInviteEntity
        {
            Id = 31,
            Email = "outro@discente.ifpe.edu.br",
            Code = "OUTRO0",
            CampiId = 1,
            CourseId = 1,
            TccId = 60,
            IsValidCode = true, // código ainda ativo/não consumido -> allIAlreadyAdded=true => bloqueia avanço de step
            ExpirationDate = DateTime.UtcNow.AddMinutes(15)
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(inviteBruno));

        var campiCourse = new CampiCourseEntity { Id = 1, CampiId = 1, CourseId = 1 };
        _courseGateway.FindByCampiAndCourseId(1, 1).Returns(Task.FromResult(campiCourse));

        var studentProfile = new ProfileEntity { Id = 1, Role = "STUDENT" };
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { studentProfile }));
        _profileGateway.FindByRole("STUDENT").Returns(Task.FromResult<ProfileEntity?>(studentProfile));
        _bcryptGateway.GenerateHashPassword(dto.Password!).Returns("hashed_bruno");
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);

        var tcc = new TccEntity
        {
            Id = 60,
            Title = "TCC Dupla 2024.1",
            // Dois convites: Bruno (aceito) e outro aluno (ainda pendente/com IsValidCode=false)
            // A lógica checa: allIAlreadyAdded = tcc.TccInvites.Any(inv => inv.IsValidCode)
            // Como nenhum IsValidCode=true, allIAlreadyAdded=false => step NÃO avança
            TccInvites = new List<TccInviteEntity> { inviteBruno, inviteOutroAluno },
            UserTccs = new List<gestaotcc.Domain.Entities.UserTcc.UserTccEntity>()
        };

        _tccGateway.FindTccById(60).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<gestaotcc.Domain.Dtos.Email.SendEmailDTO>())
            .Returns(Task.FromResult(ResultPattern<bool>.SuccessResult(true)));

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.True(result.IsSuccess);
        // Step NÃO avança — permanece null/vazio
        Assert.NotEqual(gestaotcc.Domain.Enums.StepTccType.START_AND_ORGANIZATION.ToString(), tcc.Step);
    }

    [Fact]
    public async Task Execute_ShouldCreateDocumentsForLinkedTcc_CoveringAllDocumentTypes()
    {
        // Arrange
        var dto = new AutoRegisterDTO(
            "Juliana Paes",
            "juliana@discente.ifpe.edu.br",
            "20241TADS011",
            "999.000.111-22",
            "81988880000",
            "2024.1",
            ShiftType.MORNING,
            Password: "Password123!");

        var invite = new TccInviteEntity
        {
            Id = 50,
            Email = dto.Email,
            Code = "JULI01",
            CampiId = 1,
            CourseId = 1,
            TccId = 70,
            IsValidCode = false,
            ExpirationDate = DateTime.UtcNow.AddMinutes(15)
        };

        _userGateway.FindByEmail(dto.Email).Returns(Task.FromResult<UserEntity?>(null));
        _tccGateway.FindInviteTccByEmail(dto.Email).Returns(Task.FromResult<TccInviteEntity?>(invite));

        var campiCourse = new CampiCourseEntity { Id = 1, CampiId = 1, CourseId = 1 };
        _courseGateway.FindByCampiAndCourseId(1, 1).Returns(Task.FromResult(campiCourse));

        var studentProfile = new ProfileEntity { Id = 1, Role = "STUDENT" };
        var advisorProfile = new ProfileEntity { Id = 2, Role = "ADVISOR" };
        var teacherProfile = new ProfileEntity { Id = 3, Role = "TEACHER" };

        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { studentProfile }));
        _profileGateway.FindByRole("STUDENT").Returns(Task.FromResult<ProfileEntity?>(studentProfile));
        _bcryptGateway.GenerateHashPassword(dto.Password!).Returns("hashed_juliana");
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);

        // 1. ANEXO II deve ser ignorado
        var docAnexo2 = new DocumentTypeEntity
        {
            Id = 1,
            Name = "ANEXO II - TERMO DE COMPROMISSO DE ORIENTAÇÃO VOLUNTÁRIA",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        // 2. Perfil não aceito (TEACHER) - deve ser ignorado
        var docNoProfile = new DocumentTypeEntity
        {
            Id = 2,
            Name = "ANEXO X - DOC DOCENTE",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { teacherProfile }
        };

        // 3. ONLY_DOCS com múltiplos perfis que JÁ existe em tcc.Documents (UserId == null) - deve ser ignorado
        var docSharedAlreadyExists = new DocumentTypeEntity
        {
            Id = 3,
            Name = "ANEXO III - TERMO COMPARTILHADO JÁ EXISTENTE",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile, advisorProfile }
        };
        var existingSharedDoc = new DocumentEntity
        {
            Id = 300,
            DocumentTypeId = 3,
            UserId = null,
            TccId = 70
        };

        // 4. ONLY_DOCS com múltiplos perfis que NÃO existe ainda - deve criar documento compartilhado
        var docSharedNew = new DocumentTypeEntity
        {
            Id = 4,
            Name = "ANEXO IV - FICHA AVALIATIVA",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile, advisorProfile }
        };

        // 5. ONLY_DOCS com perfil único - deve criar documento individual
        var docOnlyDocsSingle = new DocumentTypeEntity
        {
            Id = 5,
            Name = "ANEXO I - REQUERIMENTO INDIVIDUAL",
            MethodSignature = MethoSignatureType.ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        // 6. NOT_ONLY_DOCS - deve criar documento
        var docNotOnlyDocs = new DocumentTypeEntity
        {
            Id = 6,
            Name = "ANEXO VI - RELATÓRIO FINAL",
            MethodSignature = MethoSignatureType.NOT_ONLY_DOCS.ToString(),
            Profiles = new List<ProfileEntity> { studentProfile }
        };

        var allDocTypes = new List<DocumentTypeEntity>
        {
            docAnexo2,
            docNoProfile,
            docSharedAlreadyExists,
            docSharedNew,
            docOnlyDocsSingle,
            docNotOnlyDocs
        };

        var tcc = new TccEntity
        {
            Id = 70,
            Title = "TCC com Todos os Documentos",
            TccInvites = new List<TccInviteEntity> { invite },
            Documents = new List<DocumentEntity> { existingSharedDoc },
            UserTccs = new List<gestaotcc.Domain.Entities.UserTcc.UserTccEntity>()
        };

        _tccGateway.FindTccById(70).Returns(Task.FromResult<TccEntity?>(tcc));
        _documentTypeGateway.FindAll().Returns(Task.FromResult(allDocTypes));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);

        // Simula falha no envio de email para cobrir o log de aviso (linha 101)
        _emailGateway.Send(Arg.Any<gestaotcc.Domain.Dtos.Email.SendEmailDTO>())
            .Returns(Task.FromResult(ResultPattern<bool>.FailureResult("Falha temporária de SMTP", 500)));

        // Act
        var result = await _useCase.Execute(dto, "salt");

        // Assert
        Assert.True(result.IsSuccess);
        // Documentos criados: docSharedNew, docOnlyDocsSingle, docNotOnlyDocs (+ existingSharedDoc já presente)
        Assert.Contains(tcc.Documents, d => (d.DocumentTypeId == 4 || d.DocumentType?.Id == 4) && d.User == null);
        Assert.Contains(tcc.Documents, d => (d.DocumentTypeId == 5 || d.DocumentType?.Id == 5) && d.User != null);
        Assert.Contains(tcc.Documents, d => (d.DocumentTypeId == 6 || d.DocumentType?.Id == 6) && d.User != null);
        // docAnexo2 e docNoProfile NÃO devem ter sido adicionados
        Assert.DoesNotContain(tcc.Documents, d => d.DocumentTypeId == 1 || d.DocumentType?.Id == 1);
        Assert.DoesNotContain(tcc.Documents, d => d.DocumentTypeId == 2 || d.DocumentType?.Id == 2);
    }
}
