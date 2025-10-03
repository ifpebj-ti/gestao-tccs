using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class LinkBankingUserUseCase(ITccGateway tccGateway, IUserGateway userGateway, IProfileGateway profileGateway, IEmailGateway emailGateway, IAppLoggerGateway<LinkBankingUserUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(LinkBankingUserDTO data)
    {
        logger.LogInformation("Iniciando vinculação de usuários de banca ao TccId: {TccId}. InternoId: {InternalId}, ExternoId: {ExternalId}", data.idTcc, data.idInternalBanking, data.idExternalBanking);
        
        var tcc = await tccGateway.FindTccById(data.idTcc);
        if (tcc == null)
        {
            logger.LogWarning("Falha na vinculação: TCC não encontrado para o TccId: {TccId}", data.idTcc);
            return ResultPattern<string>.FailureResult("TCC não encontrado.", 404);
        }
        logger.LogInformation("TCC encontrado: {TccTitle} (Id: {TccId})", tcc.Title, tcc.Id);
        
        var userInternal = await userGateway.FindById(data.idInternalBanking);
        var userExternal = await userGateway.FindById(data.idExternalBanking);

        if (userInternal == null || userExternal == null)
        {
            logger.LogWarning("Falha na vinculação para TccId {TccId}: Usuário(s) não encontrado(s). InternoEncontrado: {InternalFound}, ExternoEncontrado: {ExternalFound}", data.idTcc, userInternal != null, userExternal != null);
            return ResultPattern<string>.FailureResult("Usuário interno ou externo não encontrado.", 404);
        }
        logger.LogInformation("Usuários de banca encontrados. Interno: {InternalUserName}, Externo: {ExternalUserName}", userInternal.Name, userExternal.Name);
        
        var profileEntity = await profileGateway.FindByRole("BANKING");

        if (profileEntity is null)
        {
            logger.LogError("Falha na vinculação para TccId {TccId}: Perfil 'BANKING' não encontrado no sistema.", data.idTcc);
            return ResultPattern<string>.FailureResult("Erro ao buscar perfil.", 404);
        }
        
        logger.LogInformation("Vinculando usuários de banca à entidade TCC...");
        TccFactory.UpdateUsersTccToCreateBanking(tcc, userInternal, profileEntity!);
        TccFactory.UpdateUsersTccToCreateBanking(tcc, userExternal, profileEntity!);

        logger.LogInformation("Salvando alterações no TCC {TccId}...", tcc.Id);
        await tccGateway.Update(tcc);
        
        logger.LogInformation("Enviando e-mails de notificação para os usuários da banca...");
        var emailDtoInternal = EmailFactory.CreateSendEmailDTO(userInternal, tcc, "LINK-BANKING-USER");
        logger.LogDebug("Enviando e-mail para usuário interno: {UserEmail}", userInternal.Email);
        await emailGateway.Send(emailDtoInternal);
        
        var emailDtoExternal = EmailFactory.CreateSendEmailDTO(userExternal, tcc, "LINK-BANKING-USER");
        logger.LogDebug("Enviando e-mail para usuário externo: {UserEmail}", userExternal.Email);
        await emailGateway.Send(emailDtoExternal);

        logger.LogInformation("Usuários de banca vinculados com sucesso ao TccId: {TccId}", data.idTcc);
        return ResultPattern<string>.SuccessResult("Usuário de banca vinculado com sucesso.");
    }
}