namespace gestaotcc.Domain.Dtos.Tcc;

public record FindAllTccByFilterDTO(long TccId, List<string> StudanteNames, string? Title = null, string? Status = null);