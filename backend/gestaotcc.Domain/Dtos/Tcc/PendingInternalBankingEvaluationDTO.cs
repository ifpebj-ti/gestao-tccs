namespace gestaotcc.Domain.Dtos.Tcc;

public record PendingInternalBankingEvaluationDTO(
    long TccId,
    string? TccTitle,
    DateTime ScheduledDate,
    string Location,
    List<string> StudentNames);
