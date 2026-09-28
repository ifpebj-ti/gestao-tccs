
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class UploadTccFileUseCase
{
    private readonly ITccGateway _tccGateway;
    private readonly IMinioGateway _minioGateway;

    public UploadTccFileUseCase(ITccGateway tccGateway, IMinioGateway minioGateway)
    {
        _tccGateway = tccGateway;
        _minioGateway = minioGateway;
    }

    public async Task<ResultPattern<string>> Execute(long tccId, byte[] fileBytes, string fileName, string contentType)
    {
        var tcc = await _tccGateway.FindTccById(tccId);
        if (tcc == null)
            return ResultPattern<string>.FailureResult("Tcc não encontrado", 404);

        var extension = Path.GetExtension(fileName);
        var finalFileName = $"filled/tcc_{tccId}_{Guid.NewGuid()}{extension}";

        await _minioGateway.Send(finalFileName, fileBytes, contentType, isFilledPdfProcess: true);

        tcc.TccFile = finalFileName;
        await _tccGateway.Update(tcc);

        return ResultPattern<string>.SuccessResult(finalFileName);
    }
}
