using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.TccSchedule;

namespace gestaotcc.Application.Factories;
public class TccScheduleFactory
{
    public static TccScheduleEntity CreateTccSchedule(ScheduleTccDTO data)
    {
        DateTime scheduleDate = DateTime.SpecifyKind(data.ScheduleDate!.Value.ToDateTime(data.ScheduleTime!.Value), DateTimeKind.Utc);
        return new TccScheduleEntityBuilder()
            .WithTccId(data.IdTcc)
            .WithScheduledDate(scheduleDate)
            .WithLocation(data.ScheduleLocation!)
            .Build();
    }
}
