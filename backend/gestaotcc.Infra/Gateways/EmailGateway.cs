using System.Net;
using System.Net.Mail;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Errors;
using Microsoft.Extensions.Configuration;
using Scriban;

namespace gestaotcc.Infra.Gateways;

public class EmailGateway(IConfiguration configuration) : IEmailGateway
{
    public async Task<ResultPattern<bool>> Send(SendEmailDTO emailDto)
    {
        var client = InitializeClient();

        var body = CreateTemplate(emailDto);
        emailDto.EmailBody = body;

        var message = CreateMessage(emailDto);

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
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
            : typeTemplate == "RESEND-INVITE-TCC" ? Path.Combine(directory, "Templates", "resend-invite-tcc-template.html")
            : typeTemplate == "ADD-USER-TCC" ? Path.Combine(directory, "Templates", "add-user-tcc-template.html")
            : typeTemplate == "INVITE-USER" ? Path.Combine(directory, "Templates", "invite-user-template.html") 
            : Path.Combine(directory, "Templates", "update-password-template.html");
        return File.ReadAllText(filePath);
    }
}