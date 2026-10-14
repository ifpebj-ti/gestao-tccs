using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;

public class ChangeOwnPasswordUseCase(IUserGateway userGateway, IBcryptGateway bcryptGateway)
{
    public async Task<ResultPattern<string>> Execute(long userId, ChangeOwnPasswordDTO data)
    {
        if (userId <= 0) return ResultPattern<string>.FailureResult("Sessão inválida.", 401);
        if (string.IsNullOrEmpty(data.CurrentPassword) || string.IsNullOrEmpty(data.NewPassword) ||
            data.NewPassword.Length < 8 || Encoding.UTF8.GetByteCount(data.CurrentPassword) > 72 ||
            Encoding.UTF8.GetByteCount(data.NewPassword) > 72 || data.NewPassword == data.CurrentPassword)
            return ResultPattern<string>.FailureResult("Informe a senha atual e uma nova senha diferente, com pelo menos 8 caracteres e até 72 bytes.", 400);
        var user = await userGateway.FindById(userId);
        if (user is null || user.Status != "ACTIVE")
            return ResultPattern<string>.FailureResult("A conta não está disponível para alteração de senha.", 403);
        if (!bcryptGateway.VerifyHashPassword(user, data.CurrentPassword))
            return ResultPattern<string>.FailureResult("A senha atual não confere.", 400);
        var hash = bcryptGateway.GenerateHashPassword(data.NewPassword);
        if (!await userGateway.UpdateOwnPassword(userId, user.Password, hash))
            return ResultPattern<string>.FailureResult("A conta foi alterada. Entre novamente e tente outra vez.", 409);
        return ResultPattern<string>.SuccessResult();
    }
}
