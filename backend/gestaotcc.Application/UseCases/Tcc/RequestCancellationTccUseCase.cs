using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class RequestCancellationTccUseCase(ITccGateway tccGateway)
{
    public async Task<ResultPattern<string>> Execute(RequestCancellationTccDTO requestCancellationTcc)
    {
        var tcc = await tccGateway.FindTccById(requestCancellationTcc.IdTcc);
        if (tcc is null)
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        if (tcc.TccCancellation is not null)
            return ResultPattern<string>.FailureResult("TCC já possui solicitação de cancelamento", 409);

        try
        {
            var tccCancellation = TccCancellationFactory.CreateTccCancellation(requestCancellationTcc.IdTcc, requestCancellationTcc.Reason);
            tcc.TccCancellation = tccCancellation;
            await tccGateway.Update(tcc);
        }
        catch (Exception)
        {
            return ResultPattern<string>.FailureResult("Erro ao solicitar cancelamento do TCC", 500);
        }

        return ResultPattern<string>.SuccessResult("Solicitação de cancelamento enviada com sucesso.");
    }
}
