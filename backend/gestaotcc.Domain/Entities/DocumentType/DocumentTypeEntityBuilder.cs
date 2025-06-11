namespace gestaotcc.Domain.Entities.DocumentType;

public class DocumentTypeEntityBuilder
{
    private long _id;
    private string _name = string.Empty;
    private long _signatureOrder;

    public DocumentTypeEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public DocumentTypeEntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public DocumentTypeEntityBuilder WithSignatureOrder(long signatureOrder)
    {
        _signatureOrder = signatureOrder;
        return this;
    }

    public DocumentTypeEntity Build()
    {
        return new DocumentTypeEntity(_id, _name, _signatureOrder);
    }
}