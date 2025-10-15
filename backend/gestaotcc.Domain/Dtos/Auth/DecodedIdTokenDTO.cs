namespace gestaotcc.Domain.Dtos.Auth;

public record DecodedIdTokenDTO(string Cpf, string? CompleteName, string? Email, string? PhoneNumber);