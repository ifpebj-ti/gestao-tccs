using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Campi;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Campi;

public class UpdateCampiUseCase(ICourseGateway courseGateway, IAppLoggerGateway<UpdateCampiUseCase> logger)
{
    public async Task<ResultPattern<CampiEntity>> Execute(UpdateCampiDTO dto)
    {
        logger.LogInformation("Iniciando a atualização do campus");

        var campi = await courseGateway.FindCampiById(dto.Id);
        if (campi == null)
        {
            return ResultPattern<CampiEntity>.FailureResult("Campus não encontrado", 404);
        }

        campi.Update(dto.Name, dto.City);
        var result = await courseGateway.UpdateCampi(campi);

        logger.LogInformation("Campus atualizado com sucesso");
        return ResultPattern<CampiEntity>.SuccessResult(result);
    }
}
