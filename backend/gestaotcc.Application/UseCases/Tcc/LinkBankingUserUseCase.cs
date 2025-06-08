using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class LinkBankingUserUseCase(ITccGateway tccGateway, IUserGateway userGateway, IProfileGateway profileGateway, IEmailGateway emailGateway)
{
    public async Task<ResultPattern<string>> Execute(LinkBankingUserDTO data)
    {
        // Verifica se o TCC existe
        var tcc = await tccGateway.FindTccById(data.idTcc);
        if (tcc == null)
        {
            return ResultPattern<string>.FailureResult("TCC não encontrado.", 404);
        }
        // Verifica se os usuários existem
        var userInternal = await userGateway.FindById(data.idInternalBanking);
        var userExternal = await userGateway.FindById(data.idExternalBanking);

        if (userInternal == null || userExternal == null)
        {
            return ResultPattern<string>.FailureResult("Usuário interno ou externo não encontrado.", 404);
        }
        var profileEntity = await profileGateway.FindByRole("BANKING");

        TccFactory.UpdateUsersTcc(tcc, userInternal, profileEntity!);
        TccFactory.UpdateUsersTcc(tcc, userExternal, profileEntity!);

        await tccGateway.Update(tcc);

        // Envia email para os usuários vinculados
        var emailDtoInternal = EmailFactory.CreateSendEmailDTO(userInternal, tcc, "LINK-BANKING-USER");
        var emailDtoExternal = EmailFactory.CreateSendEmailDTO(userExternal, tcc, "LINK-BANKING-USER");
        await emailGateway.Send(emailDtoInternal);
        await emailGateway.Send(emailDtoExternal);

        return ResultPattern<string>.SuccessResult("Usuário de banca vinculado com sucesso.");
    }
}