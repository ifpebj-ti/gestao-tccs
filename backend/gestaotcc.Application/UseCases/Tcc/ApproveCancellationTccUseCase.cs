using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class ApproveCancellationTccUseCase(ITccGateway tccGateway, IAppLoggerGateway<ApproveCancellationTccUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(long tccId)
    {
        logger.LogInformation("Iniciando aprovação de cancelamento para o TccId: {TccId}", tccId);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("Falha na aprovação: TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (tcc.TccCancellation is null)
        {
            logger.LogWarning("Falha na aprovação para TccId {TccId}: TCC não possui uma solicitação de cancelamento.", tccId);
            return ResultPattern<string>.FailureResult("TCC não possui solicitação de cancelamento", 409);
        }
        try
        {
            logger.LogInformation("Aprovando solicitação e atualizando status do TCC {TccId} para CANCELED.", tccId);
            tcc.TccCancellation.Status = "APPROVED";
            tcc.Status = StatusTccType.CANCELED.ToString();
            await tccGateway.Update(tcc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro de banco de dados ao tentar aprovar o cancelamento do TccId: {TccId}", tccId);
            return ResultPattern<string>.FailureResult("Erro ao aprovar cancelamento do TCC", 500);
        }
        
        logger.LogInformation("Cancelamento do TCC {TccId} aprovado com sucesso.", tccId);
        return ResultPattern<string>.SuccessResult("Cancelamento do TCC aprovado com sucesso.");
    }
}