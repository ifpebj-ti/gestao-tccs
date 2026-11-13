using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Semester;
using gestaotcc.Domain.Dtos.Semester;
using gestaotcc.Domain.Entities.Semester;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Semester;

public class SemesterUseCaseTests
{
    private readonly ISemesterGateway _semesterGateway = Substitute.For<ISemesterGateway>();
    private readonly IAppLoggerGateway<CreateSemesterUseCase> _loggerCreate = Substitute.For<IAppLoggerGateway<CreateSemesterUseCase>>();
    private readonly IAppLoggerGateway<DeleteSemesterUseCase> _loggerDelete = Substitute.For<IAppLoggerGateway<DeleteSemesterUseCase>>();
    private readonly IAppLoggerGateway<UpdateSemesterUseCase> _loggerUpdate = Substitute.For<IAppLoggerGateway<UpdateSemesterUseCase>>();

    // ───────────── CreateSemesterUseCase ─────────────

    [Fact]
    public async Task CreateSemester_ShouldReturnSuccess_WhenSemesterIsCreated()
    {
        var dto = new CreateSemesterDTO
        {
            Name = "2024.1",
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 7, 31),
            IsActive = true
        };
        var created = new SemesterEntity(1, dto.Name, dto.StartDate, dto.EndDate, dto.IsActive);

        _semesterGateway.Insert(Arg.Any<SemesterEntity>()).Returns(Task.FromResult(created));

        var useCase = new CreateSemesterUseCase(_semesterGateway, _loggerCreate);
        var result = await useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("2024.1", result.Data?.Name);
        Assert.True(result.Data?.IsActive);
    }

    // ───────────── DeleteSemesterUseCase ─────────────

    [Fact]
    public async Task DeleteSemester_ShouldReturnFailure_WhenSemesterNotFound()
    {
        _semesterGateway.FindById(99).Returns(Task.FromResult<SemesterEntity?>(null));

        var useCase = new DeleteSemesterUseCase(_semesterGateway, _loggerDelete);
        var result = await useCase.Execute(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task DeleteSemester_ShouldReturnSuccess_WhenSemesterExists()
    {
        var semester = new SemesterEntity(1, "2024.1", new DateTime(2024, 2, 1), new DateTime(2024, 7, 31), true);
        _semesterGateway.FindById(1).Returns(Task.FromResult<SemesterEntity?>(semester));
        _semesterGateway.Delete(1).Returns(Task.CompletedTask);

        var useCase = new DeleteSemesterUseCase(_semesterGateway, _loggerDelete);
        var result = await useCase.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        await _semesterGateway.Received(1).Delete(1);
    }

    // ───────────── FindActiveSemestersUseCase ─────────────

    [Fact]
    public async Task FindActiveSemesters_ShouldReturnOnlyActiveSemesters()
    {
        var active = new SemesterEntity(1, "2024.1", new DateTime(2024, 2, 1), new DateTime(2024, 7, 31), true);
        _semesterGateway.FindActive().Returns(Task.FromResult(new List<SemesterEntity> { active }));

        var useCase = new FindActiveSemestersUseCase(_semesterGateway);
        var result = await useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("2024.1", result.Data[0].Name);
        Assert.True(result.Data[0].IsActive);
    }

    [Fact]
    public async Task FindActiveSemesters_ShouldReturnEmptyList_WhenNoActiveSemesters()
    {
        _semesterGateway.FindActive().Returns(Task.FromResult(new List<SemesterEntity>()));

        var useCase = new FindActiveSemestersUseCase(_semesterGateway);
        var result = await useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }

    // ───────────── UpdateSemesterUseCase ─────────────

    [Fact]
    public async Task UpdateSemester_ShouldReturnFailure_WhenSemesterNotFound()
    {
        _semesterGateway.FindById(99).Returns(Task.FromResult<SemesterEntity?>(null));
        var dto = new CreateSemesterDTO { Name = "X", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(90), IsActive = false };

        var useCase = new UpdateSemesterUseCase(_semesterGateway, _loggerUpdate);
        var result = await useCase.Execute(99, dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task UpdateSemester_ShouldReturnSuccess_WhenSemesterUpdated()
    {
        var semester = new SemesterEntity(1, "2024.1", new DateTime(2024, 2, 1), new DateTime(2024, 7, 31), true);
        var dto = new CreateSemesterDTO
        {
            Name = "2024.2",
            StartDate = new DateTime(2024, 8, 1),
            EndDate = new DateTime(2024, 12, 31),
            IsActive = true
        };
        var updated = new SemesterEntity(1, dto.Name, dto.StartDate, dto.EndDate, dto.IsActive);

        _semesterGateway.FindById(1).Returns(Task.FromResult<SemesterEntity?>(semester));
        _semesterGateway.Update(Arg.Any<SemesterEntity>()).Returns(Task.FromResult(updated));

        var useCase = new UpdateSemesterUseCase(_semesterGateway, _loggerUpdate);
        var result = await useCase.Execute(1, dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("2024.2", result.Data?.Name);
    }
}

