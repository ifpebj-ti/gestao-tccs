using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class SignSignatureUseCase(IDocumentTypeGateway documentTypeGateway, ITccGateway tccGateway, IMinioGateway minioGateway)
{
    public async Task<ResultPattern<string>> Execute(SignSignatureDTO data)
    {
        if(data.FileSize > 5 || data.FileContentType != "application/pdf")
            return ResultPattern<string>.FailureResult(
                "Erro ao realizar upload. Por favor verifique o tamanho, o tipo e se já enviou arquivo enviado e tente novamente.",
                409
                );
        var tcc = await tccGateway.FindTccById(data.TccId);
        
        var user = tcc!.UserTccs.FirstOrDefault(u => u.UserId == data.UserId)!.User;
        var alreadySign = tcc.Documents.Any(doc =>
            doc.Signatures.All(sign => sign.UserId == user.Id && sign.DocumentId == data.DocumentId));
        
        if(alreadySign)
            return ResultPattern<string>.FailureResult(
                "Erro ao realizar upload. Por favor verifique o tamanho, o tipo e se já enviou arquivo enviado e tente novamente.",
                409
            );
        var newSignature = SignatureFactory.CreateSignature(user);
        var documentInTcc = tcc.Documents.FirstOrDefault(doc => doc.Id == data.DocumentId);
        documentInTcc!.Signatures.Add(newSignature);
    
        await tccGateway.Update(tcc);

        await minioGateway.Send(documentInTcc.FileName, data.File, data.FileContentType);
    
        return ResultPattern<string>.SuccessResult();
    }
}