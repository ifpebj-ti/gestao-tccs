using gestaotcc.Domain.Entities.DocumentType;

namespace gestaotcc.Domain.Entities.DocumentTypeFormFieldData;

public class DocumentTypeFormFieldDataEntity
{
    public long Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public ICollection<DocumentTypeEntity> DocumentTypes { get; set; } = null!;
    public DocumentTypeFormFieldDataEntity() { }

    public DocumentTypeFormFieldDataEntity(long id, string fieldName, ICollection<DocumentTypeEntity> documentTypes)
    {
        Id = id;
        FieldName = fieldName;
        DocumentTypes = documentTypes;
    }
}