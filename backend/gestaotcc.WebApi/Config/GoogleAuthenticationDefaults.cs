namespace gestaotcc.WebApi.Config;

public static class GoogleAuthenticationDefaults
{
    public const string Scheme = "GoogleOpenIdConnect";
    public const string ExternalCookieScheme = "GoogleExternalCookie";
    public const string ExternalCookieName = ".GradusFlow.Google.External";
    public const string LocalJwtCookieName = "token";
}