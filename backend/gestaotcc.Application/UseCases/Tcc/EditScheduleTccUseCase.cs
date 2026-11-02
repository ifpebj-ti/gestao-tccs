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
    IProfileGateway? profileGateway = null,
    IEmailGateway? emailGateway = null)
{
    public async Task<ResultPattern<string>> Execute(ScheduleTccDTO data, long currentUserId)
    {
        logger.LogInformation("Iniciando edição de agendamento de defesa para o TccId: {TccId}", data.IdTcc);

        var tcc = await tccGateway.FindTccScheduling(data.IdTcc);
        if (tcc is null)
        {
            logger.LogWarning("Falha na edição de agendamento: TCC não encontrado para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (!TccAdvisorAuthorization.IsAdvisorOf(tcc, currentUserId))
            return ResultPattern<string>.FailureResult("Somente o orientador deste TCC pode gerenciar a banca.", 403);
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

            var bankingProfile = profileGateway != null ? await profileGateway.FindByRole("BANKING") : null;
            if (data.BankingMembers != null)
            {
                var baseUser = tcc.UserTccs.FirstOrDefault()?.User;
                var retainedMembers = new HashSet<TccBankingMemberEntity>();

                foreach (var memberDto in data.BankingMembers)
                {
                    gestaotcc.Domain.Entities.User.UserEntity? registeredUser = null;
                    if (memberDto.UserId.HasValue)
                    {
                        if (userGateway is null)
                            return ResultPattern<string>.FailureResult("Não foi possível validar o examinador interno.", 500);

                        registeredUser = await userGateway.FindById(memberDto.UserId.Value);
                        if (registeredUser is null ||
                            !string.Equals(registeredUser.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ||
                            registeredUser.Profile is null ||
                            !registeredUser.Profile.Any(profile => profile.Role == "ADVISOR"))
                            return ResultPattern<string>.FailureResult("O usuário selecionado não é um orientador ativo.", 400);
                    }

                    var memberEmail = registeredUser?.Email ?? memberDto.Email;
                    var existingMember = tcc.BankingMembers.FirstOrDefault(m =>
                        (registeredUser != null && m.UserId == registeredUser.Id) ||
                        m.Email.Equals(memberEmail, StringComparison.OrdinalIgnoreCase));
                    if (existingMember != null)
                    {
                        existingMember.Name = registeredUser?.Name ?? memberDto.Name;
                        existingMember.Email = memberEmail;
                        existingMember.Role = registeredUser is null ? memberDto.Role : "Examinador Interno";
                        existingMember.UserId = registeredUser?.Id;
                        retainedMembers.Add(existingMember);
                    }
                    else
                    {
                        var token = Guid.NewGuid().ToString("N");
                        var member = new TccBankingMemberEntity(
                            registeredUser?.Name ?? memberDto.Name,
                            memberEmail,
                            registeredUser is null ? memberDto.Role : "Examinador Interno",
                            token,
                            tcc.Id)
                        {
                            UserId = registeredUser?.Id
                        };
                        tcc.BankingMembers.Add(member);
                        retainedMembers.Add(member);
                    }

                    if (registeredUser != null)
                    {
                        if (bankingProfile is null)
                            return ResultPattern<string>.FailureResult("Perfil de banca não encontrado.", 500);

                        TccFactory.UpdateUsersTccToCreateBanking(tcc, registeredUser, bankingProfile);
                    }
                    else if (userGateway != null && bankingProfile != null)
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
                            if (!tcc.UserTccs.Any(ut => ut.UserId == existingUser.Id && ut.Profile?.Role == "BANKING"))
                            {
                                TccFactory.UpdateUsersTccToCreateBanking(tcc, existingUser, bankingProfile);
                            }
                        }
                    }
                }

                var advisorEmail = tcc.UserTccs
                    .FirstOrDefault(ut => ut.Profile?.Role == gestaotcc.Domain.Enums.RoleType.ADVISOR.ToString())?.User.Email;
                foreach (var member in tcc.BankingMembers.ToList())
                {
                    if (member.Email.Equals(advisorEmail, StringComparison.OrdinalIgnoreCase) || retainedMembers.Contains(member))
                        continue;

                    tcc.BankingMembers.Remove(member);
                }

                var activeBankingEmails = tcc.BankingMembers
                    .Select(member => member.Email)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var activeBankingUserIds = tcc.BankingMembers
                    .Where(member => member.UserId.HasValue)
                    .Select(member => member.UserId!.Value)
                    .ToHashSet();
                var obsoleteBankingLinks = tcc.UserTccs
                    .Where(userTcc => userTcc.Profile?.Role == "BANKING" &&
                        !activeBankingUserIds.Contains(userTcc.UserId) &&
                        !activeBankingEmails.Contains(userTcc.User.Email))
                    .ToList();
                foreach (var link in obsoleteBankingLinks)
                    tcc.UserTccs.Remove(link);
            }

            // Garante que o orientador esteja presente na banca
            var advisorUser = tcc.UserTccs.FirstOrDefault(ut => ut.Profile?.Role == gestaotcc.Domain.Enums.RoleType.ADVISOR.ToString())?.User;
            if (advisorUser != null && !tcc.BankingMembers.Any(m => m.Email.Equals(advisorUser.Email, StringComparison.OrdinalIgnoreCase)))
            {
                var token = Guid.NewGuid().ToString("N");
                var advisorMember = new TccBankingMemberEntity(advisorUser.Name, advisorUser.Email, "Orientador(a)", token, tcc.Id)
                {
                    UserId = advisorUser.Id
                };
                tcc.BankingMembers.Add(advisorMember);
                if (bankingProfile is not null)
                    TccFactory.UpdateUsersTccToCreateBanking(tcc, advisorUser, bankingProfile);
            }
            else if (advisorUser != null)
            {
                var advisorMember = tcc.BankingMembers.First(m => m.Email.Equals(advisorUser.Email, StringComparison.OrdinalIgnoreCase));
                advisorMember.UserId ??= advisorUser.Id;
                if (bankingProfile is not null)
                    TccFactory.UpdateUsersTccToCreateBanking(tcc, advisorUser, bankingProfile);
            }
            
            logger.LogInformation("Salvando alterações do agendamento para o TccId: {TccId}", data.IdTcc);
            await tccGateway.Update(tcc);

            if (emailGateway is not null && tcc.TccSchedule is not null)
            {
                foreach (var member in tcc.BankingMembers
                             .Where(member => member.UserId.HasValue && !member.Grade.HasValue)
                             .DistinctBy(member => member.Email, StringComparer.OrdinalIgnoreCase))
                {
                    try
                    {
                        var notice = EmailFactory.CreateInternalBankingScheduleNotice(member.Name, member.Email, tcc, tcc.TccSchedule);
                        var result = await emailGateway.Send(notice);
                        if (result.IsFailure)
                            logger.LogWarning("Failed to notify internal examiner {Email} about TCC {TccId} schedule change.", member.Email, tcc.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Failed to send schedule change notice to internal examiner {Email}: {Error}", member.Email, ex.Message);
                    }
                }
            }
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
