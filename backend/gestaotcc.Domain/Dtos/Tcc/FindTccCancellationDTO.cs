namespace gestaotcc.Domain.Dtos.Tcc;
public record FindTccCancellationDTO(string? TitleTCC, List<string>? StudentName, string? AdvisorName, string? ReasonCancellation);
