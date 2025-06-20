namespace gestaotcc.Domain.Dtos.Signature;

public record FindAllPendingSignatureDTO(long TccId, List<string> StudentNames, List<FindAllPendingSignatureDetailsDTO> PendingDetails);