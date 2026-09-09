using System.Threading.Tasks;

namespace gestaotcc.Application.UseCases;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlContent);
}
