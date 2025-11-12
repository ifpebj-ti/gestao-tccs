namespace gestaotcc.Domain.Dtos.User;
public record FindAllUserDTO(
    long Id,
    string Name,
    string Email,
    string Profile,
    string? Registration,
    string Cpf,
    string? Siape,
    CampiDetailsForFindAllUserDTO Campus,
    string Status
    );

