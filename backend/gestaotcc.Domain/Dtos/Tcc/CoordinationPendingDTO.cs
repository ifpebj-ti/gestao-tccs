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
    List<CoordinationPendingTccItemDTO> AwaitingCancellation
);
