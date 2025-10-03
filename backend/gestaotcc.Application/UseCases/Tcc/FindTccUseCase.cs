using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class FindTccUseCase(ITccGateway tccGateway, IAppLoggerGateway<FindTccUseCase> logger)
{
    public async Task<ResultPattern<FindTccDTO>> Execute(long tccId)
    {
        logger.LogInformation("Iniciando busca por informações do TccId: {TccId}", tccId);

        var tcc = await tccGateway.FindTccInformations(tccId);
        if (tcc is null)
        {
            logger.LogWarning("TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<FindTccDTO>.FailureResult("TCC não encontrado.", 404);
        }

        logger.LogInformation("TCC encontrado para o TccId: {TccId}. Mapeando para DTO.", tccId);
        var findTccDTO = TccInfoFactory.CreateFindTccDTO(tcc);
        
        logger.LogInformation("Busca e mapeamento concluídos com sucesso para TccId: {TccId}.", tccId);
        return ResultPattern<FindTccDTO>.SuccessResult(findTccDTO);
    }
}