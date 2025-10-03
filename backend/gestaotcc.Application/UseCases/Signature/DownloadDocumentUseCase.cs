using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class DownloadDocumentUseCase(ITccGateway tccGateway, IMinioGateway minioGateway, IAppLoggerGateway<DownloadDocumentUseCase> logger)
{
    public async Task<ResultPattern<DownloadDocumentDTO>> Execute(long tccId, long documentId)
    {
        logger.LogInformation("Iniciando download de documento. TccId: {TccId}, DocumentId: {DocumentId}", tccId, documentId);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("Falha no download: TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<DownloadDocumentDTO>.FailureResult("Erro ao realizar download do documento", 404);
        }
        
        logger.LogInformation("TCC encontrado para download. TccId: {TccId}", tccId);

        var isSign = tcc.Documents.Any(doc => doc.Signatures.Any(sig => sig.DocumentId == documentId));
        logger.LogInformation("Verificação de assinatura para DocumentId {DocumentId}: IsSigned = {IsSigned}", documentId, isSign);

        var templateDocument = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.DocumentType.Name+".pdf";
        var document = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.FileName+".pdf";
        logger.LogInformation("Nomes de arquivos resolvidos. Template: {TemplateFileName}, Assinado: {SignedFileName}", templateDocument, document);

        var fileToDownload = isSign ? document : templateDocument;
        logger.LogInformation("Realizando download do arquivo: {FileName} do Minio.", fileToDownload);

        var documentBytes = await minioGateway.Download(fileToDownload, isSign);
        logger.LogInformation("Download do Minio concluído. Tamanho do arquivo: {FileSizeInBytes} bytes.", documentBytes.Length);

        return ResultPattern<DownloadDocumentDTO>.SuccessResult(new DownloadDocumentDTO(templateDocument, documentBytes));
    }
}