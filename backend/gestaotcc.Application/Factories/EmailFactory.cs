using gestaotcc.Domain.Dtos.Email;
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

        var chooseSubject = typeSend == "CREATE-USER" ? "Bem-vindo(a) ao Gestão tcc" 
            : typeSend == "INVITE-USER" ? "Solicitação de inclusão de Discente" 
            : typeSend == "ADD-USER-TCC" ? "Adição ao tcc com sucesso"
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
}