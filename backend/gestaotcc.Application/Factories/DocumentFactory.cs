using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Factories;

public class DocumentFactory
{
    public static DocumentEntity CreateDocument(DocumentTypeEntity documentType, UserEntity? user)
    {
        var hash = Guid.NewGuid();
        var fileName = $"{hash}_{documentType.Name}";
        
        return new DocumentEntityBuilder()
            .WithDocumentType(documentType)
            .WithFileName(fileName)
            .WithUser(user)
            .Build();
    }
}