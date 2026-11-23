using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.Helpers;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class FindTccUseCase(ITccGateway tccGateway, IUserGateway userGateway, IAppLoggerGateway<FindTccUseCase> logger)
{
    public async Task<ResultPattern<FindTccDTO>> Execute(long tccId, long requesterId)
    {
        logger.LogInformation("Iniciando busca por informações do TccId: {TccId}", tccId);

        var requester = await userGateway.FindById(requesterId);
        var tcc = requester is { Status: "ACTIVE" } ? await tccGateway.FindTccInformations(tccId) : null;
        if (tcc is null || !TccReadAuthorization.Allows(requester!, tcc))
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
