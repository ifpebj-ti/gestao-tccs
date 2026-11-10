
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;
using gestaotcc.Application.Helpers;

namespace gestaotcc.Application.UseCases.Tcc;

public class UploadTccFileUseCase
{
    private readonly ITccGateway _tccGateway;
    private readonly IStorageGateway _storageGateway;
    private readonly IUserGateway _userGateway;

    public UploadTccFileUseCase(ITccGateway tccGateway, IStorageGateway storageGateway, IUserGateway userGateway)
    {
        _tccGateway = tccGateway;
        _storageGateway = storageGateway;
        _userGateway = userGateway;
    }

    public async Task<ResultPattern<string>> Execute(long tccId, long userId, byte[] fileBytes, string fileName, string contentType)
    {
        var user = await _userGateway.FindById(userId);
        if (user is null || user.Status != "ACTIVE")
            return ResultPattern<string>.FailureResult("Usuário não autorizado ou inativo.", 401);
        var tcc = await _tccGateway.FindTccById(tccId);
        if (tcc == null)
            return ResultPattern<string>.FailureResult("Tcc não encontrado", 404);

        if (!TccUploadAuthorization.Allows(user, tcc))
            return ResultPattern<string>.FailureResult("Você não tem permissão para alterar este TCC.", 403);
        if (!PdfUploadPolicy.IsValid(fileBytes, fileName, contentType))
            return ResultPattern<string>.FailureResult("Envie um PDF válido de até 20 MB.", 400);

        var extension = Path.GetExtension(fileName);
        var finalFileName = $"filled/tcc_{tccId}_{Guid.NewGuid()}{extension}";

        await _storageGateway.Send(finalFileName, fileBytes, "application/pdf", isFilledPdfProcess: true);

        tcc.TccFile = finalFileName;
        await _tccGateway.Update(tcc);

        return ResultPattern<string>.SuccessResult(finalFileName);
    }
}
