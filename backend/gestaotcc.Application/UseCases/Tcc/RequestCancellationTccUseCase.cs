using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class RequestCancellationTccUseCase(ITccGateway tccGateway, IAppLoggerGateway<RequestCancellationTccUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(RequestCancellationTccDTO requestCancellationTcc)
    {
        logger.LogInformation("Iniciando solicitação de cancelamento para o TccId: {TccId}", requestCancellationTcc.IdTcc);

        var tcc = await tccGateway.FindTccById(requestCancellationTcc.IdTcc);
        if (tcc is null)
        {
            logger.LogWarning("Falha na solicitação de cancelamento: TCC não encontrado para o TccId: {TccId}", requestCancellationTcc.IdTcc);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (tcc.TccCancellation is not null)
        {
            logger.LogWarning("Falha na solicitação de cancelamento para TccId {TccId}: TCC já possui uma solicitação de cancelamento.", requestCancellationTcc.IdTcc);
            return ResultPattern<string>.FailureResult("TCC já possui solicitação de cancelamento", 409);
        }

        try
        {
            logger.LogInformation("Criando e atribuindo solicitação de cancelamento para o TccId: {TccId}", requestCancellationTcc.IdTcc);
            var tccCancellation = TccCancellationFactory.CreateTccCancellation(requestCancellationTcc.IdTcc, requestCancellationTcc.Reason);
            tcc.TccCancellation = tccCancellation;
            await tccGateway.Update(tcc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro de banco de dados ao tentar criar a solicitação de cancelamento para o TccId: {TccId}", requestCancellationTcc.IdTcc);
            return ResultPattern<string>.FailureResult("Erro ao solicitar cancelamento do TCC", 500);
        }

        logger.LogInformation("Solicitação de cancelamento para o TCC {TccId} enviada com sucesso.", requestCancellationTcc.IdTcc);
        return ResultPattern<string>.SuccessResult("Solicitação de cancelamento enviada com sucesso.");
    }
}