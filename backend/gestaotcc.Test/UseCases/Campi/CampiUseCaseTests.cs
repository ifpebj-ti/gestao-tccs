using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Campi;
using gestaotcc.Domain.Dtos.Campi;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using NSubstitute;

namespace gestaotcc.Test.UseCases.Campi;

public class CampiUseCaseTests
{
    private readonly ICourseGateway _courseGateway = Substitute.For<ICourseGateway>();
    private readonly IAppLoggerGateway<CreateCampiUseCase> _loggerCreate = Substitute.For<IAppLoggerGateway<CreateCampiUseCase>>();
    private readonly IAppLoggerGateway<DeleteCampiUseCase> _loggerDelete = Substitute.For<IAppLoggerGateway<DeleteCampiUseCase>>();
    private readonly IAppLoggerGateway<FindAllCampiUseCase> _loggerFindAll = Substitute.For<IAppLoggerGateway<FindAllCampiUseCase>>();
    private readonly IAppLoggerGateway<UpdateCampiUseCase> _loggerUpdate = Substitute.For<IAppLoggerGateway<UpdateCampiUseCase>>();

    // ───────────── CreateCampiUseCase ─────────────

    [Fact]
    public async Task CreateCampi_ShouldReturnSuccess_WhenCampiIsCreated()
    {
        var dto = new CreateCampiDTO("Campus Recife", "Recife");
        var created = new CampiEntity(1, dto.Name, dto.City, new List<CampiCourseEntity>());

        _courseGateway.InsertCampi(Arg.Any<CampiEntity>()).Returns(Task.FromResult(created));

        var useCase = new CreateCampiUseCase(_courseGateway, _loggerCreate);
        var result = await useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Campus Recife", result.Data?.Name);
        Assert.Equal("Recife", result.Data?.City);
    }

    // ───────────── DeleteCampiUseCase ─────────────

    [Fact]
    public async Task DeleteCampi_ShouldReturnFailure_WhenCampiNotFound()
    {
        _courseGateway.FindCampiById(99).Returns(Task.FromResult<CampiEntity>(null!));

        var useCase = new DeleteCampiUseCase(_courseGateway, _loggerDelete);
        var result = await useCase.Execute(99);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task DeleteCampi_ShouldReturnSuccess_WhenCampiExists()
    {
        var campi = new CampiEntity(1, "Campus Paulista", "Paulista", new List<CampiCourseEntity>());
        _courseGateway.FindCampiById(1).Returns(Task.FromResult(campi));
        _courseGateway.DeleteCampi(1).Returns(Task.CompletedTask);

        var useCase = new DeleteCampiUseCase(_courseGateway, _loggerDelete);
        var result = await useCase.Execute(1);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        await _courseGateway.Received(1).DeleteCampi(1);
    }

    // ───────────── FindAllCampiUseCase ─────────────

    [Fact]
    public async Task FindAllCampi_ShouldReturnListOfCampis()
    {
        var course = new CourseEntity(1, "Sistemas de Informação", "TECNOLOGIA");
        var campiCourse = new CampiCourseEntity(1,
            new CampiEntity(1, "Campus Recife", "Recife", new List<CampiCourseEntity>()),
            course, new List<gestaotcc.Domain.Entities.User.UserEntity>(), 1, 1);

        var campis = new List<CampiEntity>
        {
            new CampiEntity { Id = 1, Name = "Campus Recife", City = "Recife",
                CampiCourses = new List<CampiCourseEntity> { campiCourse } }
        };
        _courseGateway.FindAllCampis().Returns(Task.FromResult(campis));

        var useCase = new FindAllCampiUseCase(_courseGateway, _loggerFindAll);
        var result = await useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("Campus Recife", result.Data[0].Name);
        Assert.Single(result.Data[0].Courses);
        Assert.Equal("Sistemas de Informação", result.Data[0].Courses[0].Name);
    }

    [Fact]
    public async Task FindAllCampi_ShouldReturnEmptyList_WhenNoCampisExist()
    {
        _courseGateway.FindAllCampis().Returns(Task.FromResult(new List<CampiEntity>()));

        var useCase = new FindAllCampiUseCase(_courseGateway, _loggerFindAll);
        var result = await useCase.Execute();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Data);
    }

    // ───────────── UpdateCampiUseCase ─────────────

    [Fact]
    public async Task UpdateCampi_ShouldReturnFailure_WhenCampiNotFound()
    {
        var dto = new UpdateCampiDTO(99, "Novo Nome", "Nova Cidade");
        _courseGateway.FindCampiById(99).Returns(Task.FromResult<CampiEntity>(null!));

        var useCase = new UpdateCampiUseCase(_courseGateway, _loggerUpdate);
        var result = await useCase.Execute(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.ErrorDetails?.Status);
    }

    [Fact]
    public async Task UpdateCampi_ShouldReturnSuccess_WhenCampiUpdated()
    {
        var campi = new CampiEntity(1, "Campus Antigo", "Cidade Antiga", new List<CampiCourseEntity>());
        var dto = new UpdateCampiDTO(1, "Campus Atualizado", "Cidade Nova");
        var updated = new CampiEntity(1, dto.Name, dto.City, new List<CampiCourseEntity>());

        _courseGateway.FindCampiById(1).Returns(Task.FromResult(campi));
        _courseGateway.UpdateCampi(Arg.Any<CampiEntity>()).Returns(Task.FromResult(updated));

        var useCase = new UpdateCampiUseCase(_courseGateway, _loggerUpdate);
        var result = await useCase.Execute(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal("Campus Atualizado", result.Data?.Name);
    }
}

