using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class FindTccCancellationUseCase(ITccGateway tccGateway, IAppLoggerGateway<FindTccCancellationUseCase> logger)
{
    public async Task<ResultPattern<FindTccCancellationDTO?>> Execute(long tccId)
    {
        logger.LogInformation("Iniciando busca por solicitação de cancelamento para o TccId: {TccId}", tccId);

        var tcc = await tccGateway.FindTccCancellation(tccId);
        if (tcc == null || tcc.TccCancellation == null)
        {
            logger.LogWarning("Solicitação de cancelamento não encontrada para o TccId: {TccId}. TccEncontrado: {TccFound}, SolicitacaoEncontrada: {CancellationFound}", 
                tccId, 
                tcc != null, 
                tcc?.TccCancellation != null);
            return ResultPattern<FindTccCancellationDTO?>.FailureResult("Cancelamento do TCC não encontrado.", 404);
        }

        logger.LogInformation("Solicitação de cancelamento encontrada para o TccId: {TccId}. Mapeando para DTO.", tccId);
        var cancellationDTO = TccCancellationFactory.CreateFindTccCancellationDTO(tcc);

        logger.LogInformation("Busca e mapeamento concluídos com sucesso para TccId: {TccId}.", tccId);
        return ResultPattern<FindTccCancellationDTO?>.SuccessResult(cancellationDTO);
    }
}