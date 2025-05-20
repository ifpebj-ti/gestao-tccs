using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;
public class UpdatePasswordUseCase(IUserGateway userGateway, IBcryptGateway bcryptGateway)
{
    public async Task<ResultPattern<string>> Execute(UpdatePasswordDTO data)
    {
        var user = await userGateway.FindByEmail(data.UserEmail);
        if (user is null)
            return ResultPattern<string>.FailureResult("Erro ao alterar senha. Por favor verifique as informações e tente novamente.", 404);
        if (user.AccessCode!.IsActive)
            return ResultPattern<string>.FailureResult("Código de acesso ainda não validado. Por favor tente novamente.", 409);
        var duration = user.AccessCode!.ExpirationDate - DateTime.UtcNow;
        if (duration <= TimeSpan.Zero)
            return ResultPattern<string>.FailureResult("Código de acesso expirado. Por favor gere outro e tente novamente.", 409);
        if (user.AccessCode.IsUserUpdatePassword)
            return ResultPattern<string>.FailureResult("Código de acesso já utilizado. Por favor gere outro e tente novamente.", 409);

        user.Password = bcryptGateway.GenerateHashPassword(data.UserPassword);
        user.AccessCode.IsUserUpdatePassword = true;
        user.Status = user.Status != "INACTIVE" ? "ACTIVE" : user.Status;

        await userGateway.Update(user);

        return ResultPattern<string>.SuccessResult();
    }
}
