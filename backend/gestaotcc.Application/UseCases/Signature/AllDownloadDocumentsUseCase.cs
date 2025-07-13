using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class AllDownloadDocumentsUseCase(ITccGateway tccGateway, IMinioGateway minioGateway)
{
    public async Task<ResultPattern<AllDownloadDocumentsDTO>> Execute(long tccId)
    {
        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
            return ResultPattern<AllDownloadDocumentsDTO>.FailureResult("Erro ao carregar arquivos", 404);
        
        var currentYear = DateTime.Now.Year;
        var folder = $"{currentYear}/{tcc.Title}";
        
        var zipBytes = await minioGateway.DownloadFolderAsZip(folder);

        if (zipBytes.Length == 0)
            return ResultPattern<AllDownloadDocumentsDTO>.FailureResult("Erro ao carregar arquivos", 404);
        
        return ResultPattern<AllDownloadDocumentsDTO>.SuccessResult(new AllDownloadDocumentsDTO(tcc.Title, zipBytes));
    }
}