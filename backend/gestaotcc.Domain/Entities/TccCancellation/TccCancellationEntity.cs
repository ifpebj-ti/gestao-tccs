using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.TccCancellation;
public class TccCancellationEntity
{
    public long Id { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING"; // PENDING, APPROVED
    public TccEntity Tcc { get; set; } = null!;
    public long TccId { get; set; }

    public TccCancellationEntity() { }
    public TccCancellationEntity(long id, string reason, string status, long tccId)
    {
        Id = id;
        Reason = reason;
        Status = status;
        TccId = tccId;
    }
}
