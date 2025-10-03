using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class AllDownloadDocumentsUseCase(ITccGateway tccGateway, IMinioGateway minioGateway, IAppLoggerGateway<AllDownloadDocumentsUseCase> logger)
{
    public async Task<ResultPattern<AllDownloadDocumentsDTO>> Execute(long tccId)
    {
        logger.LogInformation("Iniciando o download de todos os documentos para o TccId: {TccId}", tccId);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("Falha no download: TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<AllDownloadDocumentsDTO>.FailureResult("Erro ao carregar arquivos", 404);
        }
        
        logger.LogInformation("TCC encontrado: {TccTitle}. TccId: {TccId}", tcc.Title, tccId);
        
        var currentYear = DateTime.Now.Year;
        var folder = $"{currentYear}/{tcc.Title}";
        logger.LogInformation("Caminho da pasta no Minio a ser baixada: {MinioFolderPath}", folder);
        
        var zipBytes = await minioGateway.DownloadFolderAsZip(folder);
        logger.LogInformation("Gateway Minio retornou um arquivo zip com {ZipSizeInBytes} bytes.", zipBytes.Length);

        if (zipBytes.Length == 0)
        {
            logger.LogWarning("Download falhou ou a pasta {MinioFolderPath} está vazia. O arquivo zip retornado tem 0 bytes.", folder);
            return ResultPattern<AllDownloadDocumentsDTO>.FailureResult("Erro ao carregar arquivos", 404);
        }
        
        logger.LogInformation("Download do zip concluído com sucesso para o TccId: {TccId}", tccId);
        return ResultPattern<AllDownloadDocumentsDTO>.SuccessResult(new AllDownloadDocumentsDTO(tcc.Title, zipBytes));
    }
}