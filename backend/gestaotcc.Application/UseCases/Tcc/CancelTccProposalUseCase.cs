using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class CancelTccProposalUseCase(
    ITccGateway tccGateway,
    IAppLoggerGateway<CancelTccProposalUseCase> logger,
    IProposalGateway? proposals = null)
{
    public Task<ResultPattern<string>> Execute(long tccId, long studentUserId) =>
        proposals == null ? Cancel(tccId, studentUserId) : proposals.Atomic(() => Cancel(tccId, studentUserId));

    private async Task<ResultPattern<string>> Cancel(long tccId, long studentUserId)
    {
        logger.LogInformation("Iniciando cancelamento de proposta de TCC {TccId} pelo estudante {StudentId}", tccId, studentUserId);

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
            return ResultPattern<string>.FailureResult("Você não tem permissão para cancelar esta proposta", 403);
        }

        if (tcc.Status is not ("PENDING_APPROVAL" or "REJECTED" or "AWAITING_PARTICIPANTS"))
        {
            logger.LogWarning("TCC {TccId} com status {Status} não pode ser cancelado via fluxo de proposta.", tccId, tcc.Status);
            return ResultPattern<string>.FailureResult("Apenas propostas pendentes ou recusadas podem ser canceladas por este fluxo", 409);
        }

        tcc.Status = StatusTccType.CANCELED.ToString();
        foreach (var invitation in tcc.ParticipantInvitations.Where(x => x.Status == "PENDING"))
        { invitation.Status = "CANCELED"; invitation.RespondedAt = DateTime.UtcNow; }

        await tccGateway.Update(tcc);
        logger.LogInformation("Proposta de TCC {TccId} cancelada com sucesso.", tccId);

        return ResultPattern<string>.SuccessResult("Proposta de TCC cancelada.");
    }
}
