using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindAllTccByFilterUseCase(IUserGateway userGateway, ITccGateway tccGateway)
{
    public async Task<ResultPattern<List<FindAllTccByStatusOrUserIdDTO>>> Execute(string status, long userId)
    {
        var filter = !string.IsNullOrWhiteSpace(status) ? status : userId.ToString();

        var tccs = await tccGateway.FindAllTccByFilter(filter);

        return ResultPattern<List<FindAllTccByStatusOrUserIdDTO>>.SuccessResult(tccs.
            Select(TccFactory.CreateFindAllTccByStatusOrUserIdDTO)
            .ToList());
    }
}