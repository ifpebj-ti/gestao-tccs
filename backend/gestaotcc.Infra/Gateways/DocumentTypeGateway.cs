using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Gateways;

public class DocumentTypeGateway(AppDbContext context) : IDocumentTypeGateway
{
    public async Task<List<DocumentTypeEntity>> FindAll()
    {
        return await context.DocumentTypes.ToListAsync();
    }
}