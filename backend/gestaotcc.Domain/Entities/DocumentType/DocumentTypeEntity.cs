using gestaotcc.Domain.Entities.Document;

namespace gestaotcc.Domain.Entities.DocumentType;

public class DocumentTypeEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long SignatureOrder { get; set; }
    public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
    public DocumentTypeEntity() {}

    public DocumentTypeEntity(long id, string name, long signatureOrder)
    {
        this.Id = id;
        this.Name = name;
        this.SignatureOrder = signatureOrder;
    }
}