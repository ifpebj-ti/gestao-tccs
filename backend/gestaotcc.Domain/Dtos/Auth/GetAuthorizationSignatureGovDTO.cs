namespace gestaotcc.Domain.Dtos.Auth;

public record GetAuthorizationSignatureGovDTO(
    string ClientId,
    string Scope,
    string RedirectUri,
    string GovAuthSignUrl
    );