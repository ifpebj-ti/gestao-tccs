namespace gestaotcc.Domain.Dtos.Tcc;
public record BankingMemberDto(string Name, string Email, string Role);
public record ScheduleTccDTO(DateOnly? ScheduleDate, TimeOnly? ScheduleTime, string? ScheduleLocation, long IdTcc, IEnumerable<BankingMemberDto>? BankingMembers = null);