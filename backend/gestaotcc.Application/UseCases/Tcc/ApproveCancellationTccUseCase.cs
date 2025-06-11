using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class ApproveCancellationTccUseCase(ITccGateway tccGateway)
{
    public async Task<ResultPattern<string>> Execute(long tccId)
    {
        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        if (tcc.TccCancellation is null)
            return ResultPattern<string>.FailureResult("TCC não possui solicitação de cancelamento", 409);
        try
        {
            tcc.TccCancellation.Status = "APPROVED";
            tcc.Status = StatusTccType.CANCELED.ToString();
            await tccGateway.Update(tcc);
        }
        catch (Exception)
        {
            return ResultPattern<string>.FailureResult("Erro ao aprovar cancelamento do TCC", 500);
        }
        return ResultPattern<string>.SuccessResult("Cancelamento do TCC aprovado com sucesso.");
    }
}
