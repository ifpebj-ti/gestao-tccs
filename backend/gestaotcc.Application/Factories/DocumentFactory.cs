using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Factories;

public class DocumentFactory
{
    public static DocumentEntity CreateDocument(DocumentTypeEntity documentType, string tccTitle, UserEntity? user)
    {
        var currentYear = DateTime.Now.Year;
        var hash = Guid.NewGuid();
        var fileName = $"{currentYear}/{tccTitle}/Documento_Compartilhado/{hash}_{documentType.Name}";
        if(user is not null)
            fileName = $"{currentYear}/{tccTitle}/{user.Name}/{hash}_{documentType.Name}";
        
        return new DocumentEntityBuilder()
            .WithDocumentType(documentType)
            .WithFileName(fileName)
            .WithUser(user)
            .Build();
    }
}