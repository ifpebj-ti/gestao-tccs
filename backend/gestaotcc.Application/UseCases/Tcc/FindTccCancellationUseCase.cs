using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class FindTccCancellationUseCase(ITccGateway tccGateway)
{
    public async Task<ResultPattern<FindTccCancellationDTO?>> Execute(long tccId)
    {
        var tcc = await tccGateway.FindTccCancellation(tccId);
        if (tcc == null || tcc.TccCancellation == null)
        {
            return ResultPattern<FindTccCancellationDTO?>.FailureResult("Cancelamento do TCC não encontrado.", 404);
        }

        var cancellationDTO = TccFactory.CreateFindTccCancellationDTO(tcc);

        return ResultPattern<FindTccCancellationDTO?>.SuccessResult(cancellationDTO);
    }
}
