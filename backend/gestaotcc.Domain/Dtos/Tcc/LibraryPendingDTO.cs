namespace gestaotcc.Domain.Dtos.Tcc;

public record LibraryPendingTccItemDTO(
    long TccId,
    string Title,
    List<string> StudentNames,
    string AdvisorName,
    string CourseName,
    string? ForwardedAt,
    string? FinalTccFile,
    string? CatalogCode = null,
    string? CatalogedAt = null
);

public record LibraryPendingDTO(
    int PendingCount,
    int CatalogedCount,
    List<LibraryPendingTccItemDTO> PendingCataloging,
    List<LibraryPendingTccItemDTO> RecentlyCataloged
);
