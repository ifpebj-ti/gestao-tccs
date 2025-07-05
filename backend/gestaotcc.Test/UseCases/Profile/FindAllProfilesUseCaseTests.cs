using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Profile;
using gestaotcc.Domain.Entities.Profile;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Profile;

public class FindAllProfilesUseCaseTests
{
    private readonly IProfileGateway _profileGateway = Substitute.For<IProfileGateway>();
    private readonly FindAllProfilesUseCase _useCase;

    public FindAllProfilesUseCaseTests()
    {
        _useCase = new FindAllProfilesUseCase(_profileGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnMappedProfilesSuccessfully()
    {
        // Arrange
        var profileEntities = new List<ProfileEntity>
        {
            new ProfileEntity(1, "COORDINATOR"),
            new ProfileEntity(2, "STUDENT"),
            new ProfileEntity(3, "ADVISOR")
        };

        _profileGateway.FindAll().Returns(profileEntities);

        // Act
        var result = await _useCase.Execute();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.Count);

        Assert.Contains(result.Data, p => p.Id == 1 && p.Role == "COORDINATOR");
        Assert.Contains(result.Data, p => p.Id == 2 && p.Role == "STUDENT");
        Assert.Contains(result.Data, p => p.Id == 3 && p.Role == "ADVISOR");
    }

    [Fact]
    public async Task Execute_ShouldReturnEmptyList_WhenNoProfilesExist()
    {
        // Arrange
        _profileGateway.FindAll().Returns(new List<ProfileEntity>());

        // Act
        var result = await _useCase.Execute();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }
}