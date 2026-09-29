using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class RejectTccProposalUseCase(
    ITccGateway tccGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<RejectTccProposalUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(long tccId, RejectTccProposalDTO data, long advisorUserId)
    {
        logger.LogInformation("Iniciando recusa de proposta de TCC {TccId} pelo orientador {AdvisorId}", tccId, advisorUserId);

        if (string.IsNullOrWhiteSpace(data.Reason))
        {
            return ResultPattern<string>.FailureResult("O motivo da recusa é obrigatório", 400);
        }

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("TCC com Id {TccId} não encontrado.", tccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }

        var advisorUserTcc = tcc.UserTccs.FirstOrDefault(ut => ut.UserId == advisorUserId);
        if (advisorUserTcc is null)
        {
            logger.LogWarning("Usuário {AdvisorId} não está vinculado como orientador do TCC {TccId}.", advisorUserId, tccId);
            return ResultPattern<string>.FailureResult("Você não é o orientador desta proposta", 403);
        }

        if (tcc.Status != StatusTccType.PENDING_APPROVAL.ToString())
        {
            logger.LogWarning("TCC {TccId} com status {Status} não está pendente de aprovação.", tccId, tcc.Status);
            return ResultPattern<string>.FailureResult("A proposta não está pendente de aprovação", 409);
        }

        tcc.Status = StatusTccType.REJECTED.ToString();
        tcc.RejectionReason = data.Reason.Trim();

        await tccGateway.Update(tcc);
        logger.LogInformation("Proposta de TCC {TccId} recusada com sucesso. Motivo: {Reason}", tccId, tcc.RejectionReason);

        var studentUserTcc = tcc.UserTccs.FirstOrDefault(ut => ut.Profile.Role == RoleType.STUDENT.ToString());
        if (studentUserTcc?.User != null)
        {
            try
            {
                var emailDto = EmailFactory.CreateSendEmailDTO(studentUserTcc.User, "ADD-USER-TCC");
                await emailGateway.Send(emailDto);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Erro ao enviar e-mail de recusa para o estudante {StudentEmail}: {ErrorMessage}", studentUserTcc.User.Email, ex.Message);
            }
        }

        return ResultPattern<string>.SuccessResult("Proposta de TCC recusada.");
    }
}
