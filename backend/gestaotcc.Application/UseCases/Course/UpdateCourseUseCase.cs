using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Course;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Course;

public class UpdateCourseUseCase(ICourseGateway courseGateway, IAppLoggerGateway<UpdateCourseUseCase> logger)
{
    public async Task<ResultPattern<CourseEntity>> Execute(UpdateCourseDTO dto)
    {
        logger.LogInformation("Iniciando a atualização do curso");

        var course = await courseGateway.FindCourseById(dto.Id);
        if (course == null)
        {
            return ResultPattern<CourseEntity>.FailureResult("Curso não encontrado", 404);
        }

        course.Update(dto.Name, dto.Level);
        var result = await courseGateway.UpdateCourse(course);

        logger.LogInformation("Curso atualizado com sucesso");
        return ResultPattern<CourseEntity>.SuccessResult(result);
    }
}
