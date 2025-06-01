using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Signature;

public class SignatureEntityBuilder
{
    private long _id;
    private DateTime _signatureDate;
    private DocumentEntity _document = null!;
    private UserEntity _user = null!;

    public SignatureEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public SignatureEntityBuilder WithSignatureDate(DateTime signatureDate)
    {
        _signatureDate = signatureDate;
        return this;
    }

    public SignatureEntityBuilder WithDocument(DocumentEntity document)
    {
        _document = document;
        return this;
    }

    public SignatureEntityBuilder WithUser(UserEntity user)
    {
        _user = user;
        return this;
    }

    public SignatureEntity Build()
    {
        return new SignatureEntity(_id, _signatureDate, _document, _user);
    }
}