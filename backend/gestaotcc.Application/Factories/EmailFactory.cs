using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Factories;

public class EmailFactory
{
    public static SendEmailDTO CreateSendEmailDTO(UserEntity user, string typeSend)
    {
        Dictionary<string, Object> variables = new Dictionary<string, Object>();
        variables.Add("username", user.Name);
        variables.Add("accesscode", user.AccessCode.Code);

        var chooseSubject = typeSend == "CREATE-USER" ? "Bem-vindo(a) ao Contagem de baterias" : "Alteração de senha";

        var emailDTO = new SendEmailDTO("", chooseSubject, user.Email, typeSend, variables);

        return emailDTO;
    }
}