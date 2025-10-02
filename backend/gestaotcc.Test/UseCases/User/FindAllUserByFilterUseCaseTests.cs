using FluentAssertions;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.User;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.User;
using NSubstitute;

namespace gestaotcc.Test.UseCases.User;

public class FindAllUserByFilterUseCaseTests
{
    private readonly IUserGateway _userGateway;
    private readonly FindAllUserByFilterUseCase _findAllUserByFilterUseCase;
    private readonly IAppLoggerGateway<FindAllUserByFilterUseCase> _logger;

    public FindAllUserByFilterUseCaseTests()
    {
        _userGateway = Substitute.For<IUserGateway>();
        _logger = Substitute.For<IAppLoggerGateway<FindAllUserByFilterUseCase>>();
        _findAllUserByFilterUseCase = new FindAllUserByFilterUseCase(_userGateway, _logger);
    }

    [Fact]
    public async Task Execute_ShouldReturnUsers_WhenFilterMatchesUsers()
    {
        // Arrange
        // Note: UserFilterDTO agora tem 'Profile' como string, e todos os campos podem ser nulos, exceto Profile.
        var filter = new UserFilterDTO(Email: "student@example.com", Name: "Test", Registration: null, Profile: "STUDENT");
        
        var course = new CourseEntity { Id = 1, Name = "Computer Science" };
        var profileStudent = new ProfileEntity { Id = 1, Role = "STUDENT" };
        profileStudent.Users = new List<UserEntity>();
        
        // Criando usuários com perfis e cursos associados
        var user1 = new UserEntity 
        { 
            Id = 1, 
            Name = "Test User 1", 
            Email = "student1@example.com", 
            Registration = "REG1",
            Course = course,
            Profile = new List<ProfileEntity> { profileStudent } 
        };
        var user2 = new UserEntity 
        { 
            Id = 2, 
            Name = "Test User 2", 
            Email = "student2@example.com", 
            Registration = "REG2",
            Course = course,
            Profile = new List<ProfileEntity> { profileStudent } 
        };

        // Importante: Simular a relação inversa Profile.Users para que a UserFactory funcione corretamente.
        // O FirstOrDefault da UserFactory busca um perfil que tenha o 'user.Id' em sua lista 'Users'.
        profileStudent.Users.Add(user1);
        profileStudent.Users.Add(user2);

        var usersFromGateway = new List<UserEntity> { user1, user2 };

        _userGateway.FindAllByFilter(filter).Returns(Task.FromResult(usersFromGateway));

        // Act
        var result = await _findAllUserByFilterUseCase.Execute(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(2);

        result.Data.First().Id.Should().Be(1);
        result.Data.First().Name.Should().Be("Test User 1");
        result.Data.First().Email.Should().Be("student1@example.com");
        result.Data.First().Profile.Should().Be("STUDENT"); // Deve ser a string 'STUDENT'
        result.Data.First().Course.Should().Be("Computer Science");

        result.Data.Last().Id.Should().Be(2);
        result.Data.Last().Name.Should().Be("Test User 2");
        result.Data.Last().Email.Should().Be("student2@example.com");
        result.Data.Last().Profile.Should().Be("STUDENT");
        result.Data.Last().Course.Should().Be("Computer Science");

        await _userGateway.Received(1).FindAllByFilter(filter);
    }

    [Fact]
    public async Task Execute_ShouldReturnEmptyList_WhenNoUsersMatchFilter()
    {
        // Arrange
        var filter = new UserFilterDTO(Email: "nonexistent@example.com", Name: "NonExistent", Registration: null, Profile: "ADMIN");
        _userGateway.FindAllByFilter(filter).Returns(Task.FromResult(new List<UserEntity>()));

        // Act
        var result = await _findAllUserByFilterUseCase.Execute(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeEmpty();
        await _userGateway.Received(1).FindAllByFilter(filter);
    }
    
    [Fact]
    public async Task Execute_ShouldReturnUsersWithMultipleProfiles_AndSelectCorrectOneBasedOnFactoryLogic()
    {
        // Arrange
        var filter = new UserFilterDTO(Email: "multi@example.com", Name: "Multi Profile", Registration: null, Profile: "STUDENT");
        var course = new CourseEntity { Id = 4, Name = "Business" };
        var studentProfile = new ProfileEntity { Id = 5, Role = "STUDENT" };
        var teacherProfile = new ProfileEntity { Id = 6, Role = "TEACHER" };
        studentProfile.Users = new List<UserEntity>();
        teacherProfile.Users = new List<UserEntity>();

        // Definindo a ordem dos perfis no UserEntity.Profile para testar o FirstOrDefault
        var user = new UserEntity
        {
            Id = 5,
            Name = "Multi Profile User",
            Email = "multi@example.com",
            Registration = "REG3",
            Course = course,
            // A ordem aqui importa para o FirstOrDefault no UserFactory.
            // O primeiro perfil na lista que tiver o usuário associado será escolhido.
            Profile = new List<ProfileEntity> { studentProfile, teacherProfile } 
        };

        // Importante: Simular a relação inversa Profile.Users
        studentProfile.Users.Add(user); // Associa o usuário ao perfil de estudante
        teacherProfile.Users.Add(user); // Associa o usuário ao perfil de professor também

        _userGateway.FindAllByFilter(filter).Returns(Task.FromResult(new List<UserEntity> { user }));

        // Act
        var result = await _findAllUserByFilterUseCase.Execute(filter);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data.First().Id.Should().Be(5);
        result.Data.First().Name.Should().Be("Multi Profile User");
        result.Data.First().Email.Should().Be("multi@example.com");
        // O FirstOrDefault(x => x.Users.Any(x => x.Id == user.Id)) vai pegar o 'studentProfile' primeiro
        result.Data.First().Profile.Should().Be("STUDENT"); 
        result.Data.First().Course.Should().Be("Business");

        await _userGateway.Received(1).FindAllByFilter(filter);
    }
}