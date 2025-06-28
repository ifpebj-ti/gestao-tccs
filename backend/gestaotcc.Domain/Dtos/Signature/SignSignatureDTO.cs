namespace gestaotcc.Domain.Dtos.Signature;

public record SignSignatureDTO(long TccId, long DocumentId, long UserId, byte[] File, double FileSize, string FileContentType);