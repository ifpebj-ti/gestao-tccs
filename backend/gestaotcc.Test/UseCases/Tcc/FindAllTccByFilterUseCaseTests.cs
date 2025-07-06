using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class FindAllTccByFilterUseCaseTests
{
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly FindAllTccByFilterUseCase _useCase;

    public FindAllTccByFilterUseCaseTests()
    {
        _useCase = new FindAllTccByFilterUseCase(_userGateway, _tccGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnFilteredTccs_AsDTOs()
    {
        // Arrange
        var tccFilter = new TccFilterDTO( 1,  "IN_PROGRESS");

        var tccList = new List<TccEntity>
        {
            new TccEntity
            {
                Id = 1,
                UserTccs = new List<UserTccEntity>
                {
                    new UserTccEntity
                    {
                        Profile = new ProfileEntity { Role = "STUDENT" },
                        User = new UserEntity { Name = "Student1" }
                    }
                },
                TccInvites = new List<TccInviteEntity>()
            },
            new TccEntity
            {
                Id = 2,
                UserTccs = new List<UserTccEntity>(), // No students
                TccInvites = new List<TccInviteEntity>
                {
                    new TccInviteEntity { Email = "invite@example.com" }
                }
            }
        };

        _tccGateway.FindAllTccByFilter(Arg.Is<TccFilterDTO>(f => f.UserId == tccFilter.UserId && f.StatusTcc == tccFilter.StatusTcc))
            .Returns(Task.FromResult(tccList));

        // Act
        var result = await _useCase.Execute(tccFilter);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);

        var first = result.Data.FirstOrDefault(dto => dto.TccId == 1);
        Assert.NotNull(first);
        Assert.Contains("Student1", first.StudanteNames);

        var second = result.Data.FirstOrDefault(dto => dto.TccId == 2);
        Assert.NotNull(second);
        Assert.Contains("invite@example.com", second.StudanteNames);
    }
}