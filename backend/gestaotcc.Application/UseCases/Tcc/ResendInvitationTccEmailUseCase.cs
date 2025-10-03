using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ResendInvitationTccEmailUseCase(IUserGateway userGateway, ITccGateway tccGateway, IEmailGateway emailGateway, IAppLoggerGateway<ResendInvitationTccEmailUseCase> logger)
{
    public async Task<ResultPattern<bool>> Execute()
    {
        logger.LogInformation("Iniciando tarefa de reenvio de e-mails de convite de TCC pendentes.");

        var tccInvites = await tccGateway.FindAllInviteTcc();
        if (!tccInvites.Any())
        {
            logger.LogInformation("Nenhum convite de TCC pendente encontrado. Tarefa concluída sem ações.");
            return ResultPattern<bool>.SuccessResult();
        }

        logger.LogInformation("{InviteCount} convites de TCC pendentes encontrados para reenvio.", tccInvites.Count);

        foreach (var item in tccInvites)
        {
            logger.LogInformation("Reenviando convite para o e-mail: {UserEmail} (TccInviteId: {TccInviteId})", item.Email, item.Id);
            var emailDto = EmailFactory.CreateSendEmailDTO(item, "RESEND-INVITE-TCC");
            await emailGateway.Send(emailDto);
        }

        logger.LogInformation("Tarefa de reenvio de convites concluída. {InviteCount} e-mails foram despachados.", tccInvites.Count);
        return ResultPattern<bool>.SuccessResult();
    }
}