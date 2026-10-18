using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class RequestAdvisorChangeUseCase(
    ITccGateway tccGateway,
    IUserGateway userGateway,
    IAppLoggerGateway<RequestAdvisorChangeUseCase> logger
)
{
    public async Task<ResultPattern<string>> Execute(RequestAdvisorChangeDTO request, long requesterUserId)
    {
        logger.LogInformation("Iniciando solicitação de alteração de orientador para TccId {TccId} pelo usuário {UserId}", request.TccId, requesterUserId);

        var tcc = await tccGateway.FindTccById(request.TccId);
        if (tcc is null)
        {
            logger.LogWarning("TCC não encontrado para o Id: {TccId}", request.TccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado.", 404);
        }

        var pendingChange = await tccGateway.FindPendingAdvisorChange(request.TccId);
        if (pendingChange is not null)
        {
            logger.LogWarning("Já existe uma solicitação pendente para o TCC {TccId}", request.TccId);
            return ResultPattern<string>.FailureResult("Já existe uma solicitação de substituição ou afastamento de orientador em andamento para este TCC.", 409);
        }

        var currentAdvisorUt = tcc.UserTccs.FirstOrDefault(ut => 
            ut.Profile.Role == RoleType.ADVISOR.ToString() ||
            ut.Profile.Role == RoleType.COORDINATOR.ToString() ||
            ut.Profile.Role == RoleType.SUPERVISOR.ToString()
        );

        if (currentAdvisorUt is null)
        {
            logger.LogWarning("Orientador atual não encontrado no TCC {TccId}", request.TccId);
            return ResultPattern<string>.FailureResult("Orientador atual não encontrado no TCC.", 400);
        }

        var isStudent = tcc.UserTccs.Any(ut => ut.UserId == requesterUserId && ut.Profile.Role == RoleType.STUDENT.ToString());
        var isCurrentAdvisor = currentAdvisorUt.UserId == requesterUserId;

        var roleUpper = request.RequesterRole.Trim().ToUpper();

        if (roleUpper == "STUDENT")
        {
            if (!isStudent)
            {
                logger.LogWarning("Usuário {UserId} tentou solicitar substituição sem ser estudante do TCC {TccId}", requesterUserId, request.TccId);
                return ResultPattern<string>.FailureResult("Apenas estudantes vinculados a este TCC podem solicitar a substituição do orientador.", 403);
            }
        }
        else if (roleUpper == "ADVISOR")
        {
            if (!isCurrentAdvisor)
            {
                logger.LogWarning("Usuário {UserId} tentou solicitar afastamento sem ser o orientador do TCC {TccId}", requesterUserId, request.TccId);
                return ResultPattern<string>.FailureResult("Apenas o orientador atual do TCC pode solicitar o seu afastamento.", 403);
            }
        }
        else
        {
            return ResultPattern<string>.FailureResult("Papel de solicitante inválido. Deve ser STUDENT ou ADVISOR.", 400);
        }

        if (request.SuggestedAdvisorId.HasValue)
        {
            if (request.SuggestedAdvisorId.Value == currentAdvisorUt.UserId)
            {
                return ResultPattern<string>.FailureResult("O orientador substituto não pode ser o orientador atual.", 400);
            }

            var suggestedAdvisor = await userGateway.FindById(request.SuggestedAdvisorId.Value);
            if (suggestedAdvisor is null)
            {
                return ResultPattern<string>.FailureResult("Orientador substituto sugerido não encontrado.", 404);
            }
        }

        try
        {
            var advisorChange = new TccAdvisorChangeEntity
            {
                TccId = tcc.Id,
                RequesterUserId = requesterUserId,
                RequesterRole = roleUpper,
                CurrentAdvisorId = currentAdvisorUt.UserId,
                SuggestedAdvisorId = request.SuggestedAdvisorId,
                Reason = request.Reason.Trim(),
                Status = "PENDING_SIGNATURES",
                CreatedAt = DateTime.UtcNow
            };

            await tccGateway.SaveAdvisorChange(advisorChange);

            logger.LogInformation("Solicitação de alteração de orientador criada com sucesso para TCC {TccId}. Id da alteração: {ChangeId}", tcc.Id, advisorChange.Id);
            return ResultPattern<string>.SuccessResult("Solicitação enviada com sucesso.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao registrar solicitação de alteração de orientador para TccId {TccId}", request.TccId);
            return ResultPattern<string>.FailureResult("Erro interno ao registrar solicitação de alteração de orientador.", 500);
        }
    }
}
