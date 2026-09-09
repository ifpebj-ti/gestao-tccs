using System;
using System.Linq;
using System.Threading.Tasks;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class DownloadDocumentUseCase(
    ITccGateway tccGateway, 
    FindDocumentUseCase findDocumentUseCase,
    IITextGateway iTextGateway,
    IAppLoggerGateway<DownloadDocumentUseCase> logger)
{
    public async Task<ResultPattern<DownloadDocumentDTO>> Execute(long tccId, long documentId, long? studentId, long campiCourseId)
    {
        logger.LogInformation("Iniciando download de documento via FindDocumentUseCase. TccId: {TccId}, DocumentId: {DocumentId}", tccId, documentId);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("Falha no download: TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<DownloadDocumentDTO>.FailureResult("Erro ao realizar download do documento", 404);
        }
        
        var document = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId);
        if (document is null)
        {
            logger.LogWarning("Falha no download: Documento não encontrado para o DocumentId: {DocumentId}", documentId);
            return ResultPattern<DownloadDocumentDTO>.FailureResult("Documento não encontrado", 404);
        }

        var isSign = document.Signatures.Any();
        var fileName = isSign ? (document.FileName + ".pdf") : (document.DocumentType.Name + ".pdf");
        logger.LogInformation("Resolvido nome de arquivo para download: {FileName}", fileName);

        var findResult = await findDocumentUseCase.Execute(tccId, documentId, studentId, campiCourseId, returnSignedPdfIfAvailable: true);
        
        if (findResult.IsFailure)
        {
            return ResultPattern<DownloadDocumentDTO>.FailureResult(findResult.ErrorDetails!.Detail, findResult.ErrorDetails.Status ?? 500);
        }

        var documentBytes = Convert.FromBase64String(findResult.Data.Url);
        logger.LogInformation("Conversão do base64 gerado com sucesso. Retornando {BytesCount} bytes para download.", documentBytes.Length);

        return ResultPattern<DownloadDocumentDTO>.SuccessResult(new DownloadDocumentDTO(fileName, documentBytes));
    }

    public async Task<ResultPattern<DownloadDocumentDTO>> Execute(long tccId, long documentId, string htmlContent)
    {
        logger.LogInformation("Iniciando geração de PDF a partir do HTML fornecido. TccId: {TccId}, DocumentId: {DocumentId}", tccId, documentId);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("Falha no download: TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<DownloadDocumentDTO>.FailureResult("Erro ao realizar download do documento", 404);
        }
        
        var document = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId);
        if (document is null)
        {
            logger.LogWarning("Falha no download: Documento não encontrado para o DocumentId: {DocumentId}", documentId);
            return ResultPattern<DownloadDocumentDTO>.FailureResult("Documento não encontrado", 404);
        }

        var isSign = document.Signatures.Any();
        var fileName = isSign ? (document.FileName + ".pdf") : (document.DocumentType.Name + ".pdf");
        logger.LogInformation("Resolvido nome de arquivo para download via HTML: {FileName}", fileName);

        var documentBytes = await iTextGateway.ConvertHtmlToPdf(htmlContent);
        logger.LogInformation("Conversão de HTML para PDF gerada com sucesso. Retornando {BytesCount} bytes para download.", documentBytes.Length);

        return ResultPattern<DownloadDocumentDTO>.SuccessResult(new DownloadDocumentDTO(fileName, documentBytes));
    }
}