using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using NSubstitute;
using Xunit;

namespace gestaotcc.Test.UseCases.Tcc;

public class FindLibraryPendingUseCaseTests
{
    private readonly ITccGateway _tccGateway = Substitute.For<ITccGateway>();
    private readonly IUserGateway _userGateway = Substitute.For<IUserGateway>();
    private readonly FindLibraryPendingUseCase _useCase;

    public FindLibraryPendingUseCaseTests()
    {
        _useCase = new FindLibraryPendingUseCase(_tccGateway, _userGateway);
    }

    [Fact]
    public async Task Execute_ShouldReturnBadRequest_WhenUserIdInvalid()
    {
        var result = await _useCase.Execute(0);
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnUnauthorized_WhenUserNotFoundOrInactive()
    {
        _userGateway.FindById(10).Returns((UserEntity?)null);

        var result = await _useCase.Execute(10);
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnForbidden_WhenUserIsNotLibraryOrAdmin()
    {
        var student = new UserEntity
        {
            Id = 5,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "STUDENT" } }
        };
        _userGateway.FindById(5).Returns(student);

        var result = await _useCase.Execute(5);
        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task Execute_ShouldReturnPendingAndCatalogedTccs_WhenUserIsLibrary()
    {
        var librarian = new UserEntity
        {
            Id = 50,
            Status = "ACTIVE",
            Profile = new[] { new ProfileEntity { Role = "LIBRARY" } }
        };
        _userGateway.FindById(50).Returns(librarian);

        var student = new UserEntity
        {
            Id = 20,
            Name = "Carlos Aluno",
            Email = "carlos@ifpe.edu.br",
            CampiCourse = new CampiCourseEntity { Course = new CourseEntity { Name = "Sistemas" } }
        };

        var advisor = new UserEntity
        {
            Id = 30,
            Name = "Prof. Orientador"
        };

        var pendingTcc = new TccEntity
        {
            Id = 1,
            Title = "TCC para Catalogar",
            Status = StatusTccType.IN_PROGRESS.ToString(),
            ForwardedToLibraryAt = DateTime.UtcNow.AddDays(-1),
            LibraryReceivedAt = null,
            FinalTccFile = "final_tcc_1.pdf",
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { User = student, Profile = new ProfileEntity { Role = "STUDENT" } },
                new UserTccEntity { User = advisor, Profile = new ProfileEntity { Role = "ADVISOR" } }
            }
        };

        var catalogedTcc = new TccEntity
        {
            Id = 2,
            Title = "TCC Já Catalogado",
            Status = StatusTccType.COMPLETED.ToString(),
            ForwardedToLibraryAt = DateTime.UtcNow.AddDays(-5),
            LibraryReceivedAt = DateTime.UtcNow.AddDays(-3),
            LibraryCatalogCode = "TMB-2026-001",
            UserTccs = new List<UserTccEntity>
            {
                new UserTccEntity { User = student, Profile = new ProfileEntity { Role = "STUDENT" } },
                new UserTccEntity { User = advisor, Profile = new ProfileEntity { Role = "ADVISOR" } }
            }
        };

        _tccGateway.FindAllTccByFilter(Arg.Is<TccFilterDTO>(f => f.StatusTcc == StatusTccType.IN_PROGRESS.ToString()), Arg.Any<long>())
            .Returns(new List<TccEntity> { pendingTcc });

        _tccGateway.FindAllTccByFilter(Arg.Is<TccFilterDTO>(f => f.StatusTcc == StatusTccType.COMPLETED.ToString()), Arg.Any<long>())
            .Returns(new List<TccEntity> { catalogedTcc });

        var result = await _useCase.Execute(50);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.PendingCount);
        Assert.Equal(1, result.Data.CatalogedCount);
        Assert.Single(result.Data.PendingCataloging);
        Assert.Equal("TCC para Catalogar", result.Data.PendingCataloging[0].Title);
        Assert.Equal("final_tcc_1.pdf", result.Data.PendingCataloging[0].FinalTccFile);
        Assert.Single(result.Data.RecentlyCataloged);
        Assert.Equal("TMB-2026-001", result.Data.RecentlyCataloged[0].CatalogCode);
    }
}
