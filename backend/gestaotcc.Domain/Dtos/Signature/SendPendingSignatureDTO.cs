namespace gestaotcc.Domain.Dtos.Signature;

public record SendPendingSignatureDTO(string UserEmail, string UserName, List<string> DocumentNames, string TccTitle);