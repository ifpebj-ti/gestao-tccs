using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindAllTccByFilterUseCase(IUserGateway userGateway, ITccGateway tccGateway)
{
    public async Task<ResultPattern<List<FindAllTccByFilterDTO>>> Execute(TccFilterDTO tccFilter)
    {
        var tccs = await tccGateway.FindAllTccByFilter(tccFilter);

        return ResultPattern<List<FindAllTccByFilterDTO>>.SuccessResult(tccs.
            Select(TccFactory.CreateFindAllTccByStatusOrUserIdDTO)
            .ToList());
    }
}