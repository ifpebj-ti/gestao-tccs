using System.Net;
using System.Net.Mail;
using System.Security.Authentication;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Exceptions;
using Microsoft.Extensions.Configuration;
using Scriban;

namespace gestaotcc.Infra.Gateways;

public class EmailGateway(IConfiguration configuration) : IEmailGateway
{
    public async Task<ResultPattern<bool>> Send(SendEmailDTO emailDto)
    {

        try
        {
            var client = InitializeClient();

            var body = CreateTemplate(emailDto);
            emailDto.EmailBody = body;

            var message = CreateMessage(emailDto);

            await client.SendMailAsync(message).ConfigureAwait(false);
        }
        catch (SmtpException ex)
        {
            Console.WriteLine($"SMTP Exception: {ex}");
            return ResultPattern<bool>.FailureResult(ex.Message, 500);
        }
        catch (AuthenticationException ex)
        {
            Console.WriteLine($"Auth Exception: {ex}");
            return ResultPattern<bool>.FailureResult(ex.Message, 500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Other Exception: {ex}");
            return ResultPattern<bool>.FailureResult(ex.Message, 500);
        }

        return ResultPattern<bool>.SuccessResult();
    }

    private SmtpClient InitializeClient()
    {
        var mailSettings = configuration.GetSection("MailSettings");
        var host = mailSettings.GetValue<string>("Host");
        var port = mailSettings.GetValue<int>("Port");
        var username = mailSettings.GetValue<string>("UserName");
        var password = mailSettings.GetValue<string>("Password");
        
        var client = new SmtpClient(host, port);
        client.EnableSsl = true;
        client.UseDefaultCredentials = false;
        client.Credentials = new NetworkCredential(username, password);

        return client;
    }
    
    private MailMessage CreateMessage(SendEmailDTO data)
    {
        var mailSettings = configuration.GetSection("MailSettings");
        var emailId = mailSettings.GetValue<string>("EmailId");
        
        MailMessage mailMessage = new MailMessage();
        mailMessage.From = new MailAddress(emailId!);
        mailMessage.To.Add(data.Recipient);
        mailMessage.Subject = data.Subject;
        mailMessage.IsBodyHtml = true;
        mailMessage.Body = data.EmailBody;
        
        return mailMessage;
    }

    private string CreateTemplate(SendEmailDTO data)
    {
        var corsSettings = configuration.GetSection("CORS_SETTINGS");
        var urlfront = corsSettings.GetValue<string>("URL_FRONT");
        
        data.Variables.Add("urlfront", urlfront);
        
        var emailBody = GetFileTemplate(data.TypeTemplate);
        var template = Template.Parse(emailBody);

        var directory = Directory.GetCurrentDirectory();

        return template.Render(data.Variables);
    }

    private string GetFileTemplate(string typeTemplate)
    {
        var directory = Directory.GetCurrentDirectory();
        var filePath = typeTemplate == "CREATE-USER" 
            ? Path.Combine(directory, "Templates", "create-user-template.html")
            : typeTemplate == "AUTO-REGISTER-USER" ? Path.Combine(directory, "Templates", "auto-register-user-template.html")
            : typeTemplate == "RESEND-INVITE-TCC" ? Path.Combine(directory, "Templates", "resend-invite-tcc-template.html")
            : typeTemplate == "ADD-USER-TCC" ? Path.Combine(directory, "Templates", "add-user-tcc-template.html")
            : typeTemplate == "INVITE-USER" ? Path.Combine(directory, "Templates", "invite-tcc-template.html")
            : typeTemplate == "LINK-BANKING-USER" ? Path.Combine(directory, "Templates", "link-banking-user-template.html")
            : typeTemplate == "SEND-PENDING-SIGNATURE" ? Path.Combine(directory, "Templates", "send-pending-signature-template.html")
            : typeTemplate == "SCHEDULE-TCC" ? Path.Combine(directory, "Templates", "schedule-tcc-template.html")
            : Path.Combine(directory, "Templates", "update-password-template.html");
        return File.ReadAllText(filePath);
    }
}