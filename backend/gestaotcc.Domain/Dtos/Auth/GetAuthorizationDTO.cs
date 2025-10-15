namespace gestaotcc.Domain.Dtos.Auth;

public record GetAuthorizationDTO(
    string ClientId,
    string Scope,
    string RedirectUri,
    string GovAuthUrl
    );