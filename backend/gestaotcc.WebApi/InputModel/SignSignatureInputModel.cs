namespace gestaotcc.WebApi.InputModel;

public record SignSignatureInputModel(long TccId, long DocumentTypeId, long UserId, IFormFile File);