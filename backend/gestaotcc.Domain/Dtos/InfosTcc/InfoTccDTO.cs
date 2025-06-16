namespace gestaotcc.Domain.Dtos.InfosTcc;
public record InfoTccDTO(string Title, string Summary, DateOnly? PresentationDate, TimeOnly? PresentationTime, string? PresentationLocation);
