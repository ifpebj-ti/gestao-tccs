using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class SendScheduleEmailUseCase(ITccGateway tccGateway, IEmailGateway emailGateway, IStorageGateway storageGateway, IAppLoggerGateway<SendScheduleEmailUseCase> logger)
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
            
            // Tenta baixar o arquivo do TCC se houver
            List<gestaotcc.Domain.Dtos.Email.EmailAttachmentDTO>? attachments = null;
            if (!string.IsNullOrEmpty(tcc.TccFile))
            {
                try
                {
                    var fileBytes = await storageGateway.Download(tcc.TccFile, false);
                    var contentType = "application/pdf"; 
                    attachments = new List<gestaotcc.Domain.Dtos.Email.EmailAttachmentDTO>
                    {
                        new gestaotcc.Domain.Dtos.Email.EmailAttachmentDTO(tcc.TccFile.Replace("filled/", ""), fileBytes, contentType)
                    };
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Não foi possível baixar o arquivo do TCC {TccFile} para o e-mail de agendamento do TccId {TccId}", tcc.TccFile, tccId);
                }
            }

            var sentEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Enviar para os membros da banca (examinadores com anexo)
            if (tcc.BankingMembers != null)
            {
                foreach (var member in tcc.BankingMembers)
                {
                    if (string.IsNullOrWhiteSpace(member.Email) || sentEmails.Contains(member.Email))
                        continue;

                    logger.LogInformation("Enviando e-mail de agendamento para examinador da banca: {UserEmail} ({Name})", member.Email, member.Name);
                    var emailDto = EmailFactory.CreateSendEmailDTO(member.Name, member.Email, tcc, tcc.TccSchedule);
                    emailDto.Attachments = attachments;
                    await emailGateway.Send(emailDto);
                    sentEmails.Add(member.Email);
                }
            }

            // 2. Enviar para os usuários vinculados ao TCC (aluno sem anexo, orientador/outros com anexo)
            foreach (var userTcc in tcc.UserTccs)
            {
                if (userTcc.User == null || string.IsNullOrWhiteSpace(userTcc.User.Email) || sentEmails.Contains(userTcc.User.Email))
                    continue;

                logger.LogDebug("Enviando e-mail de agendamento para o usuário: {UserEmail} (UserId: {UserId})", userTcc.User.Email, userTcc.User.Id);
                var emailDto = EmailFactory.CreateSendEmailDTO(userTcc.User.Name, userTcc.User.Email, tcc, tcc.TccSchedule);

                var isStudent = string.Equals(userTcc.Profile?.Role, "STUDENT", StringComparison.OrdinalIgnoreCase);
                if (!isStudent)
                {
                    emailDto.Attachments = attachments;
                }
                else
                {
                    emailDto.Attachments = null; // Aluno não recebe o anexo do TCC
                }

                await emailGateway.Send(emailDto);
                sentEmails.Add(userTcc.User.Email);
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