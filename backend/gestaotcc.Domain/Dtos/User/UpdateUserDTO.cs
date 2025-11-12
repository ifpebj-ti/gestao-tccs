using gestaotcc.Domain.Enums;

namespace gestaotcc.Domain.Dtos.User;
public record UpdateUserDTO(
    long Id,
    string Name, 
    string Email, 
    string Registration, 
    string Cpf, 
    string Siape, 
    List<string> Profile, 
    string Status,
    long CampiId,
    long CourseId);

