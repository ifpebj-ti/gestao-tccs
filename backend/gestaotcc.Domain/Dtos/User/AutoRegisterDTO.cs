using gestaotcc.Domain.Enums;

namespace gestaotcc.Domain.Dtos.User;

public record AutoRegisterDTO(
    string Name, 
    string Email, 
    string? Registration, 
    string CPF,
    string Phone,
    string? UserClass,
    ShiftType? Shift,
    long? CourseId = null,
    long? CampiId = null,
    string? Password = null);