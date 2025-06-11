using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Signature;

public class SignatureEntity
{
    public long Id { get; set; }
    public DateTime SignatureDate { get; set; }
    public DocumentEntity Document { get; set; } = null!;
    public long DocumentId { get; set; }
    public UserEntity User { get; set; } = null!;
    public long UserId { get; set; }
    public SignatureEntity() { }

    public SignatureEntity(long id, DateTime signatureDate, DocumentEntity document, UserEntity user)
    {
        this.Id = id;
        this.SignatureDate = signatureDate;
        this.Document = document;
        this.User = user;
    }
}