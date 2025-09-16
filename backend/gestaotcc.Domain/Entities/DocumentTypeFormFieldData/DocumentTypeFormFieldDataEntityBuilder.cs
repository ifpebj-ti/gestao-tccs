using gestaotcc.Domain.Entities.DocumentType;

namespace gestaotcc.Domain.Entities.DocumentTypeFormFieldData;

public class DocumentTypeFormFieldDataEntityBuilder
{
    private long _id;
    private string _fieldName = string.Empty;
    private ICollection<DocumentTypeEntity> _documentTypes = new List<DocumentTypeEntity>();

    public DocumentTypeFormFieldDataEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public DocumentTypeFormFieldDataEntityBuilder WithFieldName(string fieldName)
    {
        _fieldName = fieldName;
        return this;
    }

    public DocumentTypeFormFieldDataEntityBuilder WithDocumentTypes(ICollection<DocumentTypeEntity> documentTypes)
    {
        _documentTypes = documentTypes;
        return this;
    }

    public DocumentTypeFormFieldDataEntity Build()
    {
        return new DocumentTypeFormFieldDataEntity(_id, _fieldName, _documentTypes);
    }
}