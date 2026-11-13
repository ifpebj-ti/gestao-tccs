using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.User;

public class FindAvailableAdvisorsUseCaseTests
{
    private readonly IUserGateway _userGateway;
    private readonly IAppLoggerGateway<FindAvailableAdvisorsUseCase> _logger;
    private readonly FindAvailableAdvisorsUseCase _useCase;

    public FindAvailableAdvisorsUseCaseTests()
    {
        _userGateway = Substitute.For<IUserGateway>();
        _logger = Substitute.For<IAppLoggerGateway<FindAvailableAdvisorsUseCase>>();
        _useCase = new FindAvailableAdvisorsUseCase(_userGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnAdvisors_WhenAdvisorsAreFound()
    {
        // Arrange
        long campiCourseId = 1;
        var course = new CourseEntity { Id = 1, Name = "Engenharia de Software" };
        var campiCourse = new CampiCourseEntity { Id = 1, Course = course };

        var advisorProfile = new ProfileEntity { Id = 2, Role = RoleType.ADVISOR.ToString(), Users = new List<UserEntity>() };

        var advisor1 = new UserEntity
        {
            Id = 101,
            Name = "Dr. Roberto",
            Email = "roberto@ifpe.edu.br",
            CampiCourse = campiCourse,
            Profile = new List<ProfileEntity> { advisorProfile }
        };
        advisorProfile.Users.Add(advisor1);

        var advisorList = new List<UserEntity> { advisor1 };

        _userGateway.FindAllByFilter(
            Arg.Is<UserFilterDTO>(f => f.Profile == RoleType.ADVISOR.ToString()),
            campiCourseId
        ).Returns(Task.FromResult(advisorList));

        // Act
        var result = await _useCase.Execute(campiCourseId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data.First().Id.Should().Be(101);
        result.Data.First().Name.Should().Be("Dr. Roberto");
        result.Data.First().Email.Should().Be("roberto@ifpe.edu.br");
        result.Data.First().Profile.Should().Be("ADVISOR");
        result.Data.First().Course.Should().Be("Engenharia de Software");

        await _userGateway.Received(1).FindAllByFilter(
            Arg.Is<UserFilterDTO>(f => f.Profile == RoleType.ADVISOR.ToString()),
            campiCourseId
        );
    }

    [Fact]
    public async Task Execute_ShouldReturnEmptyList_WhenNoAdvisorsFound()
    {
        // Arrange
        long campiCourseId = 99;

        _userGateway.FindAllByFilter(
            Arg.Any<UserFilterDTO>(),
            campiCourseId
        ).Returns(Task.FromResult(new List<UserEntity>()));

        // Act
        var result = await _useCase.Execute(campiCourseId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
    }
}

