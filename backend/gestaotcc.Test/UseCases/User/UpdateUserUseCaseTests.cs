using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.User;

public class UpdateUserUseCaseTests
{
    private readonly IUserGateway _userGateway;
    private readonly IProfileGateway _profileGateway;
    private readonly ICourseGateway _courseGateway;
    private readonly IAppLoggerGateway<UpdateUserUseCase> _logger;
    private readonly UpdateUserUseCase _useCase;

    public UpdateUserUseCaseTests()
    {
        _userGateway = Substitute.For<IUserGateway>();
        _profileGateway = Substitute.For<IProfileGateway>();
        _courseGateway = Substitute.For<ICourseGateway>();
        _logger = Substitute.For<IAppLoggerGateway<UpdateUserUseCase>>();
        _useCase = new UpdateUserUseCase(_userGateway, _profileGateway, _courseGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var dto = new UpdateUserDTO(
            Id: 999,
            Name: "Usuário Inexistente",
            Email: "inexistente@ifpe.edu.br",
            Registration: "12345",
            Cpf: "11122233344",
            Siape: "67890",
            Phone: "81999998888",
            UserClass: "Turma A",
            Shift: ShiftType.MORNING,
            Titration: "Mestre",
            Profile: new List<string> { "TEACHER" },
            Status: "ACTIVE",
            CampiId: 1,
            CourseId: 1
        );

        _userGateway.FindById(999).Returns(Task.FromResult<UserEntity?>(null));

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorDetails?.Status.Should().Be(500);
        result.Message.Should().Be("Erro ao atualizar usuário");
        await _userGateway.DidNotReceive().Update(Arg.Any<UserEntity>());
    }

    [Fact]
    public async Task Execute_ShouldUpdateUserSuccessfully_WhenUserExists()
    {
        // Arrange
        var existingUser = new UserEntity
        {
            Id = 42,
            Name = "Nome Antigo",
            Email = "antigo@ifpe.edu.br",
            Registration = "OLD123",
            SIAPE = "OLD_SIAPE",
            CPF = "000.000.000-00",
            CampiCourseId = 1,
            Status = "PENDING",
            Phone = "81900000000",
            UserClass = "2023.2",
            Shift = "Noite",
            Titration = "Graduado"
        };

        var dto = new UpdateUserDTO(
            Id: 42,
            Name: "Nome Atualizado",
            Email: "atualizado@ifpe.edu.br",
            Registration: "NEW123",
            Cpf: "111.222.333-44",
            Siape: "NEW_SIAPE",
            Phone: "81999998888",
            UserClass: "2024.1",
            Shift: ShiftType.MORNING,
            Titration: "Mestre",
            Profile: new List<string> { "ADVISOR" },
            Status: "ACTIVE",
            CampiId: 2,
            CourseId: 3
        );

        _userGateway.FindById(42).Returns(Task.FromResult<UserEntity?>(existingUser));

        var updatedProfiles = new List<ProfileEntity> { new() { Id = 2, Role = "ADVISOR" } };
        _profileGateway.FindByRole(Arg.Any<List<string>>()).Returns(Task.FromResult(updatedProfiles));

        var campiCourse = new CampiCourseEntity { Id = 15, CampiId = 2, CourseId = 3 };
        _courseGateway.FindByCampiAndCourseId(2, 3).Returns(Task.FromResult(campiCourse));

        _userGateway.Update(Arg.Any<UserEntity>()).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.Execute(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingUser.Name.Should().Be("Nome Atualizado");
        existingUser.Email.Should().Be("atualizado@ifpe.edu.br");
        existingUser.Registration.Should().Be("NEW123");
        existingUser.CPF.Should().Be("111.222.333-44");
        existingUser.SIAPE.Should().Be("NEW_SIAPE");
        existingUser.Phone.Should().Be("81999998888");
        existingUser.UserClass.Should().Be("2024.1");
        existingUser.Titration.Should().Be("Mestre");
        existingUser.Status.Should().Be("ACTIVE");
        existingUser.CampiCourseId.Should().Be(15);
        existingUser.Profile.Should().BeEquivalentTo(updatedProfiles);

        await _userGateway.Received(1).Update(existingUser);
    }
}

