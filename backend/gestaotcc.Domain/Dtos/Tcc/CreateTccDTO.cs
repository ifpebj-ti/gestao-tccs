namespace gestaotcc.Domain.Dtos.Tcc;

public record CreateTccDTO(List<string> StudentEmails, string? Title, string? Summary, long AdvisorId);