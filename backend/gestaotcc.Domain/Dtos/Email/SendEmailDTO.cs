namespace gestaotcc.Domain.Dtos.Email;

public class SendEmailDTO
{
    public string EmailBody;
    public string Subject;
    public string Recipient;
    public string TypeTemplate;
    public Dictionary<string, Object> Variables;

    public SendEmailDTO(string emailBody, string subjet, string recipient, string typeTemplate, Dictionary<string, Object> variables)
    {
        EmailBody = emailBody;
        Subject = subjet;
        Recipient = recipient;
        TypeTemplate = typeTemplate;
        Variables = variables;
    }
}