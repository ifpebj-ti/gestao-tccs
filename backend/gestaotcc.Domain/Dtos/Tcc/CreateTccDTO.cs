namespace gestaotcc.Domain.Dtos.Tcc;

public record CreateTccDTO(List<StudentsToCreateTccDTO> Students, string? Title, string? Summary, long AdvisorId);