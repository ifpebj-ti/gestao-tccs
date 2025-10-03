using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class VerifyCodeInviteTccUseCase(IUserGateway userGateway, ITccGateway tccGateway, IAppLoggerGateway<VerifyCodeInviteTccUseCase> logger)
{
    public async Task<ResultPattern<bool>> Execute(VerifyCodeInviteTccDTO data)
    {
        logger.LogInformation("Iniciando verificação de código de convite para o e-mail: {UserEmail}", data.UserEmail);
        logger.LogDebug("Código de convite fornecido para {UserEmail}: {InviteCode}", data.UserEmail, data.Code);

        var user = await userGateway.FindByEmail(data.UserEmail);

        if (user is not null)
        {
            logger.LogWarning("Falha na verificação para {UserEmail}: um usuário com este e-mail já existe.", data.UserEmail);
            return ResultPattern<bool>.FailureResult("Erro ao verificar código", 404);
        }

        var tccInvite = await tccGateway.FindInviteTccByEmail(data.UserEmail);

        logger.LogDebug("Convite encontrado para {UserEmail}: TccInviteId {TccInviteId}", data.UserEmail, tccInvite?.Id);
        
        if(!tccInvite!.IsValidCode)
        {
            logger.LogWarning("Falha na verificação para {UserEmail}: o código de convite não é mais válido (TccInviteId: {TccInviteId}).", data.UserEmail, tccInvite.Id);
            return ResultPattern<bool>.FailureResult("Erro ao verificar código", 409);
        }

        if (tccInvite!.Code != data.Code)
        {
            logger.LogWarning("Falha na verificação para {UserEmail}: o código de convite fornecido é inválido (TccInviteId: {TccInviteId}).", data.UserEmail, tccInvite.Id);
            return ResultPattern<bool>.FailureResult("Erro ao verificar código", 409);
        }
        
        logger.LogInformation("Código de convite verificado com sucesso para {UserEmail}. Invalidando o código...", data.UserEmail);
        tccInvite.IsValidCode = false;

        await tccGateway.UpdateTccInvite(tccInvite);
        logger.LogInformation("Código invalidado e salvo no banco de dados para TccInviteId: {TccInviteId}", tccInvite.Id);
        
        return ResultPattern<bool>.SuccessResult();
    }
}