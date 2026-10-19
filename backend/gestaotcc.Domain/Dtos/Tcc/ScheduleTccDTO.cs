namespace gestaotcc.Domain.Dtos.Tcc;
public record BankingMemberDto(string Name, string Email, string Role, long? UserId = null);
public record ScheduleTccDTO(DateOnly? ScheduleDate, TimeOnly? ScheduleTime, string? ScheduleLocation, long IdTcc, IEnumerable<BankingMemberDto>? BankingMembers = null);
