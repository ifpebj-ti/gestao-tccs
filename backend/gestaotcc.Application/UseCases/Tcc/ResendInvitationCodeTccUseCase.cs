using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ResendInvitationCodeTccUseCase(IEmailGateway emailGateway, ITccGateway tccGateway, IAppLoggerGateway<ResendInvitationCodeTccUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(string userEmail, long tccId)
    {
        logger.LogInformation("Iniciando reenvio de código de convite para o e-mail: {UserEmail} no TccId: {TccId}", userEmail, tccId);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("Falha no reenvio: TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<string>.FailureResult("Erro ao gerar novo código de acesso", 404);
        }
        
        var userInvite = tcc.TccInvites.FirstOrDefault(x => x.Email == userEmail);
        if (userInvite is null)
        {
            logger.LogWarning("Falha no reenvio para TccId {TccId}: Convite para o e-mail {UserEmail} não encontrado.", tccId, userEmail);
            return ResultPattern<string>.FailureResult("Erro ao gerar novo código de acesso", 404);
        }
        
        logger.LogInformation("Convite encontrado para {UserEmail}. Gerando novo código...", userEmail);
        var random = new Random();
        
        var chars = "ABCDEFGHIJKLMONPQRSTUVWXYZ123456789";
        var code = new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        logger.LogDebug("Novo código de convite gerado para {UserEmail}: {InviteCode}", userEmail, code);
        
        userInvite.Code = code;
        userInvite.IsValidCode = true;

        logger.LogInformation("Atualizando convite no banco de dados para o TccInviteId: {TccInviteId}", userInvite.Id);
        await tccGateway.UpdateTccInvite(userInvite);

        logger.LogInformation("Enviando e-mail de convite atualizado para: {UserEmail}", userEmail);
        var emailDto = EmailFactory.CreateSendEmailDTO(userInvite, "INVITE-USER");
        await emailGateway.Send(emailDto);
        
        logger.LogInformation("Reenvio de código de convite para {UserEmail} no TccId {TccId} concluído com sucesso.", userEmail, tccId);
        return ResultPattern<string>.SuccessResult();
    }
}