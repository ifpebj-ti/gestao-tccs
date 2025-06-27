using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class FindDocumentUseCase(ITccGateway tccGateway, IMinioGateway minioGateway)
{
    public async Task<ResultPattern<FindDocumentDTO>> Execute(long tccId, long documentId)
    {
        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
            return ResultPattern<FindDocumentDTO>.FailureResult("Erro ao realizar download do documento", 404);
        
        var isSign = tcc.Documents.Any(doc => doc.Signatures.Any(sig => sig.DocumentId == documentId));
        var templateDocument = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.DocumentType.Name+".pdf";
        var document = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.FileName+".pdf";
        
        var documentUrl = await minioGateway.GetPresignedUrl(isSign ? document : templateDocument, isSign);

        return ResultPattern<FindDocumentDTO>.SuccessResult(new FindDocumentDTO(documentUrl));
    }
}