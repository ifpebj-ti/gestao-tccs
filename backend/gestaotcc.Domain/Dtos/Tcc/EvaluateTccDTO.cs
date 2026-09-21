namespace gestaotcc.Domain.Dtos.Tcc;

public record EvaluateTccDTO(
    string Token,
    decimal Grade,
    string? EvaluationComments,
    string? EvaluationDetails);
