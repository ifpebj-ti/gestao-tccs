using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Course;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Course;

public class CreateCourseUseCase(ICourseGateway courseGateway, IAppLoggerGateway<CreateCourseUseCase> logger)
{
    public async Task<ResultPattern<CourseEntity>> Execute(CreateCourseDTO dto)
    {
        logger.LogInformation("Iniciando a criação do curso");

        var course = new CourseEntity(0, dto.Name, dto.Level);
        var result = await courseGateway.InsertCourse(course, dto.CampiId);

        logger.LogInformation("Curso criado com sucesso");
        return ResultPattern<CourseEntity>.SuccessResult(result);
    }
}
