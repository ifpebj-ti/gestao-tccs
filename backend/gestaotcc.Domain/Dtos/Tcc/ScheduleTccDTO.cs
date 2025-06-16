namespace gestaotcc.Domain.Dtos.Tcc;
public record ScheduleTccDTO(DateOnly? ScheduleDate, TimeOnly? ScheduleTime, string? ScheduleLocation, long IdTcc);