namespace gestaotcc.WebApi.InputModel;

public record SignSignatureInputModel(long TccId, long DocumentId, long UserId, IFormFile File);