using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindAllTccByFilterUseCase(IUserGateway userGateway, ITccGateway tccGateway, IAppLoggerGateway<FindAllTccByFilterUseCase> logger)
{
    public async Task<ResultPattern<List<FindAllTccByFilterDTO>>> Execute(TccFilterDTO tccFilter, long campiId)
    {
        logger.LogInformation("Iniciando busca de TCCs com filtro. UserId: {UserId}, Status: {Status}", tccFilter.UserId, tccFilter.StatusTcc);

        var tccs = await tccGateway.FindAllTccByFilter(tccFilter, campiId);
        
        logger.LogInformation("Busca no gateway concluída. {TccCount} TCCs encontrados para o filtro aplicado.", tccs.Count);

        var resultDTO = tccs.
            Select(TccFactory.CreateFindAllTccByStatusOrUserIdDTO)
            .ToList();
        
        logger.LogInformation("Mapeamento para DTO concluído. Retornando {TccCount} TCCs.", resultDTO.Count);

        return ResultPattern<List<FindAllTccByFilterDTO>>.SuccessResult(resultDTO);
    }
}