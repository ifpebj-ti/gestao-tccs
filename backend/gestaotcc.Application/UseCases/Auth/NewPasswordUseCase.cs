using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;
public class NewPasswordUseCase(IUserGateway userGateway, IBcryptGateway bcryptGateway, ITccGateway tccGateway, IEmailGateway emailGateway)
{
    public async Task<ResultPattern<string>> Execute(NewPasswordDTO data)
    {
        var user = await userGateway.FindByEmail(data.Email);
        var tccInvite = await tccGateway.FindInviteTccByEmail(data.Email);

        if (user is null || tccInvite is null)
            return ResultPattern<string>.FailureResult("Erro ao criar nova senha. Por favor verifique as informações e tente novamente.", 404);
        if (tccInvite.Email != data.Email ||
                tccInvite.Code != data.InviteCode ||
                tccInvite.IsValidCode) // Aqui a lógica é invertida, pois o código já deve ter sido utilizado na etapa anterior de verificar código do convite do TCC
            return ResultPattern<string>.FailureResult("Erro ao criar nova senha. Por favor verifique as informações e tente novamente.", 409);

        user.Password = bcryptGateway.GenerateHashPassword(data.Password);
        user.Status = user.Status != "INACTIVE" ? "ACTIVE" : user.Status;

        await userGateway.Update(user);

        var emailDTO = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(user, "AUTO-REGISTER-USER"));
        if (emailDTO.IsFailure)
            return ResultPattern<string>.FailureResult(emailDTO.Message, 500);

        return ResultPattern<string>.SuccessResult();
    }
}
