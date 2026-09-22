using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Entities.TccBankingMember;
using Hangfire;

namespace gestaotcc.Application.UseCases.Tcc;
public class CreateScheduleTccUseCase(ITccGateway tccGateway, IUserGateway userGateway, IProfileGateway profileGateway, IAppLoggerGateway<CreateScheduleTccUseCase> logger, IEmailGateway emailGateway, IBackgroundJobClient backgroundJobClient)
{
    public async Task<ResultPattern<string>> Execute(ScheduleTccDTO data)
    {
        logger.LogInformation("Iniciando criação de agendamento de defesa para o TccId: {TccId}. Data: {ScheduledDate}", data.IdTcc, data.ScheduleDate);

        var tcc = await tccGateway.FindTccScheduling(data.IdTcc);
        if (tcc is null)
        {
            logger.LogWarning("Falha na criação de agendamento: TCC não encontrado para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (tcc.TccSchedule is not null)
        {
            logger.LogWarning("Falha na criação de agendamento para TccId {TccId}: TCC já possui um agendamento.", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC já possui agendamento de defesa", 409);
        }

        if (tcc.Step != gestaotcc.Domain.Enums.StepTccType.PRESENTATION_AND_EVALUATION.ToString())
        {
            logger.LogWarning("Falha na criação de agendamento para TccId {TccId}: TCC não está na etapa de Apresentação e Avaliação. Etapa atual: {Step}", data.IdTcc, tcc.Step);
            return ResultPattern<string>.FailureResult("O agendamento da defesa só é permitido após a conclusão da etapa 4 (Preparação para Apresentação).", 400);
        }

        try
        {
            logger.LogInformation("Criando e atribuindo agendamento para o TccId: {TccId}", data.IdTcc);
            var tccSchedule = TccScheduleFactory.CreateTccSchedule(data);
            tcc.TccSchedule = tccSchedule;

            if (data.BankingMembers != null)
            {
                var bankingProfile = await profileGateway.FindByRole("BANKING");
                var baseUser = tcc.UserTccs.FirstOrDefault()?.User;

                foreach (var memberDto in data.BankingMembers)
                {
                    var token = Guid.NewGuid().ToString("N");
                    var member = new TccBankingMemberEntity(memberDto.Name, memberDto.Email, memberDto.Role, token, tcc.Id);
                    tcc.BankingMembers.Add(member);

                    var existingUser = await userGateway.FindByEmail(memberDto.Email);
                    if (existingUser == null && bankingProfile != null)
                    {
                        var tempUser = new gestaotcc.Domain.Entities.User.UserEntity
                        {
                            Name = memberDto.Name,
                            Email = memberDto.Email,
                            Password = Guid.NewGuid().ToString("N"), // Random password
                            Status = "ACTIVE",
                            CampiCourseId = baseUser?.CampiCourseId,
                            Profile = new List<gestaotcc.Domain.Entities.Profile.ProfileEntity> { bankingProfile }
                        };

                        TccFactory.UpdateUsersTccToCreateBanking(tcc, tempUser, bankingProfile);
                    }
                    else if (existingUser != null && bankingProfile != null)
                    {
                        if (!tcc.UserTccs.Any(ut => ut.UserId == existingUser.Id))
                        {
                            TccFactory.UpdateUsersTccToCreateBanking(tcc, existingUser, bankingProfile);
                        }
                    }
                }
            }

            // Ensure the advisor is always a banking member
            var advisorUser = tcc.UserTccs.FirstOrDefault(ut => ut.Profile.Role == gestaotcc.Domain.Enums.RoleType.ADVISOR.ToString())?.User;
            if (advisorUser != null && !tcc.BankingMembers.Any(m => m.Email.Equals(advisorUser.Email, StringComparison.OrdinalIgnoreCase)))
            {
                var token = Guid.NewGuid().ToString("N");
                var advisorMember = new TccBankingMemberEntity(advisorUser.Name, advisorUser.Email, "Orientador(a)", token, tcc.Id);
                tcc.BankingMembers.Add(advisorMember);
            }

            await tccGateway.Update(tcc);

            // Send instant emails and schedule reminders
            if (tcc.BankingMembers.Any())
            {
                foreach (var member in tcc.BankingMembers)
                {
                    // Instant invite email
                    var variables = new Dictionary<string, object>
                    {
                        { "username", member.Name },
                        { "titulo_tcc", tcc.Title ?? "TCC" },
                        { "resumo_tcc", tcc.Summary ?? "Resumo não informado." },
                        { "data_horario", tccSchedule.ScheduledDate.ToString("dd/MM/yyyy HH:mm") },
                        { "local", tccSchedule.Location },
                        { "token", member.AccessToken },
                        { "role", member.Role }
                    };
                    
                    var emailDto = new gestaotcc.Domain.Dtos.Email.SendEmailDTO(
                        emailBody: string.Empty,
                        subjet: "Convite para Banca Avaliadora",
                        recipient: member.Email,
                        typeTemplate: "BANKING-INVITE",
                        variables: variables
                    );
                    
                    await emailGateway.Send(emailDto);
                }

                // Schedule reminder 1 day before
                var scheduledDateTime = tccSchedule.ScheduledDate;
                var reminderTime = scheduledDateTime.AddDays(-1);
                var delay = reminderTime - DateTime.UtcNow;

                if (delay.TotalMinutes > 0)
                {
                    backgroundJobClient.Schedule<IBankingEmailJob>(
                        job => job.SendReminderEmails(tcc.Id), 
                        delay);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro de banco de dados ao tentar criar o agendamento de defesa para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("Erro ao criar agendamento de defesa do TCC", 500);
        }
        
        logger.LogInformation("Agendamento de defesa do TCC {TccId} criado com sucesso.", data.IdTcc);
        return ResultPattern<string>.SuccessResult("Agendamento de defesa do TCC criado com sucesso.");
    }
}