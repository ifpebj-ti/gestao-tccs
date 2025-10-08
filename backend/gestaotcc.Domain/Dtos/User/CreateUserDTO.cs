
namespace gestaotcc.Domain.Dtos.User;
public record CreateUserDTO(
    string Name, 
    string Email, 
    string? Registration, 
    string CPF, 
    string? SIAPE, 
    List<string> Profile, 
    long CourseId,
    long CampusId);
