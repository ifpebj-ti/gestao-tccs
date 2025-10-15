using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Campi;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Campi;

public class FindAllCourseByCampiCourseIdUseCase(ICourseGateway courseGateway, IAppLoggerGateway<FindAllCourseByCampiCourseIdUseCase> logger)
{
    public async Task<ResultPattern<List<FindAllCourseByCampiCourseIdDTO>>> Execute(long campiCourseId)
    {
        logger.LogInformation("Iniciando busca de todos os cursos para o campiCourseid {id}", campiCourseId);
        var courses = await courseGateway.FindAllCoursesByCampiCourseId(campiCourseId);

        logger.LogInformation("Cosntruindo DTO para retornar cursos");
        var coursesReturn = courses.Select(c => new FindAllCourseByCampiCourseIdDTO(
            c.Id,
            c.Name
            )).ToList();
        
        logger.LogInformation("Finalizando busca de cursos");
        return ResultPattern<List<FindAllCourseByCampiCourseIdDTO>>.SuccessResult(coursesReturn);
    }
}