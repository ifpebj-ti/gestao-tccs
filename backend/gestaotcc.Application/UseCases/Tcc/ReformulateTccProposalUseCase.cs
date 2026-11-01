using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ReformulateTccProposalUseCase(
    IUserGateway userGateway,
    ITccGateway tccGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<ReformulateTccProposalUseCase> logger,
    IProposalGateway? proposals = null)
{
    public Task<ResultPattern<string>> Execute(long tccId, ReformulateTccProposalDTO data, long studentUserId) =>
        proposals == null ? Reformulate(tccId, data, studentUserId) : proposals.Atomic(() => Reformulate(tccId, data, studentUserId));

    private async Task<ResultPattern<string>> Reformulate(long tccId, ReformulateTccProposalDTO data, long studentUserId)
    {
        logger.LogInformation("Iniciando reformulação de proposta de TCC {TccId} pelo estudante {StudentId}", tccId, studentUserId);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("TCC com Id {TccId} não encontrado.", tccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }

        var studentUserTcc = tcc.UserTccs.FirstOrDefault(ut => ut.UserId == studentUserId && ut.Profile.Role == RoleType.STUDENT.ToString());
        if (studentUserTcc is null)
        {
            logger.LogWarning("Usuário {StudentId} não é o estudante do TCC {TccId}.", studentUserId, tccId);
            return ResultPattern<string>.FailureResult("Você não tem permissão para reformular este TCC", 403);
        }

        if (tcc.Status != StatusTccType.REJECTED.ToString())
        {
            logger.LogWarning("TCC {TccId} com status {Status} não está recusado para permitir reformulação.", tccId, tcc.Status);
            return ResultPattern<string>.FailureResult("Apenas propostas recusadas podem ser reformuladas", 409);
        }

        foreach (var member in tcc.UserTccs.Where(x => x.Profile.Role == "STUDENT"))
        {
            var existing = await tccGateway.FindAllTccByFilter(new TccFilterDTO(member.UserId, null), 0);
            if (existing != null && existing.Any(x => x.Id != tccId && ProposalParticipantsUseCase.Active(x)))
                return ResultPattern<string>.FailureResult("Um integrante já possui outra proposta ou TCC ativo.", 409);
        }
        // A changed proposal requires fresh consent from invited participants.
        foreach (var invitation in tcc.ParticipantInvitations)
        {
            var member = tcc.UserTccs.FirstOrDefault(x => x.UserId == invitation.UserId && x.Profile.Role == "STUDENT");
            if (member != null) tcc.UserTccs.Remove(member);
            invitation.Status = "PENDING"; invitation.RespondedAt = null;
        }
        tcc.Title = data.Title;
        tcc.Summary = data.Summary;
        tcc.Status = tcc.ParticipantInvitations.Count == 0 ? "PENDING_APPROVAL" : "AWAITING_PARTICIPANTS";
        tcc.Step = StepTccType.PROPOSAL_REGISTRATION.ToString();
        tcc.RejectionReason = null;

        if (data.AdvisorId.HasValue && data.AdvisorId.Value > 0)
        {
            var newAdvisor = await userGateway.FindById(data.AdvisorId.Value);
            if (newAdvisor != null && newAdvisor.Profile.Any(p => p.Role == RoleType.ADVISOR.ToString() || p.Role == RoleType.COORDINATOR.ToString() || p.Role == RoleType.SUPERVISOR.ToString()))
            {
                var existingAdvisorUt = tcc.UserTccs.FirstOrDefault(ut => ut.Profile.Role == RoleType.ADVISOR.ToString() || ut.Profile.Role == RoleType.COORDINATOR.ToString() || ut.Profile.Role == RoleType.SUPERVISOR.ToString());
                if (existingAdvisorUt != null)
                {
                    tcc.UserTccs.Remove(existingAdvisorUt);
                }

                var advisorProfile = newAdvisor.Profile.First(p => p.Role == RoleType.ADVISOR.ToString() || p.Role == RoleType.COORDINATOR.ToString() || p.Role == RoleType.SUPERVISOR.ToString());
                tcc.UserTccs.Add(new UserTccEntityBuilder()
                    .WithTcc(tcc)
                    .WithUser(newAdvisor)
                    .WithProfile(advisorProfile)
                    .WithBindingDate(DateTime.UtcNow)
                    .Build());
            }
        }

        await tccGateway.Update(tcc);
        logger.LogInformation("Proposta de TCC {TccId} reformulada e reenviada com sucesso.", tccId);

        var advisorUt = tcc.UserTccs.FirstOrDefault(ut => ut.Profile.Role == RoleType.ADVISOR.ToString() || ut.Profile.Role == RoleType.COORDINATOR.ToString() || ut.Profile.Role == RoleType.SUPERVISOR.ToString());
        if (advisorUt?.User != null && tcc.ParticipantInvitations.Count == 0)
        {
            try
            {
                var emailDto = EmailFactory.CreateSendEmailDTO(advisorUt.User, "ADD-USER-TCC");
                await emailGateway.Send(emailDto);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Erro ao enviar e-mail de reformulação para o orientador {AdvisorEmail}: {ErrorMessage}", advisorUt.User.Email, ex.Message);
            }
        }

        return ResultPattern<string>.SuccessResult("Proposta de TCC reformulada e enviada com sucesso!");
    }
}
