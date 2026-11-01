namespace gestaotcc.Domain.Dtos.Tcc;

/// <summary>
/// DTO para solicitação de afastamento da orientação pelo próprio docente orientador (Anexo V).
/// </summary>
public record RequestAdvisorDepartureDTO(
    long TccId,
    string Reason,
    long SuggestedAdvisorId // obrigatório para afastamento pelo orientador
);
