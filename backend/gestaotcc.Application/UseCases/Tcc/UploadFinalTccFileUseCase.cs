using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class UploadFinalTccFileUseCase
{
    private readonly ITccGateway _tccGateway;
    private readonly IStorageGateway _storageGateway;
    private readonly IUserGateway _userGateway;

    public UploadFinalTccFileUseCase(ITccGateway tccGateway, IStorageGateway storageGateway, IUserGateway userGateway)
    {
        _tccGateway = tccGateway;
        _storageGateway = storageGateway;
        _userGateway = userGateway;
    }

    public async Task<ResultPattern<string>> Execute(long tccId, long userId, byte[] fileBytes, string fileName, string contentType)
    {
        if (tccId <= 0)
            return ResultPattern<string>.FailureResult("TCC inválido.", 400);

        if (fileBytes == null || fileBytes.Length == 0)
            return ResultPattern<string>.FailureResult("Nenhum arquivo enviado.", 400);

        var user = await _userGateway.FindById(userId);
        if (user == null || user.Status != "ACTIVE")
            return ResultPattern<string>.FailureResult("Usuário não autorizado ou inativo.", 401);

        var tcc = await _tccGateway.FindTccById(tccId);
        if (tcc == null)
            return ResultPattern<string>.FailureResult("TCC não encontrado.", 404);

        var roles = user.Profile.Select(p => p.Role).ToArray();
        var isLinked = tcc.UserTccs.Any(link => link.UserId == userId && roles.Contains(link.Profile.Role));
        var isManagement = roles.Any(r => r is "ADMIN" or "COORDINATOR" or "SUPERVISOR");

        if (!isLinked && !isManagement)
            return ResultPattern<string>.FailureResult("Você não tem permissão para enviar a versão final deste TCC.", 403);

        var isFinalStage = tcc.Step == StepTccType.FINALIZATION_AND_PUBLICATION.ToString() ||
                           tcc.Step == "6" ||
                           tcc.FinalSubmissionDeadline.HasValue;

        if (!isFinalStage)
            return ResultPattern<string>.FailureResult("O TCC não está na etapa final para envio da versão pós-banca.", 400);

        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            return ResultPattern<string>.FailureResult("Apenas arquivos no formato PDF são permitidos.", 400);

        var finalFileName = $"final/tcc_{tccId}_{Guid.NewGuid()}{extension}";

        await _storageGateway.Send(finalFileName, fileBytes, contentType, isFilledPdfProcess: true);

        tcc.FinalTccFile = finalFileName;
        await _tccGateway.Update(tcc);

        return ResultPattern<string>.SuccessResult(finalFileName);
    }
}
