using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindPendingAdvisorChangeUseCase(
    ITccGateway tccGateway,
    IAppLoggerGateway<FindPendingAdvisorChangeUseCase> logger
)
{
    public async Task<ResultPattern<FindTccAdvisorChangeDTO?>> Execute(long tccId)
    {
        logger.LogInformation("Buscando solicitação pendente de alteração de orientador para TccId: {TccId}", tccId);

        var change = await tccGateway.FindPendingAdvisorChange(tccId);
        if (change is null)
        {
            return ResultPattern<FindTccAdvisorChangeDTO?>.SuccessResult(null);
        }

        var dto = new FindTccAdvisorChangeDTO(
            change.Id,
            change.TccId,
            change.RequesterUserId,
            change.RequesterUser?.Name ?? string.Empty,
            change.RequesterRole,
            change.CurrentAdvisorId,
            change.CurrentAdvisor?.Name ?? string.Empty,
            change.SuggestedAdvisorId,
            change.SuggestedAdvisor?.Name,
            change.Reason,
            change.Status,
            change.DocumentId,
            change.CreatedAt
        );

        return ResultPattern<FindTccAdvisorChangeDTO?>.SuccessResult(dto);
    }
}
