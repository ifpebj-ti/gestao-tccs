namespace gestaotcc.Domain.Dtos.User;
public record FindAllUserDTO(
    long Id,
    string Name,
    string Email,
    string Profile,
    string? Registration,
    string Cpf,
    string? Siape,
    string Phone,
    string? UserClass,
    string? Shift,
    string? Titration,
    CampiDetailsForFindAllUserDTO Campus,
    string Status
    );

