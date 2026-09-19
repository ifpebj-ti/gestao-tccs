using System.Threading.Tasks;
using gestaotcc.Application.UseCases;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using System.Collections.Generic;

namespace gestaotcc.Infra.Gateways;

public class EmailSender : IEmailSender
{
    private readonly IEmailGateway _emailGateway;

    public EmailSender(IEmailGateway emailGateway)
    {
        _emailGateway = emailGateway;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlContent)
    {
        // Envia o e-mail usando o EmailGateway já existente
        var emailDto = new SendEmailDTO(
            emailBody: htmlContent, 
            subjet: subject, 
            recipient: to, 
            typeTemplate: "RAW", 
            variables: new Dictionary<string, object>()
        );

        await _emailGateway.Send(emailDto);
    }
}
