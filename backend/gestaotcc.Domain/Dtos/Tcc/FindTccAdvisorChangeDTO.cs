namespace gestaotcc.Domain.Dtos.Tcc;

public record FindTccAdvisorChangeDTO(
    long Id,
    long TccId,
    long RequesterUserId,
    string RequesterName,
    string RequesterRole,
    long CurrentAdvisorId,
    string CurrentAdvisorName,
    long? SuggestedAdvisorId,
    string? SuggestedAdvisorName,
    string Reason,
    string Status,
    long? DocumentId,
    DateTime CreatedAt
);
