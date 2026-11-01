using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class DownloadFinalTccFileUseCase
{
    private readonly TccFilesUseCase _tccFilesUseCase;
    private readonly IStorageGateway _storageGateway;

    public DownloadFinalTccFileUseCase(TccFilesUseCase tccFilesUseCase, IStorageGateway storageGateway)
    {
        _tccFilesUseCase = tccFilesUseCase;
        _storageGateway = storageGateway;
    }

    public async Task<ResultPattern<TccFileDownload>> Execute(long tccId, long userId)
    {
        var tcc = await _tccFilesUseCase.FindAccessible(tccId, userId);
        if (tcc == null)
            return ResultPattern<TccFileDownload>.FailureResult("TCC não encontrado ou acesso indisponível.", 404);

        if (string.IsNullOrWhiteSpace(tcc.FinalTccFile))
            return ResultPattern<TccFileDownload>.FailureResult("A versão final do TCC ainda não foi enviada.", 404);

        try
        {
            var fileBytes = await _storageGateway.Download(tcc.FinalTccFile, signedDocument: false);
            if (fileBytes == null || fileBytes.Length == 0)
                return ResultPattern<TccFileDownload>.FailureResult("Arquivo da versão final não encontrado no armazenamento.", 404);

            return ResultPattern<TccFileDownload>.SuccessResult(new(fileBytes, $"TCC_{tccId}_Versao_Final.pdf"));
        }
        catch
        {
            return ResultPattern<TccFileDownload>.FailureResult("Não foi possível acessar o arquivo no armazenamento.", 502);
        }
    }
}
