using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Tcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Tcc;

public class PendingTccQueuesTests
{
    private static readonly DateTime FirstDay = new(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SecondDay = FirstDay.AddDays(1);
    private readonly ITccGateway _tccs = Substitute.For<ITccGateway>();
    private readonly IUserGateway _users = Substitute.For<IUserGateway>();

    [Theory]
    [InlineData(0, null, null, 400)]
    [InlineData(1, null, null, 401)]
    [InlineData(1, "INACTIVE", "LIBRARY", 401)]
    [InlineData(1, "ACTIVE", "STUDENT", 403)]
    public async Task LibraryQueue_RejectsInvalidUserOrRole(long id, string? status, string? role, int expected)
    {
        if (status is not null)
            _users.FindById(id).Returns(User(role!, status: status));

        var result = await new FindLibraryPendingUseCase(_tccs, _users).Execute(id);

        Assert.Equal(expected, result.ErrorDetails?.Status);
        await _tccs.DidNotReceive().FindAllTccByFilter(Arg.Any<TccFilterDTO>(), Arg.Any<long>());
    }

    [Theory]
    [InlineData("LIBRARY", 42L)]
    [InlineData("ADMIN", null)]
    public async Task LibraryQueue_SortsCatalogingAndUsesDefaultsForLegacyRecords(string role, long? campusId)
    {
        _users.FindById(1).Returns(User(role, campusId));
        var current = new TccEntity
        {
            Id = 10,
            Title = "Projeto atual",
            ForwardedToLibraryAt = SecondDay,
            FinalTccFile = "final.pdf",
            UserTccs = [Student("Ana", "ana@example.org", new CourseEntity { Name = "Computacao" }), Advisor("Bia")]
        };
        var legacy = new TccEntity
        {
            Id = 11,
            Title = null,
            ForwardedToLibraryAt = FirstDay,
            UserTccs = [Student(null, "legacy@example.org"), Student(null, null)]
        };
        var cataloged = new TccEntity
        {
            Id = 20,
            Title = "Projeto no acervo",
            ForwardedToLibraryAt = FirstDay,
            LibraryReceivedAt = SecondDay,
            LibraryCatalogCode = "CAT-20",
            UserTccs = [Student("Eva", "eva@example.org", new CourseEntity { Name = "Redes" }), Advisor("Lia")]
        };
        var older = new TccEntity
        {
            Id = 21,
            Title = null,
            LibraryReceivedAt = FirstDay,
            UserTccs = [Student(null, "older@example.org"), Student(null, null), Advisor(null)]
        };
        var withMissingCourse = new TccEntity
        {
            Id = 22, LibraryReceivedAt = FirstDay.AddDays(-1),
            UserTccs = [Student("Jose", null, hasCampus: true)]
        };
        var withMissingCourseInQueue = new TccEntity
        {
            Id = 12, ForwardedToLibraryAt = FirstDay.AddDays(2),
            UserTccs = [Student("Paulo", null, hasCampus: true), Advisor(null)]
        };
        _tccs.FindAllTccByFilter(new TccFilterDTO(null, "IN_PROGRESS"), campusId ?? 0).Returns(
            new List<TccEntity> { current, legacy, withMissingCourseInQueue, cataloged, new() { Id = 19 } });
        _tccs.FindAllTccByFilter(new TccFilterDTO(null, "COMPLETED"), campusId ?? 0).Returns(
            new List<TccEntity> { older, cataloged, withMissingCourse, current });

        var result = await new FindLibraryPendingUseCase(_tccs, _users).Execute(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data.PendingCount);
        Assert.Equal(3, result.Data.CatalogedCount);
        Assert.Equal([11L, 10L, 12L], result.Data.PendingCataloging.Select(item => item.TccId));
        Assert.Equal([20L, 21L, 22L], result.Data.RecentlyCataloged.Select(item => item.TccId));
        var first = result.Data.PendingCataloging[0];
        Assert.Equal("TCC sem t\u00edtulo", first.Title);
        Assert.Equal(["legacy@example.org", "Aluno"], first.StudentNames);
        Assert.Equal("Orientador", first.AdvisorName);
        Assert.Equal("Curso Superior", first.CourseName);
        Assert.Equal("2026-11-17T09:00:00Z", first.ForwardedAt);
        Assert.Equal("Computacao", result.Data.PendingCataloging[1].CourseName);
        Assert.Equal("Bia", result.Data.PendingCataloging[1].AdvisorName);
        Assert.Equal("final.pdf", result.Data.PendingCataloging[1].FinalTccFile);
        Assert.Equal("CAT-20", result.Data.RecentlyCataloged[0].CatalogCode);
        Assert.Equal("2026-11-18T09:00:00Z", result.Data.RecentlyCataloged[0].CatalogedAt);
        Assert.Null(result.Data.RecentlyCataloged[1].ForwardedAt);
        Assert.Equal(["older@example.org", "Aluno"], result.Data.RecentlyCataloged[1].StudentNames);
        Assert.Equal("Curso Superior", result.Data.RecentlyCataloged[2].CourseName);
    }

    [Fact]
    public async Task CoordinationQueue_AcceptsSupervisorAndListsLegacyPendingItems()
    {
        _users.FindById(1).Returns(User("SUPERVISOR"));
        var legacy = new TccEntity { Id = 1, Step = "6", Title = null, UserTccs = [Student(null, "legacy@example.org"), Advisor("Bia")] };
        var withDeadline = new TccEntity { Id = 2, FinalSubmissionDeadline = SecondDay, UserTccs = [Student(null, null)] };
        var withCover = new TccEntity { Id = 3, HardCoverReceivedAt = FirstDay, Title = null, UserTccs = [Student(null, "cover@example.org"), Student(null, null), Advisor("Bia")] };
        var forwarded = new TccEntity { Id = 4, Step = "6", HardCoverReceivedAt = FirstDay, ForwardedToLibraryAt = SecondDay };
        var cancellation = new TccEntity
        {
            Id = 5, Title = null, TccCancellation = new TccCancellationEntity(5, "Motivo", "PENDING", 5),
            UserTccs = [Student(null, "cancel@example.org"), Student(null, null), Advisor("Bia")]
        };
        var changes = new TccEntity
        {
            Id = 6, Title = null, UserTccs = [Student(null, "change@example.org"), Student(null, null), Advisor("Bia")],
            AdvisorChanges =
            [
                new() { Id = 7, Status = "PENDING_COORDINATION", CreatedAt = FirstDay, SuggestedAdvisor = new UserEntity { Name = "Lisa" } },
                new() { Id = 8, Status = "APPROVED", CreatedAt = FirstDay }
            ]
        };
        _tccs.FindAllTccByFilter(new TccFilterDTO(null, "IN_PROGRESS"), 0)
            .Returns(new List<TccEntity> { legacy, withDeadline, withCover, forwarded, cancellation, changes });

        var result = await new FindCoordinationPendingUseCase(_tccs, _users).Execute(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data.AwaitingHardCoverCount);
        Assert.Equal([1L, 2L], result.Data.AwaitingHardCover.Select(item => item.TccId));
        Assert.Equal("TCC sem t\u00edtulo", result.Data.AwaitingHardCover[0].Title);
        Assert.Equal(["legacy@example.org"], result.Data.AwaitingHardCover[0].StudentNames);
        Assert.Equal("2026-11-18T09:00:00Z", result.Data.AwaitingHardCover[1].FinalSubmissionDeadline);
        Assert.Equal(["cover@example.org", "Aluno"], Assert.Single(result.Data.AwaitingLibraryForward).StudentNames);
        Assert.Equal(["cancel@example.org", "Aluno"], Assert.Single(result.Data.AwaitingCancellation).StudentNames);
        var change = Assert.Single(result.Data.AwaitingAdvisorChanges!);
        Assert.Equal(7, change.AdvisorChangeId);
        Assert.Equal("Solicitante", change.RequesterName);
        Assert.Equal("Orientador atual", change.CurrentAdvisorName);
        Assert.Equal("Lisa", change.SuggestedAdvisorName);
        Assert.Equal(["change@example.org", "Aluno"], change.StudentNames);
    }

    private static UserEntity User(string role, long? campusId = null, string status = "ACTIVE") =>
        new() { Id = 1, Status = status, CampiCourseId = campusId, Profile = [new ProfileEntity { Role = role }] };

    private static UserTccEntity Student(string? name, string? email, CourseEntity? course = null, bool hasCampus = false) =>
        new()
        {
            Profile = new ProfileEntity { Role = "STUDENT" },
            User = new UserEntity
            {
                Name = name!, Email = email!,
                CampiCourse = course is not null || hasCampus ? new CampiCourseEntity { Course = course! } : null
            }
        };

    private static UserTccEntity Advisor(string? name) =>
        new() { Profile = new ProfileEntity { Role = "ADVISOR" }, User = new UserEntity { Name = name! } };
}
