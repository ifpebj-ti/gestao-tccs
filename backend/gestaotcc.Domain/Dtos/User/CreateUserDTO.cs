
namespace gestaotcc.Domain.Dtos.User;
public record CreateUserDTO(string Name, string Email, string? Registration, string CPF, string? SIAPE, List<string> Profile, string Course);
