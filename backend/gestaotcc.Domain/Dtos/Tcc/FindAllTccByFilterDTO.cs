namespace gestaotcc.Domain.Dtos.Tcc;

public record FindAllTccByFilterDTO(long TccId, List<string> StudanteNames);