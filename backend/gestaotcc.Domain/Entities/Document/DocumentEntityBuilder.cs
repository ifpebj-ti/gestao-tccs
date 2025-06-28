using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Document;

public class DocumentEntityBuilder
{
    private long _id;
    private DocumentTypeEntity _documentType = null!;
    private TccEntity _tcc = null!;
    private UserEntity? _user = null!;
    private ICollection<SignatureEntity> _signatures = null!;
    private string _fileName = string.Empty;

    public DocumentEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public DocumentEntityBuilder WithDocumentType(DocumentTypeEntity documentType)
    {
        _documentType = documentType;
        return this;
    }

    public DocumentEntityBuilder WithTcc(TccEntity tcc)
    {
        _tcc = tcc;
        return this;
    }

    public DocumentEntityBuilder WithUser(UserEntity? user)
    {
        _user = user;
        return this;
    }

    public DocumentEntityBuilder WithSignature(ICollection<SignatureEntity> signatures)
    {
        _signatures = signatures;
        return this;
    }

    public DocumentEntityBuilder WithFileName(string fileName)
    {
        _fileName = fileName;
        return this;
    }

    public DocumentEntity Build()
    {
        return new DocumentEntity(_id, _documentType, _tcc, _user, _signatures, _fileName);
    }
}