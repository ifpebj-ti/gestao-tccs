using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class UpdateTccUseCase(ITccGateway tccGateway, IAppLoggerGateway<UpdateTccUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(UpdateTccDTO data, long tccId)
    {
        logger.LogInformation("Iniciando atualização do tcc de id {tccId}", tccId);
        var tcc = await tccGateway.FindTccById(tccId);

        if (tcc is null)
        {
            logger.LogWarning("Tcc com o id {tccId} não foi encontrado", tccId);
            return ResultPattern<string>.FailureResult("Erro ao buscar tcc", 500);
        }
        logger.LogInformation("Tcc com o id {tccId} encontrado", tccId);
        
        tcc.Title = data.Title;
        tcc.Summary = data.Summary;
        
        logger.LogInformation("Atualizando tcc com o id {tccId}", tccId);
        await tccGateway.Update(tcc);
        
        logger.LogInformation("Finalizando atualização para o tcc com id {tccId}", tccId);
        return ResultPattern<string>.SuccessResult();
    }
}