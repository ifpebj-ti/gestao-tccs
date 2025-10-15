namespace gestaotcc.Domain.Dtos.Auth;

public record ResponseGetGovTokenDTO(string AccessToken, string IdToken, string TokenType, int ExpiresIn);