using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Document;

public class DocumentEntity
{
    public long Id { get; set; }
    public DocumentTypeEntity DocumentType { get; set; } = null!;
    public long DocumentTypeId { get; set; }
    public UserEntity? User { get; set; }
    public long? UserId { get; set; }
    public TccEntity Tcc { get; set; } = null!;
    public long TccId { get; set; }
    public ICollection<SignatureEntity> Signatures { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    
    public DocumentEntity() { }

    public DocumentEntity(long id, DocumentTypeEntity documentType, TccEntity tcc, UserEntity? user, ICollection<SignatureEntity> signatures, string fileName)
    {
        this.Id = id;
        this.DocumentType = documentType;
        this.Tcc = tcc;
        this.User = user;
        this.Signatures = signatures;
        this.FileName = fileName;
    }

}