using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.DocumentType;
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
        variables.Add("accesscode", user.AccessCode != null ?  user.AccessCode.Code : "");
        

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

        var chooseSubject = typeSend == "RESEND-INVITE-TCC" 
            ? "Lembrete: Cadastro pendente no Gestão TCC" 
            : "Convite para participar de projeto de TCC - Gestão TCC"; 

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
        if (!string.IsNullOrEmpty(data.Token))
        {
            variables.Add("token", data.Token);
        }

        var chooseSubject = "Assinatura pendente";

        var emailDTO = new SendEmailDTO("", chooseSubject, data.UserEmail, "SEND-PENDING-SIGNATURE", variables);

        return emailDTO;
    }


    public static SendEmailDTO CreateSendEmailDTO(UserEntity user, TccEntity tcc, TccScheduleEntity tccSchedule)
    {
        return CreateSendEmailDTO(user.Name, user.Email, tcc, tccSchedule);
    }

    public static SendEmailDTO CreateSendEmailDTO(string name, string email, TccEntity tcc, TccScheduleEntity tccSchedule)
    {
        Dictionary<string, Object> variables = new Dictionary<string, Object>();
        variables.Add("username", name);
        variables.Add("titulo_tcc", tcc.Title ?? string.Empty);
        variables.Add("resumo_tcc", tcc.Summary ?? string.Empty);
        variables.Add("data_horario", tccSchedule.ScheduledDate.ToString("dd/MM/yyyy HH:mm"));
        variables.Add("local", tccSchedule.Location);
        
        var chooseSubject = "Agendamento de defesa do TCC"; 
        
        var emailDTO = new SendEmailDTO("", chooseSubject, email, "SCHEDULE-TCC", variables);
        
        return emailDTO;
    }

    public static SendEmailDTO CreateInternalBankingScheduleNotice(string name, string email, TccEntity tcc, TccScheduleEntity schedule)
        => CreateInternalBankingScheduleNotice(name, email, tcc, schedule, hasTccAttachment: false);

    public static SendEmailDTO CreateInternalBankingScheduleNotice(
        string name, string email, TccEntity tcc, TccScheduleEntity schedule, bool hasTccAttachment)
    {
        var variables = CreateScheduleVariables(name, tcc, schedule, hasTccAttachment);
        return new SendEmailDTO(string.Empty, $"Banca de TCC: apresentação agendada — {tcc.Title ?? "TCC"}", email, "BANKING-INTERNAL-SCHEDULE", variables);
    }

    public static SendEmailDTO CreateExternalBankingScheduleNotice(
        string name, string email, string role, string token, TccEntity tcc, TccScheduleEntity schedule, bool hasTccAttachment)
    {
        var variables = CreateScheduleVariables(name, tcc, schedule, hasTccAttachment);
        variables["role"] = role;
        variables["token"] = token;
        return new SendEmailDTO(string.Empty, $"Convite para banca de TCC: {tcc.Title ?? "TCC"}", email, "BANKING-INVITE", variables);
    }

    public static SendEmailDTO CreateAdvisorScheduleNotice(
        string name, string email, TccEntity tcc, TccScheduleEntity schedule, bool hasTccAttachment)
    {
        var variables = CreateScheduleVariables(name, tcc, schedule, hasTccAttachment);
        return new SendEmailDTO(string.Empty, $"Apresentação agendada: {tcc.Title ?? "TCC"}", email, "SCHEDULE-TCC-ADVISOR", variables);
    }

    public static SendEmailDTO CreateStudentScheduleNotice(string name, string email, TccEntity tcc, TccScheduleEntity schedule)
    {
        var variables = CreateScheduleVariables(name, tcc, schedule, hasTccAttachment: false);
        return new SendEmailDTO(string.Empty, $"Data da apresentação do seu TCC: {tcc.Title ?? "TCC"}", email, "SCHEDULE-TCC-STUDENT", variables);
    }

    public static SendEmailDTO CreateParticipantScheduleNotice(
        string name, string email, TccEntity tcc, TccScheduleEntity schedule, bool hasTccAttachment)
    {
        var variables = CreateScheduleVariables(name, tcc, schedule, hasTccAttachment);
        return new SendEmailDTO(string.Empty, $"Apresentação de TCC agendada: {tcc.Title ?? "TCC"}", email, "SCHEDULE-TCC-PARTICIPANT", variables);
    }

    public static SendEmailDTO CreateExternalBankingReminder(string name, string email, TccEntity tcc, TccScheduleEntity schedule)
    {
        var variables = CreateScheduleVariables(name, tcc, schedule, hasTccAttachment: false);
        return new SendEmailDTO(string.Empty, $"Lembrete: banca de TCC amanhã — {tcc.Title ?? "TCC"}", email, "BANKING-REMINDER", variables);
    }

    public static SendEmailDTO CreateInternalBankingReminder(string name, string email, TccEntity tcc, TccScheduleEntity schedule)
    {
        var variables = CreateScheduleVariables(name, tcc, schedule, hasTccAttachment: false);
        return new SendEmailDTO(string.Empty, $"Lembrete: banca de TCC amanhã — {tcc.Title ?? "TCC"}", email, "BANKING-INTERNAL-REMINDER", variables);
    }

    public static SendEmailDTO CreateExternalBankingEvaluationNotice(string name, string email, string token, TccEntity tcc)
    {
        var variables = new Dictionary<string, object>
        {
            ["username"] = name,
            ["titulo_tcc"] = tcc.Title ?? "TCC",
            ["data_horario"] = tcc.TccSchedule?.ScheduledDate.ToString("dd/MM/yyyy HH:mm") ?? string.Empty,
            ["local"] = tcc.TccSchedule?.Location ?? string.Empty,
            ["token"] = token
        };
        return new SendEmailDTO(string.Empty, $"Avalie a banca de TCC: {tcc.Title ?? "TCC"}", email, "BANKING-EVALUATION", variables);
    }

    public static SendEmailDTO CreateInternalBankingEvaluationNotice(string name, string email, TccEntity tcc)
    {
        var variables = new Dictionary<string, object>
        {
            ["username"] = name,
            ["titulo_tcc"] = tcc.Title ?? "TCC",
            ["data_horario"] = tcc.TccSchedule?.ScheduledDate.ToString("dd/MM/yyyy HH:mm") ?? string.Empty,
            ["local"] = tcc.TccSchedule?.Location ?? string.Empty
        };

        return new SendEmailDTO(string.Empty, $"Avaliação de banca pendente: {tcc.Title ?? "TCC"}", email, "BANKING-INTERNAL-EVALUATION", variables);
    }

    private static Dictionary<string, object> CreateScheduleVariables(string name, TccEntity tcc, TccScheduleEntity schedule, bool hasTccAttachment)
    {
        return new Dictionary<string, object>
        {
            ["username"] = name,
            ["titulo_tcc"] = tcc.Title ?? "TCC",
            ["resumo_tcc"] = tcc.Summary ?? "Resumo não informado.",
            ["data_horario"] = schedule.ScheduledDate.ToString("dd/MM/yyyy HH:mm"),
            ["local"] = schedule.Location,
            ["has_tcc_attachment"] = hasTccAttachment
        };
    }
}
