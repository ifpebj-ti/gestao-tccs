using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class SendScheduleEmailUseCase(
    ITccGateway tccGateway,
    IEmailGateway emailGateway,
    IStorageGateway storageGateway,
    IAppLoggerGateway<SendScheduleEmailUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(long tccId, long currentUserId)
    {
        logger.LogInformation("Iniciando envio dos comunicados da apresentação do TCC {TccId}.", tccId);

        var tcc = await tccGateway.FindTccScheduling(tccId);
        if (tcc is null)
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        if (!TccAdvisorAuthorization.IsAdvisorOf(tcc, currentUserId))
            return ResultPattern<string>.FailureResult("Somente o orientador deste TCC pode gerenciar a banca.", 403);
        if (tcc.TccSchedule is null)
            return ResultPattern<string>.FailureResult("TCC não possui agendamento de defesa", 409);

        try
        {
            var attachment = await TryLoadTccAttachment(tcc);
            var sentEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var member in tcc.BankingMembers)
            {
                if (string.IsNullOrWhiteSpace(member.Email) || !sentEmails.Add(member.Email.Trim()))
                    continue;

                SendEmailDTO email;
                if (member.Role.Contains("Orientador", StringComparison.OrdinalIgnoreCase))
                {
                    email = EmailFactory.CreateAdvisorScheduleNotice(
                        member.Name, member.Email, tcc, tcc.TccSchedule, hasTccAttachment: false);
                }
                else if (member.UserId.HasValue)
                {
                    email = EmailFactory.CreateInternalBankingScheduleNotice(
                        member.Name, member.Email, tcc, tcc.TccSchedule, attachment is not null);
                }
                else
                {
                    email = EmailFactory.CreateExternalBankingScheduleNotice(
                        member.Name, member.Email, member.Role, member.AccessToken,
                        tcc, tcc.TccSchedule, attachment is not null);
                }

                email.Attachments = member.Role.Contains("Orientador", StringComparison.OrdinalIgnoreCase) ? null : attachment;
                await Send(email);
            }

            foreach (var userTcc in tcc.UserTccs)
            {
                var user = userTcc.User;
                if (user is null || string.IsNullOrWhiteSpace(user.Email) || !sentEmails.Add(user.Email.Trim()))
                    continue;

                var role = userTcc.Profile?.Role;
                SendEmailDTO email;
                if (string.Equals(role, "STUDENT", StringComparison.OrdinalIgnoreCase))
                {
                    email = EmailFactory.CreateStudentScheduleNotice(user.Name, user.Email, tcc, tcc.TccSchedule);
                }
                else if (string.Equals(role, "ADVISOR", StringComparison.OrdinalIgnoreCase))
                {
                    email = EmailFactory.CreateAdvisorScheduleNotice(
                        user.Name, user.Email, tcc, tcc.TccSchedule, hasTccAttachment: false);
                }
                else
                {
                    email = EmailFactory.CreateParticipantScheduleNotice(
                        user.Name, user.Email, tcc, tcc.TccSchedule, attachment is not null);
                    email.Attachments = attachment;
                }

                await Send(email);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar comunicados da apresentação do TCC {TccId}.", tccId);
            return ResultPattern<string>.FailureResult("Erro ao enviar os comunicados do agendamento do TCC", 500);
        }

        logger.LogInformation("Comunicados da apresentação do TCC {TccId} enviados.", tccId);
        return ResultPattern<string>.SuccessResult("Agenda enviada com mensagens adequadas ao perfil de cada participante.");
    }

    private async Task<List<EmailAttachmentDTO>?> TryLoadTccAttachment(gestaotcc.Domain.Entities.Tcc.TccEntity tcc)
    {
        if (string.IsNullOrWhiteSpace(tcc.TccFile)) return null;
        try
        {
            var fileBytes = await storageGateway.Download(tcc.TccFile, false);
            if (fileBytes is null || fileBytes.Length == 0) return null;
            return new List<EmailAttachmentDTO>
            {
                new(tcc.TccFile.Replace("filled/", ""), fileBytes, "application/pdf")
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning("Não foi possível anexar o TCC {TccFile} aos comunicados: {Error}", tcc.TccFile, ex.Message);
            return null;
        }
    }

    private async Task Send(SendEmailDTO email)
    {
        var result = await emailGateway.Send(email);
        if (result.IsFailure)
            throw new InvalidOperationException($"Falha ao enviar e-mail para {email.Recipient}: {result.Message}");
    }
}
