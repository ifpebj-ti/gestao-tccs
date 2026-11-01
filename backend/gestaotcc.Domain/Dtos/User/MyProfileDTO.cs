namespace gestaotcc.Domain.Dtos.User;

public record MyProfileDTO(
    long Id,
    string Name,
    string Email,
    string Phone,
    string? Registration,
    string? Siape,
    string? Campus,
    string? Course,
    IReadOnlyList<string> Roles,
    string? Cpf,
    string? UserClass,
    string? Shift);
