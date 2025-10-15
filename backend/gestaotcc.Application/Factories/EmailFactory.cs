using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.TccSchedule;

namespace gestaotcc.Application.Factories;

public class EmailFactory
{
    public static SendEmailDTO CreateSendEmailDTO(UserEntity user, string typeSend)
    {
        Dictionary<string, Object> variables = new Dictionary<string, Object>();
        variables.Add("username", user.Name);
        

        var chooseSubject = (typeSend == "CREATE-USER" || typeSend == "AUTO-REGISTER-USER") ? "Bem-vindo(a) ao Gestão TCC" 
            : typeSend == "INVITE-USER" ? "Solicitação de inclusão de Discente" 
            : typeSend == "ADD-USER-TCC" ? "Adição ao TCC com sucesso"
            : typeSend == "LINK-BANKING-USER" ? "Vinculação de usuário de banca"
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

    public static SendEmailDTO CreateSendEmailDTO(UserEntity user, TccEntity tcc, string typeSend)
    {
        Dictionary<string, Object> variables = new Dictionary<string, Object>();
        variables.Add("username", user.Name);
        variables.Add("tituloTcc", tcc.Title!);

        var chooseSubject = "Vinculação de usuário de banca";

        var emailDTO = new SendEmailDTO("", chooseSubject, user.Email, typeSend, variables);
      
        return emailDTO;
    }

    public static SendEmailDTO CreateSendEmailDTO(SendPendingSignatureDTO data)
    {
        Dictionary<string, object> variables = new Dictionary<string, object>();
        variables.Add("username", data.UserName);
        variables.Add("tccTitle", data.TccTitle);
        variables.Add("details", data.Details);  // Passa a lista de detalhes para o template

        var chooseSubject = "Assinatura pendente";

        var emailDTO = new SendEmailDTO("", chooseSubject, data.UserEmail, "SEND-PENDING-SIGNATURE", variables);

        return emailDTO;
    }


    public static SendEmailDTO CreateSendEmailDTO(UserEntity user, TccEntity tcc, TccScheduleEntity tccSchedule)
    {
        Dictionary<string, Object> variables = new Dictionary<string, Object>();
        variables.Add("username", user.Name);
        variables.Add("titulo_tcc", tcc.Title!);
        variables.Add("resumo_tcc", tcc.Summary!);
        variables.Add("data_horario", tccSchedule.ScheduledDate.ToString("dd/MM/yyyy HH:mm"));
        variables.Add("local", tccSchedule.Location);
        
        var chooseSubject = "Agendamento de defesa do TCC"; 
        
        var emailDTO = new SendEmailDTO("", chooseSubject, user.Email, "SCHEDULE-TCC", variables);
        
        return emailDTO;
    }
}