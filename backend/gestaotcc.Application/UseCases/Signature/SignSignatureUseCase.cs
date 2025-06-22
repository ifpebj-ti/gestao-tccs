using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class SignSignatureUseCase(IDocumentTypeGateway documentTypeGateway, ITccGateway tccGateway)
{
    // public async Task<ResultPattern<string>> Execute(SignSignatureDTO data)
    // {
    //     if(data.FileSize > 5 || data.FileContentType != "application/pdf")
    //         return ResultPattern<string>.FailureResult(
    //             "Erro ao realizar upload. Por favor verifique o tamanho e arquivo enviado e tente novamente.",
    //             409
    //             );
    //     var tcc = await tccGateway.FindTccById(data.TccId);
    //     var allDocumentTypes = await documentTypeGateway.FindAll();
    //     
    //     var user = tcc.UserTccs.FirstOrDefault(u => u.UserId == data.UserId).User;
    //     var document = tcc.Documents.FirstOrDefault(document => document.DocumentTypeId == data.DocumentTypeId);
    //     var documentType = allDocumentTypes.FirstOrDefault(documentType => documentType.Id == data.DocumentTypeId);
    //     
    //     if (document is null)
    //     {
    //         var newDocument = DocumentFactory.CreateDocument(documentType!, user);
    //         tcc.Documents.Add(newDocument);
    //         await minioGateway.Send(newDocument.FileName, data.FilePath);
    //     }
    //     else
    //     {
    //         var newSignature = SignatureFactory.CreateSignature(user);
    //         document.Signatures.Add(newSignature);
    //         await minioGateway.Send(document.FileName, data.FilePath);
    //     }
    //
    //     await tccGateway.Update(tcc);
    //
    //     return ResultPattern<string>.SuccessResult();
    // }
}