namespace gestaotcc.Domain.Dtos.Auth;

public record ResponseGetSignatureGovTokenDTO(string AccessToken, string TokenType, long ExpiresIn);