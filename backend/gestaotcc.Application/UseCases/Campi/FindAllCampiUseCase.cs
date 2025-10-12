using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Campi;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Campi;

public class FindAllCampiUseCase(ICourseGateway courseGateway, IAppLoggerGateway<FindAllCampiUseCase> logger)
{
    public async Task<ResultPattern<List<FindAllCampiDTO>>> Execute()
    {
        logger.LogInformation("Iniciando busca para pegar campis");
        
        var campis = await courseGateway.FindAllCampis();

        logger.LogInformation("Construindo DTO para retornar");
        
        var campisReturn = campis.Select(c => new FindAllCampiDTO(
            c.Id,
            c.Name,
            c.CampiCourses.Select(cc => new CourseDetailsForFindAllCampiDTO(cc.CourseId, cc.Course.Name)).ToList()
            )).ToList();
        
        logger.LogInformation("Finalizando busca para pegar campis");
        return ResultPattern<List<FindAllCampiDTO>>.SuccessResult(campisReturn);
    }
}