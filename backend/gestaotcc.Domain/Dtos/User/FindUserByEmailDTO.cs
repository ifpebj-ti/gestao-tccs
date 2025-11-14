
namespace gestaotcc.Domain.Dtos.User;

public record FindUserByEmailDTO(
    long Id,
    string Name,
    string Email,
    string Status,
    string? UserClass,
    string? Shift,
    string? Titration
    );
