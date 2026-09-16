using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;

namespace gestaotcc.Application.UseCases.Tcc;

public interface IBankingEmailJob
{
    Task SendReminderEmails(long tccId);
}

public class BankingEmailJob(ITccGateway tccGateway, IEmailGateway emailGateway) : IBankingEmailJob
{
    public async Task SendReminderEmails(long tccId)
    {
        var tcc = await tccGateway.FindTccScheduling(tccId);
        if (tcc == null || tcc.TccSchedule == null) return;

        foreach (var member in tcc.BankingMembers)
        {
            var variables = new Dictionary<string, object>
            {
                { "name", member.Name },
                { "tccTitle", tcc.Title ?? "TCC" },
                { "date", tcc.TccSchedule.ScheduledDate.ToString("dd/MM/yyyy HH:mm") },
                { "location", tcc.TccSchedule.Location },
                { "token", member.AccessToken }
            };

            var emailDto = new SendEmailDTO(
                emailBody: string.Empty,
                subjet: "Lembrete: Defesa de TCC Amanhã",
                recipient: member.Email,
                typeTemplate: "BANKING-REMINDER",
                variables: variables
            );

            await emailGateway.Send(emailDto);
        }
    }
}
