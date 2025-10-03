using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class SendScheduleEmailUseCase(ITccGateway tccGateway, IEmailGateway emailGateway, IAppLoggerGateway<SendScheduleEmailUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(long tccId)
    {
        logger.LogInformation("Iniciando envio de e-mails de agendamento para o TccId: {TccId}", tccId);

        var tcc = await tccGateway.FindTccScheduling(tccId);
        if (tcc is null)
        {
            logger.LogWarning("Falha no envio de e-mails: TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (tcc.TccSchedule is null)
        {
            logger.LogWarning("Falha no envio de e-mails para TccId {TccId}: TCC não possui um agendamento de defesa.", tccId);
            return ResultPattern<string>.FailureResult("TCC não possui agendamento de defesa", 409);
        }
        try
        {
            logger.LogInformation("Enviando e-mails de agendamento para {UserCount} usuários do TccId {TccId}.", tcc.UserTccs.Count, tccId);
            foreach (var userTcc in tcc.UserTccs)
            {
                logger.LogDebug("Enviando e-mail de agendamento para o usuário: {UserEmail} (UserId: {UserId})", userTcc.User.Email, userTcc.User.Id);
                var emailDto = EmailFactory.CreateSendEmailDTO(userTcc.User, tcc, tcc.TccSchedule);
                await emailGateway.Send(emailDto);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar e-mails de agendamento de defesa para o TccId: {TccId}", tccId);
            return ResultPattern<string>.FailureResult("Erro ao enviar email de agendamento de defesa do TCC", 500);
        }
        
        logger.LogInformation("E-mails de agendamento para o TccId {TccId} enviados com sucesso.", tccId);
        return ResultPattern<string>.SuccessResult("Emails de agendamento de defesa do TCC enviados com sucesso.");
    }
}