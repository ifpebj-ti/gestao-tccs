namespace gestaotcc.Domain.Dtos.Signature;

public record SendPendingSignatureDTO(string UserEmail, string UserName, List<SendPendingSignatureDetailsDTO> Details, string TccTitle);