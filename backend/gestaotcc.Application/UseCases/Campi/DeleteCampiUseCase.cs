using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Campi;

public class DeleteCampiUseCase(ICourseGateway courseGateway, IAppLoggerGateway<DeleteCampiUseCase> logger)
{
    public async Task<ResultPattern<bool>> Execute(long id)
    {
        logger.LogInformation("Iniciando a deleção do campus");

        var campi = await courseGateway.FindCampiById(id);
        if (campi == null)
        {
            return ResultPattern<bool>.FailureResult("Campus não encontrado", 404);
        }

        await courseGateway.DeleteCampi(id);

        logger.LogInformation("Campus deletado com sucesso");
        return ResultPattern<bool>.SuccessResult(true);
    }
}
