using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class VerifyCodeInviteTccUseCase(IUserGateway userGateway, ITccGateway tccGateway)
{
    public async Task<ResultPattern<bool>> Execute(VerifyCodeInviteTccDTO data)
    {
        var user = await userGateway.FindByEmail(data.UserEmail);

        if (user is not null)
            return ResultPattern<bool>.FailureResult("Erro ao verificar código", 404);

        var tccInvite = await tccGateway.FindInviteTccByEmail(data.UserEmail);
        
        if(!tccInvite!.IsValidCode)
            return ResultPattern<bool>.FailureResult("Erro ao verificar código", 409);

        if (tccInvite!.Code != data.Code)
            return ResultPattern<bool>.FailureResult("Erro ao verificar código", 409);
        
        tccInvite.IsValidCode = false;

        await tccGateway.UpdateTccInvite(tccInvite);
        
        return ResultPattern<bool>.SuccessResult();
    }
}