namespace gestaotcc.Domain.Entities.TccSchedule;
public class TccScheduleEntityBuilder
{
    private long _id;
    private DateTime _scheduledDate = DateTime.Now;
    private string _location = string.Empty;
    private long _tccId;
    public TccScheduleEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }
    public TccScheduleEntityBuilder WithScheduledDate(DateTime scheduledDate)
    {
        _scheduledDate = scheduledDate;
        return this;
    }
    public TccScheduleEntityBuilder WithLocation(string location)
    {
        _location = location;
        return this;
    }
    public TccScheduleEntityBuilder WithTccId(long tccId)
    {
        _tccId = tccId;
        return this;
    }
    public TccScheduleEntity Build()
    {
        return new TccScheduleEntity(_id, _scheduledDate, _location, _tccId);
    }
}
