using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Factories;

public class EmailFactory
{
    public static SendEmailDTO CreateSendEmailDTO(UserEntity user, string typeSend)
    {
        Dictionary<string, Object> variables = new Dictionary<string, Object>();
        variables.Add("username", user.Name);
        variables.Add("accesscode", user.AccessCode != null ?  user.AccessCode.Code : "");
        

        var chooseSubject = (typeSend == "CREATE-USER" || typeSend == "AUTO-REGISTER-USER") ? "Bem-vindo(a) ao Gestão TCC" 
            : typeSend == "INVITE-USER" ? "Solicitação de inclusão de Discente" 
            : typeSend == "ADD-USER-TCC" ? "Adição ao TCC com sucesso"
            : typeSend == "SEND-PENDING-SIGNATURE" ? "Pendência de assinatura"
            : "Alteração de senha";

        var emailDTO = new SendEmailDTO("", chooseSubject, user.Email, typeSend, variables);

        return emailDTO;
    }
    public static SendEmailDTO CreateSendEmailDTO(TccInviteEntity tccInvite, string typeSend)
    {
        Dictionary<string, Object> variables = new Dictionary<string, Object>();
        variables.Add("code", tccInvite.Code);
        variables.Add("email", tccInvite.Email);

        var chooseSubject = "Solicitação de inclusão de Discente"; 

        var emailDTO = new SendEmailDTO("", chooseSubject, tccInvite.Email, typeSend, variables);

        return emailDTO;
    }
    public static SendEmailDTO CreateSendEmailDTO(SendPendingSignatureDTO data)
    {
        Dictionary<string, Object> variables = new Dictionary<string, Object>();
        variables.Add("username", data.UserName);
        variables.Add("tccTitle", data.TccTitle);
        variables.Add("documentNames", data.DocumentNames);

        var chooseSubject = "Assinatura pendente"; 

        var emailDTO = new SendEmailDTO("", chooseSubject, data.UserEmail, "SEND-PENDING-SIGNATURE", variables);

        return emailDTO;
    }
}