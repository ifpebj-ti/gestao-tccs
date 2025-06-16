using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class FindTccUseCase(ITccGateway tccGateway)
{
    public async Task<ResultPattern<FindTccDTO>> Execute(long tccId)
    {
        var tcc = await tccGateway.FindTccInformations(tccId);
        if (tcc is null)
            return ResultPattern<FindTccDTO>.FailureResult("TCC não encontrado.", 404);

        var findTccDTO = TccInfoFactory.CreateFindTccDTO(tcc);
        return ResultPattern<FindTccDTO>.SuccessResult(findTccDTO);
    }
}
