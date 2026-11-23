using gestaotcc.Application.Factories;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Entities.TccSchedule;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Test.Factories;

public class EmailFactoryTests
{
    private const string Recipient = "aluno@example.org";
    private const string Name = "Ana";

    [Theory]
    [InlineData("CREATE-USER", "Bem-vindo(a) ao Gest\u00e3o TCC", "123456")]
    [InlineData("AUTO-REGISTER-USER", "Bem-vindo(a) ao Gest\u00e3o TCC", "123456")]
    [InlineData("INVITE-USER", "Solicita\u00e7\u00e3o de inclus\u00e3o de Discente", "123456")]
    [InlineData("ADD-USER-TCC", "Adi\u00e7\u00e3o ao TCC com sucesso", null)]
    [InlineData("LINK-BANKING-USER", "Vincula\u00e7\u00e3o de usu\u00e1rio de banca", null)]
    [InlineData("SEND-PENDING-SIGNATURE", "Pend\u00eancia de assinatura", null)]
    [InlineData("UPDATE-PASSWORD", "Altera\u00e7\u00e3o de senha", "654321")]
    public void AccountEmail_UsesTheSubjectAndVariablesForTheSelectedTemplate(string template, string subject, string? code)
    {
        var user = new UserEntity { Email = Recipient, Name = Name, AccessCode = code is null ? null : new() { Code = code } };

        var email = EmailFactory.CreateSendEmailDTO(user, template);

        Assert.Equal(subject, email.Subject);
        Assert.Equal(template, email.TypeTemplate);
        Assert.Equal(Recipient, email.Recipient);
        Assert.Equal(Name, email.Variables["username"]);
        Assert.Equal(code ?? "", email.Variables["accesscode"]);
    }

    [Theory]
    [InlineData("RESEND-INVITE-TCC", "Lembrete: Cadastro pendente no Gest\u00e3o TCC")]
    [InlineData("INVITE-USER", "Convite para participar de projeto de TCC - Gest\u00e3o TCC")]
    public void Invitation_IncludesTheCodeAndDistinguishesAnInitialInviteFromAReminder(string template, string subject)
    {
        var invitation = new TccInviteEntity { Email = Recipient, Code = "123456" };

        var email = EmailFactory.CreateSendEmailDTO(invitation, template);

        Assert.Equal(subject, email.Subject);
        Assert.Equal(template, email.TypeTemplate);
        Assert.Equal(Recipient, email.Recipient);
        Assert.Equal(invitation.Code, email.Variables["code"]);
        Assert.Equal(invitation.Email, email.Variables["email"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invite-token")]
    public void SignatureNotice_IncludesTheTokenOnlyForAnExternalRecipient(string? token)
    {
        var details = new List<SendPendingSignatureDetailsDTO> { new("Anexo", null) };

        var email = EmailFactory.CreateSendEmailDTO(new SendPendingSignatureDTO(Recipient, Name, details, "Projeto", token));

        Assert.Equal("SEND-PENDING-SIGNATURE", email.TypeTemplate);
        Assert.Same(details, email.Variables["details"]);
        Assert.Equal("Projeto", email.Variables["tccTitle"]);
        if (string.IsNullOrEmpty(token)) Assert.DoesNotContain("token", email.Variables.Keys);
        else Assert.Equal(token, email.Variables["token"]);
    }

    [Theory]
    [InlineData(null, null, "TCC")]
    [InlineData("Reuso de software", "Resumo do projeto", "Reuso de software")]
    public void ScheduleNotices_UseConsistentCalendarDataAcrossRecipients(string? title, string? summary, string expectedTitle)
    {
        var tcc = new TccEntity { Id = 21, Title = title, Summary = summary };
        var schedule = Schedule();
        var notices = new (SendEmailDTO Email, string Template, bool HasAttachment)[]
        {
            (EmailFactory.CreateSendEmailDTO(new UserEntity { Name = Name, Email = Recipient }, tcc, schedule), "SCHEDULE-TCC", false),
            (EmailFactory.CreateInternalBankingScheduleNotice(Name, Recipient, tcc, schedule), "BANKING-INTERNAL-SCHEDULE", false),
            (EmailFactory.CreateExternalBankingScheduleNotice(Name, Recipient, "Externo", "invite-token", tcc, schedule, true), "BANKING-INVITE", true),
            (EmailFactory.CreateAdvisorScheduleNotice(Name, Recipient, tcc, schedule, false), "SCHEDULE-TCC-ADVISOR", false),
            (EmailFactory.CreateStudentScheduleNotice(Name, Recipient, tcc, schedule), "SCHEDULE-TCC-STUDENT", false),
            (EmailFactory.CreateParticipantScheduleNotice(Name, Recipient, tcc, schedule, true), "SCHEDULE-TCC-PARTICIPANT", true),
            (EmailFactory.CreateExternalBankingReminder(Name, Recipient, tcc, schedule), "BANKING-REMINDER", false),
            (EmailFactory.CreateInternalBankingReminder(Name, Recipient, tcc, schedule), "BANKING-INTERNAL-REMINDER", false)
        };

        foreach (var (email, template, hasAttachment) in notices)
        {
            Assert.Equal(template, email.TypeTemplate);
            Assert.Equal(Recipient, email.Recipient);
            Assert.Equal(expectedTitle, email.Variables["titulo_tcc"]);
            Assert.Equal(summary ?? "Resumo n\u00e3o informado.", email.Variables["resumo_tcc"]);
            Assert.Equal(schedule.Location, email.Variables["local"]);
            Assert.Equal(hasAttachment, email.Variables["has_tcc_attachment"]);
            Assert.Contains("UID:defesa-tcc-21@", email.CalendarIcsContent);
            Assert.Contains("DTSTART:20261118T140000Z", email.CalendarIcsContent);
            Assert.Contains("DTEND:20261118T160000Z", email.CalendarIcsContent);
        }
        Assert.Equal("invite-token", notices.Single(notice => notice.Template == "BANKING-INVITE").Email.Variables["token"]);
    }

    [Theory]
    [InlineData(null, false, "TCC", "", "")]
    [InlineData("Projeto", true, "Projeto", "18/11/2026 14:00", "Audit\u00f3rio")]
    public void EvaluationNotices_ShowScheduleOnlyWhenItExists(
        string? title, bool scheduled, string expectedTitle, string expectedDate, string expectedLocation)
    {
        var tcc = new TccEntity { Title = title, TccSchedule = scheduled ? Schedule() : null };
        var external = EmailFactory.CreateExternalBankingEvaluationNotice(Name, Recipient, "invite-token", tcc);
        var internalNotice = EmailFactory.CreateInternalBankingEvaluationNotice(Name, Recipient, tcc);

        foreach (var email in new[] { external, internalNotice })
        {
            Assert.Contains(expectedTitle, email.Subject);
            Assert.Equal(expectedTitle, email.Variables["titulo_tcc"]);
            Assert.Equal(expectedDate, email.Variables["data_horario"]);
            Assert.Equal(expectedLocation, email.Variables["local"]);
        }
        Assert.Equal("invite-token", external.Variables["token"]);
        Assert.DoesNotContain("token", internalNotice.Variables.Keys);
    }

    private static TccScheduleEntity Schedule() => new()
    {
        ScheduledDate = new DateTime(2026, 11, 18, 14, 0, 0, DateTimeKind.Utc),
        Location = "Audit\u00f3rio"
    };
}
