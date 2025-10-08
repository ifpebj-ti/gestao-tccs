using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Entities.CampiCourse;

namespace gestaotcc.Test.UseCases.User;

using gestaotcc.Domain.Entities.TccInvite;
using Xunit;
using NSubstitute;
using FluentAssertions;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

public class CreateUserUseCaseTests
{
    private readonly IUserGateway _userGateway;
    private readonly IProfileGateway _profileGateway;
    private readonly IEmailGateway _emailGateway;
    private readonly ICourseGateway _courseGateway;
    private readonly ITccGateway _tccGateway;
    private readonly IDocumentTypeGateway _documentTypeGateway;
    private readonly CreateAccessCodeUseCase _createAccessCodeUseCase;
    private readonly CreateUserUseCase _createUserUseCase;
    private readonly IAppLoggerGateway<CreateUserUseCase> _loggerCreateUserUseCase;
    private readonly IAppLoggerGateway<CreateAccessCodeUseCase> _loggerCreateAccessCodeUseCase;

    public CreateUserUseCaseTests()
    {
        _userGateway = Substitute.For<IUserGateway>();
        _profileGateway = Substitute.For<IProfileGateway>();
        _emailGateway = Substitute.For<IEmailGateway>();
        _courseGateway = Substitute.For<ICourseGateway>();
        _tccGateway = Substitute.For<ITccGateway>();
        _documentTypeGateway = Substitute.For<IDocumentTypeGateway>();
        _loggerCreateUserUseCase = Substitute.For<IAppLoggerGateway<CreateUserUseCase>>();
        _loggerCreateAccessCodeUseCase = Substitute.For<IAppLoggerGateway<CreateAccessCodeUseCase>>();
        _createAccessCodeUseCase = Substitute.For<CreateAccessCodeUseCase>(_loggerCreateAccessCodeUseCase);

        _createUserUseCase = new CreateUserUseCase(
            _userGateway,
            _profileGateway,
            _emailGateway,
            _courseGateway,
            _tccGateway,
            _documentTypeGateway,
            _createAccessCodeUseCase,
            _loggerCreateUserUseCase);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserAlreadyExists()
    {
        // Arrange
        var createUserDto = new CreateUserDTO(
            "Student User", 
            "student@example.com",
            "67890",
            "555.666.777-88",
            null,
            new List<string> { "STUDENT" },
            1,
            1);
        var existingUser = new UserEntity();
        _userGateway.FindByEmail(createUserDto.Email).Returns(existingUser);

        // Act
        var result = await _createUserUseCase.Execute(createUserDto, "combination");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Message.Should().Be("Erro ao cadastrar usuário; Por favor verifique as informações e tente novamente");
        result.ErrorDetails.Status.Should().Be(404);
        await _userGateway.DidNotReceive().Save(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_ShouldCreateUserAndSendEmail_WhenUserIsOnlyStudentAndHasNoTccInvite()
    {
        // Arrange
        var createUserDto = new CreateUserDTO(
            "Student User", 
            "student@example.com",
            "67890",
            "555.666.777-88",
            null,
            new List<string> { "STUDENT" },
            1,
            1);
        UserEntity newUser = null;
        var profileStudent = new ProfileEntity { Role = "STUDENT" };
        var campiCourse =  new CampiCourseEntity { CampiId = 1, CourseId = 1  };
        var accessCode = new AccessCodeEntity { Code = "ACC123", ExpirationDate = DateTime.UtcNow.AddMinutes(5) };

        _userGateway.FindByEmail(createUserDto.Email).Returns((UserEntity)null);
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { profileStudent }));
        _createAccessCodeUseCase.Execute(Arg.Any<string>()).Returns(ResultPattern<AccessCodeEntity>.SuccessResult(accessCode));
        _userGateway.Save(Arg.Do<UserEntity>(u => newUser = u)).Returns(Task.CompletedTask);
        _tccGateway.FindInviteTccByEmail(createUserDto.Email).Returns(Task.FromResult((TccInviteEntity)null));
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult()));

        // Act
        var result = await _createUserUseCase.Execute(createUserDto, "combination");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Name.Should().Be(createUserDto.Name);
        result.Data.Email.Should().Be(createUserDto.Email);
        result.Data.Profile.Should().ContainSingle(p => p.Role == "STUDENT");
        result.Data.CampiCourse.Should().BeEquivalentTo(campiCourse);
        result.Data.AccessCode.Should().Be(accessCode);

        await _userGateway.Received(1).Save(Arg.Is<UserEntity>(u => u.Email == createUserDto.Email));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(dto =>
            dto.Recipient == createUserDto.Email && dto.TypeTemplate == "ADD-USER-TCC"));
        await _tccGateway.Received(1).FindInviteTccByEmail(createUserDto.Email);
        await _tccGateway.DidNotReceive().FindTccById(Arg.Any<long>());
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Fact]
    public async Task Execute_ShouldCreateUserAndSendEmail_WhenUserIsOnlyStudentAndHasTccInvite_ButTccDoesNotExist()
    {
        // Arrange
        var createUserDto = new CreateUserDTO(
            "Student User", 
            "student@example.com",
            "67890",
            "555.666.777-88",
            null,
            new List<string> { "STUDENT" },
            1,
            1);
        UserEntity newUser = null;
        var profileStudent = new ProfileEntity { Role = "STUDENT" };
        var accessCode = new AccessCodeEntity { Code = "ACC123", ExpirationDate = DateTime.UtcNow.AddMinutes(5) };

        var tccInvite = new TccInviteEntity { TccId = 1, Email = createUserDto.Email, IsValidCode = true };

        _userGateway.FindByEmail(createUserDto.Email).Returns((UserEntity)null);
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { profileStudent }));
        _createAccessCodeUseCase.Execute(Arg.Any<string>()).Returns(ResultPattern<AccessCodeEntity>.SuccessResult(accessCode));
        _userGateway.Save(Arg.Do<UserEntity>(u => newUser = u)).Returns(Task.CompletedTask);
        _tccGateway.FindInviteTccByEmail(createUserDto.Email).Returns(Task.FromResult(tccInvite));
        _tccGateway.FindTccById(tccInvite.TccId).Returns(Task.FromResult((TccEntity)null));
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult()));

        // Act
        var result = await _createUserUseCase.Execute(createUserDto, "combination");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();

        await _userGateway.Received(1).Save(Arg.Is<UserEntity>(u => u.Email == createUserDto.Email));
        await _tccGateway.Received(1).FindInviteTccByEmail(createUserDto.Email);
        await _tccGateway.Received(1).FindTccById(tccInvite.TccId);
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(dto =>
            dto.Recipient == createUserDto.Email && dto.TypeTemplate == "ADD-USER-TCC"));
    }

    [Fact]
    public async Task Execute_ShouldCreateUserAndSendEmail_WhenUserIsNotOnlyStudent()
    {
        // Arrange
        var createUserDto = new CreateUserDTO(
            "Student User", 
            "student@example.com",
            "67890",
            "555.666.777-88",
            null,
            new List<string> { "ADVISOR" },
            1,
            1);
        UserEntity newUser = null;
        var profileTeacher = new ProfileEntity { Role = "TEACHER" };
        var campiCourse =  new CampiCourseEntity { CampiId = 1, CourseId = 1 };
        var accessCode = new AccessCodeEntity { Code = "ACC456", ExpirationDate = DateTime.UtcNow.AddMinutes(5) };

        _userGateway.FindByEmail(createUserDto.Email).Returns((UserEntity)null);
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { profileTeacher }));
        _createAccessCodeUseCase.Execute(Arg.Any<string>()).Returns(ResultPattern<AccessCodeEntity>.SuccessResult(accessCode));
        _userGateway.Save(Arg.Do<UserEntity>(u => newUser = u)).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult()));

        // Act
        var result = await _createUserUseCase.Execute(createUserDto, "combination");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Name.Should().Be(createUserDto.Name);
        result.Data.Email.Should().Be(createUserDto.Email);
        result.Data.Profile.Should().ContainSingle(p => p.Role == "TEACHER");
        result.Data.CampiCourse.Should().BeEquivalentTo(campiCourse);
        result.Data.AccessCode.Should().Be(accessCode);

        await _userGateway.Received(1).Save(Arg.Is<UserEntity>(u => u.Email == createUserDto.Email));
        await _emailGateway.Received(1).Send(Arg.Is<SendEmailDTO>(dto =>
            dto.Recipient == createUserDto.Email && dto.TypeTemplate == "CREATE-USER"));
        await _tccGateway.DidNotReceive().FindInviteTccByEmail(Arg.Any<string>());
        await _tccGateway.DidNotReceive().FindTccById(Arg.Any<long>());
        await _tccGateway.DidNotReceive().Update(Arg.Any<TccEntity>());
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenEmailSendFailsForStudent()
    {
        // Arrange
        var createUserDto = new CreateUserDTO(
            "Student User", 
            "student@example.com",
            "67890",
            "555.666.777-88",
            null,
            new List<string> { "STUDENT" },
            1,
            1);
        var profileStudent = new ProfileEntity { Role = "STUDENT" };
        var accessCode = new AccessCodeEntity { Code = "ACC123", ExpirationDate = DateTime.UtcNow.AddMinutes(5) };

        _userGateway.FindByEmail(createUserDto.Email).Returns((UserEntity)null);
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { profileStudent }));
        _createAccessCodeUseCase.Execute(Arg.Any<string>()).Returns(ResultPattern<AccessCodeEntity>.SuccessResult(accessCode));
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);
        _tccGateway.FindInviteTccByEmail(createUserDto.Email).Returns(Task.FromResult((TccInviteEntity)null));
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.FailureResult("Email send error", 500)));

        // Act
        var result = await _createUserUseCase.Execute(createUserDto, "combination");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Message.Should().Be("Email send error");
        result.ErrorDetails.Status.Should().Be(500);
        await _userGateway.Received(1).Save(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenEmailSendFailsForOtherProfiles()
    {
        // Arrange
        var createUserDto = new CreateUserDTO(
            "Student User", 
            "student@example.com",
            "67890",
            "555.666.777-88",
            null,
            new List<string> { "STUDENT" },
            1,
            1);
        var profileCoordinator = new ProfileEntity { Role = "COORDINATOR" };
        var accessCode = new AccessCodeEntity { Code = "ACC789", ExpirationDate = DateTime.UtcNow.AddMinutes(5) };

        _userGateway.FindByEmail(createUserDto.Email).Returns((UserEntity)null);
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { profileCoordinator }));
        _createAccessCodeUseCase.Execute(Arg.Any<string>()).Returns(ResultPattern<AccessCodeEntity>.SuccessResult(accessCode));
        _userGateway.Save(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.FailureResult("Email service unavailable", 500)));

        // Act
        var result = await _createUserUseCase.Execute(createUserDto, "combination");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Message.Should().Be("Email service unavailable");
        result.ErrorDetails.Status.Should().Be(500);
        await _userGateway.Received(1).Save(Arg.Any<UserEntity>());
    }
    

    [Fact]
    public async Task Execute_ShouldNotSetTccStepToStartAndOrganization_WhenNotAllInvitesAreAdded()
    {
        // Arrange
        var createUserDto = new CreateUserDTO(
            "Student User", 
            "student@example.com",
            "67890",
            "555.666.777-88",
            null,
            new List<string> { "STUDENT" },
            1,
            1);
        UserEntity newUser = null;
        var profileStudent = new ProfileEntity { Role = "STUDENT" };
        var accessCode = new AccessCodeEntity { Code = "ACC123", ExpirationDate = DateTime.UtcNow.AddMinutes(5) };

        var tccInvite1 = new TccInviteEntity { TccId = 1, Email = createUserDto.Email, IsValidCode = true };
        var tccInvite2 = new TccInviteEntity { TccId = tccInvite1.TccId, Email = "yet_another@example.com", IsValidCode = true };
        var tcc = new TccEntity { Id = tccInvite1.TccId, Documents = new List<DocumentEntity>(), TccInvites = new List<TccInviteEntity> { tccInvite1, tccInvite2 }, Step = StepTccType.PROPOSAL_REGISTRATION.ToString() };

        _userGateway.FindByEmail(createUserDto.Email).Returns((UserEntity)null);
        _documentTypeGateway.FindAll().Returns(Task.FromResult(new List<DocumentTypeEntity>()));
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(new List<ProfileEntity> { profileStudent }));
        _createAccessCodeUseCase.Execute(Arg.Any<string>()).Returns(ResultPattern<AccessCodeEntity>.SuccessResult(accessCode));
        _userGateway.Save(Arg.Do<UserEntity>(u => newUser = u)).Returns(Task.CompletedTask);
        _tccGateway.FindInviteTccByEmail(createUserDto.Email).Returns(Task.FromResult(tccInvite1));
        _tccGateway.FindTccById(tccInvite1.TccId).Returns(Task.FromResult(tcc));
        _tccGateway.Update(Arg.Any<TccEntity>()).Returns(Task.CompletedTask);
        _emailGateway.Send(Arg.Any<SendEmailDTO>()).Returns(Task.FromResult(ResultPattern<bool>.SuccessResult()));

        // Act
        var result = await _createUserUseCase.Execute(createUserDto, "combination");

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _tccGateway.Received(1).Update(Arg.Is<TccEntity>(t => t.Step == StepTccType.PROPOSAL_REGISTRATION.ToString()));
    }
}