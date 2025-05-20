using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.AccessCode;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.AccessCode;
public class VerifyAccessCodeUseCase(IUserGateway userGateway)
{
    public async Task<ResultPattern<string>> Execute(VerifyAccessCodeDTO data)
    {
        var user = await userGateway.FindByEmail(data.UserEmail);
        if (user is null)
            return ResultPattern<string>.FailureResult("Erro ao verificar código de acesso. Por favor verifique as informações e tente novamente.", 404);

        if (!user.AccessCode!.IsActive)
            return ResultPattern<string>.FailureResult("Código de acesso já validado. Por favor gere outro e tente novamente.", 409);

        if (!user.AccessCode!.Code.Equals(data.AccessCode))
            return ResultPattern<string>.FailureResult("Erro ao verificar código de acesso. Por favor verifique as informações e tente novamente.", 409);

        var duration = user.AccessCode!.ExpirationDate - DateTime.UtcNow;
        if (duration <= TimeSpan.Zero)
            return ResultPattern<string>.FailureResult("Código de acesso expirado. Por favor gere outro e tente novamente.", 409);

        user.AccessCode.IsActive = false;

        await userGateway.Update(user);

        return ResultPattern<string>.SuccessResult();
    }
}
