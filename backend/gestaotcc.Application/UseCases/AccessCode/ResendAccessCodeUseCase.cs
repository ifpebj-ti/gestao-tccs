using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.AccessCode;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.AccessCode;
public class ResendAccessCodeUseCase(IUserGateway userGateway, IEmailGateway emailGateway)
{
    public async Task<ResultPattern<string>> Execute(ResendAccessCodeDTO data, string combination)
    {
        var user = await userGateway.FindByEmail(data.UserEmail);
        if (user is null)
            return ResultPattern<string>.FailureResult("Erro ao reenviar código de acesso. Por favor verifique as informações e tente novamente.", 404);

        var newAccessCode = GenerateNewAccessCode(combination);
        user.AccessCode!.Code = newAccessCode;
        user.AccessCode!.ExpirationDate = DateTime.UtcNow.AddMinutes(5);
        user.AccessCode.IsActive = true;
        user.AccessCode.IsUserUpdatePassword = false;

        await userGateway.Update(user);

        var sendResult = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(user, "UPDATE-PASSWORD"));
        if (sendResult.IsFailure)
            return ResultPattern<string>.FailureResult(sendResult.Message, 500);

        return ResultPattern<string>.SuccessResult();
    }

    private string GenerateNewAccessCode(string combination)
    {
        var chars = combination;
        var random = new Random();

        var code = new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return code;
    }
}
