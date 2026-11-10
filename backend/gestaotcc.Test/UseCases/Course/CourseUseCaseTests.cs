using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Course;
using gestaotcc.Domain.Dtos.Course;
using gestaotcc.Domain.Entities.Course;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Course;

public class CourseUseCaseTests
{
    private readonly ICourseGateway _courseGateway = Substitute.For<ICourseGateway>();
    private readonly IAppLoggerGateway<CreateCourseUseCase> _loggerCreate = Substitute.For<IAppLoggerGateway<CreateCourseUseCase>>();
    private readonly IAppLoggerGateway<DeleteCourseUseCase> _loggerDelete = Substitute.For<IAppLoggerGateway<DeleteCourseUseCase>>();
    private readonly IAppLoggerGateway<UpdateCourseUseCase> _loggerUpdate = Substitute.For<IAppLoggerGateway<UpdateCourseUseCase>>();

    // ───────────── CreateCourseUseCase ─────────────

    [Fact]
    public async Task CreateCourse_ShouldReturnSuccess_WhenCourseIsCreated()
    {
        var dto = new CreateCourseDTO("Sistemas de Informação", "TECNOLOGIA", 1);
        var created = new CourseEntity(1, dto.Name, dto.Level);

        _courseGateway.InsertCourse(Arg.Any<CourseEntity>(), dto.CampiId)
            .Returns(Task.FromResult(created));

        var useCase = new CreateCourseUseCase(_courseGateway, _loggerCreate);
        var result = await useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sistemas de Informação", result.Data?.Name);
        Assert.Equal("TECNOLOGIA", result.Data?.Level);
    }

    // ───────────── DeleteCourseUseCase ─────────────

    [Fact]
    public async Task DeleteCourse_ShouldReturnFailure_WhenCourseNotFound()
    {
        _courseGateway.FindCourseById(99).Returns(Task.FromResult<CourseEntity>(null!));

        var useCase = new DeleteCourseUseCase(_courseGateway, _loggerDelete);
        var result = await useCase.Execute(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task DeleteCourse_ShouldReturnSuccess_WhenCourseExists()
    {
        var course = new CourseEntity(1, "Engenharia", "BACHARELADO");
        _courseGateway.FindCourseById(1).Returns(Task.FromResult(course));
        _courseGateway.DeleteCourse(1).Returns(Task.CompletedTask);

        var useCase = new DeleteCourseUseCase(_courseGateway, _loggerDelete);
        var result = await useCase.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        await _courseGateway.Received(1).DeleteCourse(1);
    }

    // ───────────── UpdateCourseUseCase ─────────────

    [Fact]
    public async Task UpdateCourse_ShouldReturnFailure_WhenCourseNotFound()
    {
        var dto = new UpdateCourseDTO(99, "Novo Nome", "BACHARELADO");
        _courseGateway.FindCourseById(99).Returns(Task.FromResult<CourseEntity>(null!));

        var useCase = new UpdateCourseUseCase(_courseGateway, _loggerUpdate);
        var result = await useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task UpdateCourse_ShouldReturnSuccess_WhenCourseUpdated()
    {
        var course = new CourseEntity(1, "Curso Antigo", "TECNOLOGIA");
        var dto = new UpdateCourseDTO(1, "Curso Atualizado", "BACHARELADO");
        var updated = new CourseEntity(1, dto.Name, dto.Level);

        _courseGateway.FindCourseById(1).Returns(Task.FromResult(course));
        _courseGateway.UpdateCourse(Arg.Any<CourseEntity>()).Returns(Task.FromResult(updated));

        var useCase = new UpdateCourseUseCase(_courseGateway, _loggerUpdate);
        var result = await useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Curso Atualizado", result.Data?.Name);
        Assert.Equal("BACHARELADO", result.Data?.Level);
    }
}

