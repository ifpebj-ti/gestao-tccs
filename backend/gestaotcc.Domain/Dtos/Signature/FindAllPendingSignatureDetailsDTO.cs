namespace gestaotcc.Domain.Dtos.Signature;

public record FindAllPendingSignatureDetailsDTO(long DocumentId, string DocumentName, List<FindAllPendignSignatureUserDetailsDTO> UserDetails);