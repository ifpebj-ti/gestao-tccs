using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Utils;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.User;

public class FindAllUserUseCaseTests
{
    private readonly IUserGateway _userGateway;
    private readonly IAppLoggerGateway<FindAllUserUseCase> _logger;
    private readonly FindAllUserUseCase _useCase;

    public FindAllUserUseCaseTests()
    {
        _userGateway = Substitute.For<IUserGateway>();
        _logger = Substitute.For<IAppLoggerGateway<FindAllUserUseCase>>();
        _useCase = new FindAllUserUseCase(_userGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnPagedUsers_WhenUsersExist()
    {
        // Arrange
        var campi = new CampiEntity { Id = 1, Name = "Belo Jardim" };
        var course = new CourseEntity { Id = 1, Name = "TADS" };
        var campiCourse = new CampiCourseEntity
        {
            Id = 1,
            CampiId = 1,
            Campi = campi,
            CourseId = 1,
            Course = course
        };

        var user1 = new UserEntity
        {
            Id = 10,
            Name = "João Silva",
            Email = "joao@ifpe.edu.br",
            Profile = new List<ProfileEntity> { new() { Role = "STUDENT" } },
            Registration = "20241TADS001",
            CPF = "111.222.333-44",
            SIAPE = null,
            Phone = "81999998888",
            UserClass = "2024.1",
            Shift = "Manhã",
            Titration = null,
            CampiCourse = campiCourse,
            Status = "ACTIVE"
        };

        var user2 = new UserEntity
        {
            Id = 20,
            Name = "Prof. Carlos",
            Email = "carlos@ifpe.edu.br",
            Profile = new List<ProfileEntity> { new() { Role = "TEACHER" } },
            Registration = null,
            CPF = "555.666.777-88",
            SIAPE = "1234567",
            Phone = "81988887777",
            UserClass = null,
            Shift = "Noite",
            Titration = "Doutorado",
            CampiCourse = campiCourse,
            Status = "ACTIVE"
        };

        var pagedResult = new PagedResult<List<UserEntity>>
        {
            Pages = new List<UserEntity> { user1, user2 },
            TotalRecords = 25
        };

        _userGateway.FindAll(1, 10).Returns(Task.FromResult(pagedResult));

        // Act
        var result = await _useCase.Execute(1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TotalRecords.Should().Be(25);
        result.Data.CurrentPageNumber.Should().Be(1);
        result.Data.CurrentPageSize.Should().Be(10);
        result.Data.TotalPages.Should().Be(3); // ceil(25 / 10) = 3
        result.Data.Data.Should().HaveCount(2);

        var firstUser = result.Data.Data[0];
        firstUser.Id.Should().Be(10);
        firstUser.Name.Should().Be("João Silva");
        firstUser.Email.Should().Be("joao@ifpe.edu.br");
        firstUser.Profile.Should().Be("STUDENT");
        firstUser.Registration.Should().Be("20241TADS001");
        firstUser.Cpf.Should().Be("111.222.333-44");
        firstUser.Campus.Id.Should().Be(1);
        firstUser.Campus.Name.Should().Be("Belo Jardim");
        firstUser.Campus.Course.Id.Should().Be(1);
        firstUser.Campus.Course.Name.Should().Be("TADS");

        var secondUser = result.Data.Data[1];
        secondUser.Id.Should().Be(20);
        secondUser.Name.Should().Be("Prof. Carlos");
        secondUser.Profile.Should().Be("TEACHER");
        secondUser.Siape.Should().Be("1234567");

        await _userGateway.Received(1).FindAll(1, 10);
    }

    [Fact]
    public async Task Execute_ShouldReturnEmptyPagedUsers_WhenNoUsersExist()
    {
        // Arrange
        var pagedResult = new PagedResult<List<UserEntity>>
        {
            Pages = new List<UserEntity>(),
            TotalRecords = 0
        };

        _userGateway.FindAll(1, 10).Returns(Task.FromResult(pagedResult));

        // Act
        var result = await _useCase.Execute(1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TotalRecords.Should().Be(0);
        result.Data.TotalPages.Should().Be(0);
        result.Data.Data.Should().BeEmpty();
    }
}

