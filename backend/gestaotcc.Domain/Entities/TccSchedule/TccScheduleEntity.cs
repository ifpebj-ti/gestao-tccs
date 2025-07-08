using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Domain.Entities.TccSchedule;
public class TccScheduleEntity
{
    public long Id { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public TccEntity Tcc { get; set; } = null!;
    public long TccId { get; set; }
    public TccScheduleEntity() { }
    public TccScheduleEntity(long id, DateTime scheduledDate, string location, long tccId)
    {
        Id = id;
        ScheduledDate = scheduledDate;
        Location = location;
        TccId = tccId;
    }
}
