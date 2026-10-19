namespace gestaotcc.Domain.Dtos.Tcc;

public record CoordinationPendingTccItemDTO(
    long TccId,
    string Title,
    List<string> StudentNames,
    string? FinalSubmissionDeadline
);

public record CoordinationPendingDTO(
    int AwaitingHardCoverCount,
    int AwaitingLibraryForwardCount,
    int AwaitingCancellationCount,
    List<CoordinationPendingTccItemDTO> AwaitingHardCover,
    List<CoordinationPendingTccItemDTO> AwaitingLibraryForward,
    List<CoordinationPendingTccItemDTO> AwaitingCancellation,
    int AwaitingAdvisorChangesCount = 0,
    List<CoordinationPendingAdvisorChangeDTO>? AwaitingAdvisorChanges = null
);

public record CoordinationPendingAdvisorChangeDTO(
    long TccId, long AdvisorChangeId, string Title, List<string> StudentNames,
    string RequesterName, string RequesterRole, string CurrentAdvisorName,
    string? SuggestedAdvisorName, string Reason, string CreatedAt,
    long? DocumentId
);

public record RejectAdvisorChangeDTO(string ReviewNotes);
