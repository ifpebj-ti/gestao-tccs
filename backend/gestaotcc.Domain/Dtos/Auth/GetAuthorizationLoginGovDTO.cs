namespace gestaotcc.Domain.Dtos.Auth;

public record GetAuthorizationLoginGovDTO(
    string ClientId,
    string Scope,
    string RedirectUri,
    string GovAuthUrl
    );