using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Course;

public class DeleteCourseUseCase(ICourseGateway courseGateway, IAppLoggerGateway<DeleteCourseUseCase> logger)
{
    public async Task<ResultPattern<bool>> Execute(long id)
    {
        logger.LogInformation("Iniciando a deleção do curso");

        var course = await courseGateway.FindCourseById(id);
        if (course == null)
        {
            return ResultPattern<bool>.FailureResult("Curso não encontrado", 404);
        }

        await courseGateway.DeleteCourse(id);

        logger.LogInformation("Curso deletado com sucesso");
        return ResultPattern<bool>.SuccessResult(true);
    }
}
