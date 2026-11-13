using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

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
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = GenericFailure });
    }
}