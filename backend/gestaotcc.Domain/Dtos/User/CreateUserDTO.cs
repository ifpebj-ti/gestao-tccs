
using gestaotcc.Domain.Enums;

namespace gestaotcc.Domain.Dtos.User;
public record CreateUserDTO(
    string Name, 
    string Email, 
    string? Registration, 
    string CPF, 
    string? SIAPE, 
    List<string> Profile, 
    string Phone,
    string? UserClass,
    ShiftType? Shift,
    string? Titration,
    long CourseId,
    long CampusId);
