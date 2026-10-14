using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Tcc;

// Participation consent is separate from the existing registration invitations.
public class ProposalParticipantEntity
{
    public long Id { get; set; }
    public long TccId { get; set; }
    public TccEntity Tcc { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public UserEntity? User { get; set; }
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public DateTime? NotificationDismissedAt { get; set; }
}
