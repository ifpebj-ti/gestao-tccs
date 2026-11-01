using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindCoordinationPendingUseCase
{
    private readonly ITccGateway _tccGateway;
    private readonly IUserGateway _userGateway;

    public FindCoordinationPendingUseCase(ITccGateway tccGateway, IUserGateway userGateway)
    {
        _tccGateway = tccGateway;
        _userGateway = userGateway;
    }

    public async Task<ResultPattern<CoordinationPendingDTO>> Execute(long coordinatorUserId)
    {
        if (coordinatorUserId <= 0)
            return ResultPattern<CoordinationPendingDTO>.FailureResult("Parâmetros inválidos.", 400);

        var user = await _userGateway.FindById(coordinatorUserId);
        if (user == null || user.Status != "ACTIVE")
            return ResultPattern<CoordinationPendingDTO>.FailureResult("Usuário não autorizado ou inativo.", 401);

        var roles = user.Profile.Select(p => p.Role).ToArray();
        var canView = roles.Any(r => r is "COORDINATOR" or "SUPERVISOR");
        if (!canView)
            return ResultPattern<CoordinationPendingDTO>.FailureResult("Acesso restrito à Coordenação e Supervisão.", 403);

        // Busca todos os TCCs em andamento do curso do coordenador
        var filter = new TccFilterDTO(null, "IN_PROGRESS");
        var allInProgress = await _tccGateway.FindAllTccByFilter(filter, user.CampiCourseId ?? 0);

        // 1) TCCs na etapa final aguardando capa dura (HardCoverReceivedAt == null)
        var awaitingHardCover = allInProgress
            .Where(t =>
                (t.Step == StepTccType.FINALIZATION_AND_PUBLICATION.ToString() ||
                 t.Step == "6" ||
                 t.FinalSubmissionDeadline.HasValue) &&
                !t.HardCoverReceivedAt.HasValue)
            .Select(t => new CoordinationPendingTccItemDTO(
                t.Id,
                t.Title ?? "TCC sem título",
                t.UserTccs
                    .Where(u => u.Profile.Role == "STUDENT")
                    .Select(u => u.User.Name ?? u.User.Email ?? "Aluno")
                    .ToList(),
                t.FinalSubmissionDeadline.HasValue
                    ? t.FinalSubmissionDeadline.Value.ToString("yyyy-MM-ddTHH:mm:ssZ")
                    : null
            ))
            .ToList();

        // 2) TCCs com capa dura confirmada aguardando encaminhamento à biblioteca (ForwardedToLibraryAt == null)
        var awaitingLibraryForward = allInProgress
            .Where(t =>
                t.HardCoverReceivedAt.HasValue &&
                !t.ForwardedToLibraryAt.HasValue)
            .Select(t => new CoordinationPendingTccItemDTO(
                t.Id,
                t.Title ?? "TCC sem título",
                t.UserTccs
                    .Where(u => u.Profile.Role == "STUDENT")
                    .Select(u => u.User.Name ?? u.User.Email ?? "Aluno")
                    .ToList(),
                null
            ))
            .ToList();

        // 3) TCCs com solicitação de cancelamento pendente
        var awaitingCancellation = allInProgress
            .Where(t => t.TccCancellation != null)
            .Select(t => new CoordinationPendingTccItemDTO(
                t.Id,
                t.Title ?? "TCC sem título",
                t.UserTccs
                    .Where(u => u.Profile.Role == "STUDENT")
                    .Select(u => u.User.Name ?? u.User.Email ?? "Aluno")
                    .ToList(),
                null
            ))
            .ToList();

        var awaitingAdvisorChanges = allInProgress
            .SelectMany(t => t.AdvisorChanges
                .Where(change => change.Status == "PENDING_COORDINATION")
                .Select(change => new CoordinationPendingAdvisorChangeDTO(
                    t.Id, change.Id, t.Title ?? "TCC sem título",
                    t.UserTccs.Where(u => u.Profile.Role == "STUDENT").Select(u => u.User.Name ?? u.User.Email ?? "Aluno").ToList(),
                    change.RequesterUser?.Name ?? "Solicitante", change.RequesterRole,
                    change.CurrentAdvisor?.Name ?? "Orientador atual", change.SuggestedAdvisor?.Name,
                    change.Reason, change.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"), change.DocumentId)))
            .ToList();

        var result = new CoordinationPendingDTO(
            awaitingHardCover.Count,
            awaitingLibraryForward.Count,
            awaitingCancellation.Count,
            awaitingHardCover,
            awaitingLibraryForward,
            awaitingCancellation,
            awaitingAdvisorChanges.Count,
            awaitingAdvisorChanges
        );

        return ResultPattern<CoordinationPendingDTO>.SuccessResult(result);
    }
}
