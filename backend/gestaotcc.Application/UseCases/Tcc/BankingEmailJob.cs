using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;

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
            if (string.IsNullOrWhiteSpace(member.Email)) continue;

            var email = member.UserId.HasValue
                ? EmailFactory.CreateInternalBankingReminder(member.Name, member.Email, tcc, tcc.TccSchedule)
                : EmailFactory.CreateExternalBankingReminder(member.Name, member.Email, tcc, tcc.TccSchedule);
            await emailGateway.Send(email);
        }
    }
}
