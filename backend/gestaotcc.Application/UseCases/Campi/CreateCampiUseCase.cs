using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Campi;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Campi;

public class CreateCampiUseCase(ICourseGateway courseGateway, IAppLoggerGateway<CreateCampiUseCase> logger)
{
    public async Task<ResultPattern<CampiEntity>> Execute(CreateCampiDTO dto)
    {
        logger.LogInformation("Iniciando a criação do campus");

        var campi = new CampiEntity(0, dto.Name, dto.City, new List<gestaotcc.Domain.Entities.CampiCourse.CampiCourseEntity>());
        var result = await courseGateway.InsertCampi(campi);

        logger.LogInformation("Campus criado com sucesso");
        return ResultPattern<CampiEntity>.SuccessResult(result);
    }
}
