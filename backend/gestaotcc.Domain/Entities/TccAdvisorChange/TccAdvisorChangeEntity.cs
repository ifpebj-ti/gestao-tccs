using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.TccAdvisorChange;

public class TccAdvisorChangeEntity
{
    public long Id { get; set; }
    public long TccId { get; set; }
    public TccEntity Tcc { get; set; } = null!;

    public long RequesterUserId { get; set; }
    public UserEntity? RequesterUser { get; set; }
    public string RequesterRole { get; set; } = string.Empty; // "STUDENT" ou "ADVISOR"

    public long CurrentAdvisorId { get; set; }
    public UserEntity? CurrentAdvisor { get; set; }

    public long? SuggestedAdvisorId { get; set; }
    public UserEntity? SuggestedAdvisor { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING_SIGNATURES"; // PENDING_SIGNATURES, PENDING_COORDINATION, APPROVED, REJECTED

    public long? DocumentId { get; set; }
    public DocumentEntity? Document { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public long? ReviewedByUserId { get; set; }
    public UserEntity? ReviewedByUser { get; set; }
    public string? ReviewNotes { get; set; }

    public TccAdvisorChangeEntity() { }
}
