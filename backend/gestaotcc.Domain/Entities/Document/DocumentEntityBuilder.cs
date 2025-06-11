using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.Document;

public class DocumentEntityBuilder
{
    private long _id;
    private DocumentTypeEntity _documentType = null!;
    private TccEntity _tcc = null!;
    private ICollection<SignatureEntity> _signatures = null!;
    private bool _isSigned = false;
    private byte[] _file = Array.Empty<byte>();

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

    public DocumentEntityBuilder WithSignature(ICollection<SignatureEntity> signatures)
    {
        _signatures = signatures;
        return this;
    }

    public DocumentEntityBuilder WithIsSigned(bool isSigned)
    {
        _isSigned = isSigned;
        return this;
    }

    public DocumentEntityBuilder WithFile(byte[] file)
    {
        _file = file;
        return this;
    }

    public DocumentEntity Build()
    {
        return new DocumentEntity(_id, _documentType, _tcc, _signatures, _isSigned, _file);
    }
}