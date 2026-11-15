using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class UpdatePasswordUseCase(IUserGateway userGateway, IBcryptGateway bcryptGateway, IAppLoggerGateway<UpdatePasswordUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(UpdatePasswordDTO data)
    {
        var user = await userGateway.FindByEmail(data.UserEmail);
        if (user is null) return ResultPattern<string>.FailureResult("Não foi possível alterar a senha.", 404);
        if (user.Status == "INACTIVE") return ResultPattern<string>.FailureResult("A conta não está disponível.", 403);
        if (user.AccessCode is null || user.AccessCode.IsActive)
            return ResultPattern<string>.FailureResult("Código de acesso não validado.", 409);
        if (user.AccessCode.ExpirationDate <= DateTime.UtcNow)
            return ResultPattern<string>.FailureResult("Código de acesso expirado.", 409);
        if (user.AccessCode.IsUserUpdatePassword)
            return ResultPattern<string>.FailureResult("Código de acesso já utilizado.", 409);
        if (string.IsNullOrEmpty(data.UserPassword) || data.UserPassword.Length < 8 ||
            Encoding.UTF8.GetByteCount(data.UserPassword) > 72)
            return ResultPattern<string>.FailureResult("A senha deve ter pelo menos 8 caracteres e até 72 bytes.", 400);
        var hash = bcryptGateway.GenerateHashPassword(data.UserPassword);
        if (!await userGateway.ResetPassword(user.Id, user.Password, user.AccessCode.Id, user.AccessCode.Code, hash))
            return ResultPattern<string>.FailureResult("A recuperação expirou ou já foi utilizada. Solicite outro código.", 409);
        logger.LogInformation("Senha redefinida com código de uso único para UserId {UserId}.", user.Id);
        return ResultPattern<string>.SuccessResult();
    }
}
