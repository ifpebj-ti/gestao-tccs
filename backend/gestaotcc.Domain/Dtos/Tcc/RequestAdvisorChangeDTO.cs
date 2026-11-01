namespace gestaotcc.Domain.Dtos.Tcc;

/// <summary>
/// DTO para solicitação de substituição de orientador (pelo estudante - Anexo IX)
/// ou afastamento da orientação (pelo próprio orientador - Anexo V).
/// </summary>
public record RequestAdvisorChangeDTO(
    long TccId,
    string Reason,
    long? SuggestedAdvisorId,
    string RequesterRole // "STUDENT" ou "ADVISOR"
);
