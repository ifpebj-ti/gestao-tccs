using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.Document;

public class DocumentEntity
{
    public long Id { get; set; }
    public DocumentTypeEntity DocumentType { get; set; } = null!;
    public long DocumentTypeId { get; set; }
    public TccEntity Tcc { get; set; } = null!;
    public long TccId { get; set; }
    public ICollection<SignatureEntity> Signatures { get; set; } = null!;
    public bool IsSigned { get; set; } = false;
    public byte[] File { get; set; } = null!;
    public DocumentEntity() { }

    public DocumentEntity(long id, DocumentTypeEntity documentType, TccEntity tcc, ICollection<SignatureEntity> signatures, bool isSigned, byte[] file)
    {
        this.Id = id;
        this.DocumentType = documentType;
        this.Tcc = tcc;
        this.Signatures = signatures;
        this.IsSigned = isSigned;
        this.File = file;
    }

}