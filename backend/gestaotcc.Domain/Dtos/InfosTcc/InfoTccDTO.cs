namespace gestaotcc.Domain.Dtos.InfosTcc;
public record InfoTccDTO(string Title, string Summary, string Status, DateOnly? PresentationDate, TimeOnly? PresentationTime, string? PresentationLocation);
