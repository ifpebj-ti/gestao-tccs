using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Entities.TccBankingMember;
using Hangfire;

namespace gestaotcc.Application.UseCases.Tcc;
public class CreateScheduleTccUseCase(ITccGateway tccGateway, IAppLoggerGateway<CreateScheduleTccUseCase> logger, IEmailGateway emailGateway, IBackgroundJobClient backgroundJobClient)
{
    public async Task<ResultPattern<string>> Execute(ScheduleTccDTO data)
    {
        logger.LogInformation("Iniciando criação de agendamento de defesa para o TccId: {TccId}. Data: {ScheduledDate}", data.IdTcc, data.ScheduleDate);

        var tcc = await tccGateway.FindTccScheduling(data.IdTcc);
        if (tcc is null)
        {
            logger.LogWarning("Falha na criação de agendamento: TCC não encontrado para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (tcc.TccSchedule is not null)
        {
            logger.LogWarning("Falha na criação de agendamento para TccId {TccId}: TCC já possui um agendamento.", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC já possui agendamento de defesa", 409);
        }

        try
        {
            logger.LogInformation("Criando e atribuindo agendamento para o TccId: {TccId}", data.IdTcc);
            var tccSchedule = TccScheduleFactory.CreateTccSchedule(data);
            tcc.TccSchedule = tccSchedule;

            if (data.BankingMembers != null && data.BankingMembers.Any())
            {
                foreach (var memberDto in data.BankingMembers)
                {
                    var token = Guid.NewGuid().ToString("N");
                    var member = new TccBankingMemberEntity(memberDto.Name, memberDto.Email, memberDto.Role, token, tcc.Id);
                    tcc.BankingMembers.Add(member);
                }
            }

            await tccGateway.Update(tcc);

            // Send instant emails and schedule reminders
            if (tcc.BankingMembers.Any())
            {
                foreach (var member in tcc.BankingMembers)
                {
                    // Instant invite email
                    var variables = new Dictionary<string, object>
                    {
                        { "name", member.Name },
                        { "tccTitle", tcc.Title ?? "TCC" },
                        { "date", tccSchedule.ScheduledDate.ToString("dd/MM/yyyy HH:mm") },
                        { "location", tccSchedule.Location },
                        { "token", member.AccessToken },
                        { "role", member.Role }
                    };
                    
                    var emailDto = new gestaotcc.Domain.Dtos.Email.SendEmailDTO(
                        emailBody: string.Empty,
                        subjet: "Convite para Banca Avaliadora",
                        recipient: member.Email,
                        typeTemplate: "BANKING-INVITE",
                        variables: variables
                    );
                    
                    await emailGateway.Send(emailDto);
                }

                // Schedule reminder 1 day before
                var scheduledDateTime = tccSchedule.ScheduledDate;
                var reminderTime = scheduledDateTime.AddDays(-1);
                var delay = reminderTime - DateTime.UtcNow;

                if (delay.TotalMinutes > 0)
                {
                    backgroundJobClient.Schedule<IBankingEmailJob>(
                        job => job.SendReminderEmails(tcc.Id), 
                        delay);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro de banco de dados ao tentar criar o agendamento de defesa para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("Erro ao criar agendamento de defesa do TCC", 500);
        }
        
        logger.LogInformation("Agendamento de defesa do TCC {TccId} criado com sucesso.", data.IdTcc);
        return ResultPattern<string>.SuccessResult("Agendamento de defesa do TCC criado com sucesso.");
    }
}