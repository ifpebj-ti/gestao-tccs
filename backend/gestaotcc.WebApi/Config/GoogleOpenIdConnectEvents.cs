using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;

namespace gestaotcc.WebApi.Config;

public sealed class GoogleOpenIdConnectEvents(IConfiguration configuration) : OpenIdConnectEvents
{
    private const string GenericFailure = "Não foi possível concluir o login com o Google.";

    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        var redirectUri = configuration["GoogleAuth:RedirectUri"];
        if (Uri.TryCreate(redirectUri, UriKind.Absolute, out var configuredUri))
            context.ProtocolMessage.RedirectUri = configuredUri.AbsoluteUri;
        return Task.CompletedTask;
    }

    public override Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
    {
        var redirectUri = configuration["GoogleAuth:RedirectUri"];
        if (Uri.TryCreate(redirectUri, UriKind.Absolute, out var configuredUri))
            context.TokenEndpointRequest!.RedirectUri = configuredUri.AbsoluteUri;
        return Task.CompletedTask;
    }

    public override async Task RemoteFailure(RemoteFailureContext context)
    {
        context.HandleResponse();
        await context.HttpContext.SignOutAsync(GoogleAuthenticationDefaults.ExternalCookieScheme);
        var frontendUrl = configuration["CORS_SETTINGS:URL_FRONT"];
        if (Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontendUri) &&
            (frontendUri.Scheme == Uri.UriSchemeHttps || frontendUri.Scheme == Uri.UriSchemeHttp))
        {
            context.Response.Redirect(QueryHelpers.AddQueryString(frontendUri.AbsoluteUri, "googleError", "failed"));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = GenericFailure });
    }
}