using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public record ProposalInvitationDTO(long Id, long TccId, string? Title, string? Summary, string RequestedBy);
public record ParticipantConsentDTO(string Name, string Status);
public record ProposalDeclinedDTO(long Id, long TccId, string? Title, string DeclinedBy);

public class ProposalParticipantsUseCase(IProposalGateway proposals, ITccGateway tccs, IUserGateway users,
    IEmailGateway? email = null, IAppLoggerGateway<ProposalParticipantsUseCase>? logger = null)
{
    public static bool Eligible(UserEntity? user) => user?.Status == "ACTIVE" && user.Profile.Any(x => x.Role == "STUDENT");

    public static bool Active(TccEntity tcc) => tcc.Status is "IN_PROGRESS" or "PENDING_APPROVAL" or "AWAITING_PARTICIPANTS";

    private static UserEntity? Owner(TccEntity tcc) => tcc.UserTccs.FirstOrDefault(x => x.Profile.Role == "STUDENT" &&
        !tcc.ParticipantInvitations.Any(i => i.UserId == x.UserId))?.User;

    public async Task<ResultPattern<List<ProposalDeclinedDTO>>> Notifications(long userId)
    {
        if (!Eligible(await users.FindById(userId)))
            return ResultPattern<List<ProposalDeclinedDTO>>.FailureResult("Conta discente ativa necessária.", 403);
        return ResultPattern<List<ProposalDeclinedDTO>>.SuccessResult((await proposals.DeclinedFor(userId))
            .Select(x => new ProposalDeclinedDTO(x.Id, x.TccId, x.Tcc.Title, x.Email)).ToList());
    }

    public Task<ResultPattern<string>> Dismiss(long invitationId, long userId) => proposals.Atomic(async () =>
    {
        var invitation = await proposals.Find(invitationId);
        var tcc = invitation == null ? null : await tccs.FindTccById(invitation.TccId);
        if (invitation == null || invitation.Status != "DECLINED" || tcc == null || Owner(tcc)?.Id != userId)
            return ResultPattern<string>.FailureResult("Notificação não encontrada.", 404);
        if (!Eligible(await users.FindById(userId))) return ResultPattern<string>.FailureResult("Conta discente ativa necessária.", 403);
        invitation.NotificationDismissedAt = DateTime.UtcNow;
        await tccs.Update(tcc);
        return ResultPattern<string>.SuccessResult("Notificação lida.");
    });

    public async Task<ResultPattern<List<ParticipantConsentDTO>>> Status(long tccId, long userId)
    {
        if (!Eligible(await users.FindById(userId)))
            return ResultPattern<List<ParticipantConsentDTO>>.FailureResult("Conta discente ativa necessária.", 403);
        var tcc = await tccs.FindTccById(tccId);
        if (tcc == null || !tcc.UserTccs.Any(x => x.UserId == userId && x.Profile.Role == "STUDENT"))
            return ResultPattern<List<ParticipantConsentDTO>>.FailureResult("Proposta não encontrada.", 404);
        var status = new List<ParticipantConsentDTO>();
        foreach (var invitation in tcc.ParticipantInvitations)
            status.Add(new(invitation.UserId.HasValue ? (await users.FindById(invitation.UserId.Value))?.Name ?? invitation.Email : invitation.Email, invitation.Status));
        return ResultPattern<List<ParticipantConsentDTO>>.SuccessResult(status);
    }

    public async Task<ResultPattern<List<ProposalInvitationDTO>>> List(long userId)
    {
        if (!Eligible(await users.FindById(userId)))
            return ResultPattern<List<ProposalInvitationDTO>>.FailureResult("Conta discente ativa necessária.", 403);
        var pending = await proposals.PendingFor(userId);
        return ResultPattern<List<ProposalInvitationDTO>>.SuccessResult(pending.Select(x => new ProposalInvitationDTO(
            x.Id, x.TccId, x.Tcc.Title, x.Tcc.Summary,
            x.Tcc.UserTccs.FirstOrDefault(u => u.Profile.Role == "STUDENT" && !x.Tcc.ParticipantInvitations.Any(i => i.UserId == u.UserId))?.User.Name ?? "Estudante")).ToList());
    }

    public async Task<ResultPattern<string>> Respond(long invitationId, long userId, bool accept)
    {
        UserEntity? advisorToNotify = null;
        UserEntity? ownerToNotify = null;
        var result = await proposals.Atomic(async () =>
        {
            var invitation = await proposals.Find(invitationId);
            var user = await users.FindById(userId);
            if (invitation == null || (invitation.UserId.HasValue && invitation.UserId != userId) ||
                user?.Email.Trim().ToLowerInvariant() != invitation.Email)
                return ResultPattern<string>.FailureResult("Convite não encontrado.", 404);
            if (!Eligible(user)) return ResultPattern<string>.FailureResult("Conta discente ativa necessária.", 403);
            var tcc = await tccs.FindTccById(invitation.TccId);
            if (tcc == null || tcc.Status is not ("AWAITING_PARTICIPANTS" or "PENDING_APPROVAL") || invitation.Status != "PENDING")
                return ResultPattern<string>.FailureResult("Este convite não está mais disponível.", 409);

            if (accept)
            {
                var owner = Owner(tcc);
                if (!Eligible(owner) || user!.CampiCourseId == null || user.CampiCourseId != owner!.CampiCourseId)
                    return ResultPattern<string>.FailureResult("Os participantes devem ser estudantes ativos do mesmo campus e curso.", 409);
                if (tcc.UserTccs.Any(x => x.UserId == userId) || tcc.UserTccs.Count(x => x.Profile.Role == "STUDENT") >= 4)
                    return ResultPattern<string>.FailureResult("Participante duplicado ou limite de integrantes atingido.", 409);
                var existing = await tccs.FindAllTccByFilter(new TccFilterDTO(userId, null), 0);
                if (existing.Any(x => x.Id != tcc.Id && Active(x)))
                    return ResultPattern<string>.FailureResult("Você já possui um TCC em andamento ou proposta pendente.", 409);
                tcc.UserTccs.Add(new UserTccEntity
                {
                    Tcc = tcc,
                    TccId = tcc.Id,
                    User = user!,
                    UserId = user!.Id,
                    Profile = user.Profile.First(x => x.Role == "STUDENT"),
                    BindingDate = DateTime.UtcNow
                });
                invitation.Status = "ACCEPTED";
            }
            else
            {
                invitation.Status = "DECLINED";
                ownerToNotify = Owner(tcc);
                tcc.Status = "CANCELED";
                foreach (var pending in tcc.ParticipantInvitations.Where(x => x.Status == "PENDING"))
                { pending.Status = "CANCELED"; pending.RespondedAt = DateTime.UtcNow; }
            }
            invitation.RespondedAt = DateTime.UtcNow;
            invitation.UserId = userId;
            invitation.User = user;
            if (accept && tcc.ParticipantInvitations.All(x => x.Status == "ACCEPTED")) tcc.Status = "PENDING_APPROVAL";
            await tccs.Update(tcc);
            if (accept && tcc.ParticipantInvitations.All(x => x.Status == "ACCEPTED") && email != null)
            {
                advisorToNotify = tcc.UserTccs.FirstOrDefault(x => x.Profile.Role is "ADVISOR" or "COORDINATOR" or "SUPERVISOR")?.User;
            }
            return ResultPattern<string>.SuccessResult(accept ? "Participação aceita." : "Convite recusado e proposta cancelada.");
        });
        if (result.IsSuccess && advisorToNotify != null && email != null)
        {
            try
            {
                var delivery = await email.Send(gestaotcc.Application.Factories.EmailFactory.CreateSendEmailDTO(advisorToNotify, "ADD-USER-TCC"));
                if (delivery?.IsFailure == true) logger?.LogWarning("Falha ao notificar orientador após os aceites do convite {InvitationId}.", invitationId);
            }
            catch (Exception) { logger?.LogWarning("Falha ao notificar orientador após os aceites do convite {InvitationId}.", invitationId); }
        }
        if (result.IsSuccess && ownerToNotify != null && email != null)
        {
            try { await email.Send(new("", "Proposta de TCC: participação recusada", ownerToNotify.Email, "PROPOSAL-DECLINED", new())); }
            catch (Exception) { logger?.LogWarning("Falha ao notificar solicitante sobre a recusa do convite {InvitationId}.", invitationId); }
        }
        return result;
    }
}
