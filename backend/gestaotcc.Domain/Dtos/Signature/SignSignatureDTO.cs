namespace gestaotcc.Domain.Dtos.Signature;

public record SignSignatureDTO(long TccId, long DocumentTypeId, long UserId, string FilePath, double FileSize, string FileContentType);