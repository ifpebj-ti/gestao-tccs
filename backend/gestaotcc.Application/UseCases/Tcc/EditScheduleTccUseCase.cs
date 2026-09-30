using gestaotcc.Application.Gateways;
using gestaotcc.Application.Factories;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Entities.TccBankingMember;

namespace gestaotcc.Application.UseCases.Tcc;
public class EditScheduleTccUseCase(
    ITccGateway tccGateway, 
    IAppLoggerGateway<EditScheduleTccUseCase> logger,
    IUserGateway? userGateway = null, 
    IProfileGateway? profileGateway = null)
{
    public async Task<ResultPattern<string>> Execute(ScheduleTccDTO data)
    {
        logger.LogInformation("Iniciando edição de agendamento de defesa para o TccId: {TccId}", data.IdTcc);

        var tcc = await tccGateway.FindTccScheduling(data.IdTcc);
        if (tcc is null)
        {
            logger.LogWarning("Falha na edição de agendamento: TCC não encontrado para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (tcc.TccSchedule is null)
        {
            logger.LogWarning("Falha na edição de agendamento para TccId {TccId}: TCC não possui um agendamento para editar.", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC ainda não possui agendamento de defesa", 409);
        }
        try
        {
            logger.LogInformation("Agendamento atual para TccId {TccId}: Data: {CurrentDate}, Local: {CurrentLocation}", data.IdTcc, tcc.TccSchedule.ScheduledDate, tcc.TccSchedule.Location);

            if (data.ScheduleDate is not null && data.ScheduleTime is not null)
            {
                var newDateTime = DateTime.SpecifyKind(data.ScheduleDate.Value.ToDateTime(data.ScheduleTime.Value), DateTimeKind.Utc);
                logger.LogInformation("Atualizando data do agendamento para: {NewDateTime}", newDateTime);
                tcc.TccSchedule.ScheduledDate = newDateTime;
            }

            if (data.ScheduleLocation is not null)
            {
                logger.LogInformation("Atualizando local do agendamento para: {NewLocation}", data.ScheduleLocation);
                tcc.TccSchedule.Location = data.ScheduleLocation;
            }

            if (data.BankingMembers != null)
            {
                var bankingProfile = profileGateway != null ? await profileGateway.FindByRole("BANKING") : null;
                var baseUser = tcc.UserTccs.FirstOrDefault()?.User;

                foreach (var memberDto in data.BankingMembers)
                {
                    var existingMember = tcc.BankingMembers.FirstOrDefault(m => m.Email.Equals(memberDto.Email, StringComparison.OrdinalIgnoreCase));
                    if (existingMember != null)
                    {
                        existingMember.Name = memberDto.Name;
                        existingMember.Role = memberDto.Role;
                    }
                    else
                    {
                        var token = Guid.NewGuid().ToString("N");
                        var member = new TccBankingMemberEntity(memberDto.Name, memberDto.Email, memberDto.Role, token, tcc.Id);
                        tcc.BankingMembers.Add(member);
                    }

                    if (userGateway != null && bankingProfile != null)
                    {
                        var existingUser = await userGateway.FindByEmail(memberDto.Email);
                        if (existingUser == null)
                        {
                            var tempUser = new gestaotcc.Domain.Entities.User.UserEntity
                            {
                                Name = memberDto.Name,
                                Email = memberDto.Email,
                                Password = Guid.NewGuid().ToString("N"),
                                Status = "ACTIVE",
                                CampiCourseId = baseUser?.CampiCourseId,
                                Profile = new List<gestaotcc.Domain.Entities.Profile.ProfileEntity> { bankingProfile }
                            };

                            TccFactory.UpdateUsersTccToCreateBanking(tcc, tempUser, bankingProfile);
                        }
                        else
                        {
                            if (!tcc.UserTccs.Any(ut => ut.UserId == existingUser.Id))
                            {
                                TccFactory.UpdateUsersTccToCreateBanking(tcc, existingUser, bankingProfile);
                            }
                        }
                    }
                }
            }

            // Garante que o orientador esteja presente na banca
            var advisorUser = tcc.UserTccs.FirstOrDefault(ut => ut.Profile?.Role == gestaotcc.Domain.Enums.RoleType.ADVISOR.ToString())?.User;
            if (advisorUser != null && !tcc.BankingMembers.Any(m => m.Email.Equals(advisorUser.Email, StringComparison.OrdinalIgnoreCase)))
            {
                var token = Guid.NewGuid().ToString("N");
                var advisorMember = new TccBankingMemberEntity(advisorUser.Name, advisorUser.Email, "Orientador(a)", token, tcc.Id);
                tcc.BankingMembers.Add(advisorMember);
            }
            
            logger.LogInformation("Salvando alterações do agendamento para o TccId: {TccId}", data.IdTcc);
            await tccGateway.Update(tcc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro de banco de dados ao tentar editar o agendamento de defesa para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("Erro ao editar agendamento de defesa do TCC", 500);
        }
        
        logger.LogInformation("Agendamento de defesa do TCC {TccId} editado com sucesso.", data.IdTcc);
        return ResultPattern<string>.SuccessResult("Agendamento de defesa do TCC editado com sucesso.");
    }
}