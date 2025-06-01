namespace gestaotcc.Domain.Dtos.Tcc;

public record FindAllTccByStatusOrUserIdDTO(long TccId, List<string> StudanteNames);