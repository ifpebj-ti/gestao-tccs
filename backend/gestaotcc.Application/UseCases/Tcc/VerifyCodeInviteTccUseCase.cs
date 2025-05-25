using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class VerifyCodeInviteTccUseCase(IUserGateway userGateway, ITccGateway tccGateway)
{
    public async Task<ResultPattern<bool>> Execute(VerifyCodeInviteTccDTO data)
    {
        var user = await userGateway.FindByEmail(data.UserEmail);

        if (user is null)
            return ResultPattern<bool>.FailureResult("Erro ao verificar código", 404);
        
        var tccInvite = user.UserTccs
            .SelectMany(utc => utc.Tcc.TccInvites)
            .FirstOrDefault(invite => invite.Email == user.Email);


        if (tccInvite.Code != data.Code)
            return ResultPattern<bool>.FailureResult("Erro ao verificar código", 409);
        
        tccInvite.IsValidCode = true;

        await userGateway.Update(user);
        
        return ResultPattern<bool>.SuccessResult();
    }
}