using gestaotcc.Domain.Entities.DocumentType;

namespace gestaotcc.Application.Gateways;

public interface IDocumentTypeGateway
{
    Task<List<DocumentTypeEntity>> FindAll();
}