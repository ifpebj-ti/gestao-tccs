using System.Threading.Tasks;
using gestaotcc.Application.UseCases;

namespace gestaotcc.Infra.Gateways;

public class FakeEmailSender : IEmailSender
{
    public Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        return Task.CompletedTask;
    }
}
