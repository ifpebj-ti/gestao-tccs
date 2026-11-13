using System.Text;
using gestaotcc.WebApi.Config;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace gestaotcc.WebApi.Config;

public static class AuthenticationExtension
{
    public static void AddAuthenticationExtension(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JWT_SETTINGS");
        var key = jwtSettings.GetValue<string>("TOKEN_KEY");
        var googleSettings = configuration.GetSection("GoogleAuth");
        var clientId = googleSettings.GetValue<string>("ClientId") ?? string.Empty;
        var clientSecret = googleSettings.GetValue<string>("ClientSecret") ?? string.Empty;
        var configuredRedirectUri = googleSettings.GetValue<string>("RedirectUri");
        var callbackPath = new PathString("/api/Auth/google/callback");

        if (Uri.TryCreate(configuredRedirectUri, UriKind.Absolute, out var redirectUri))
            callbackPath = new PathString(redirectUri.AbsolutePath);

        services.AddScoped<GoogleOpenIdConnectEvents>();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key!)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuer = false,
                ValidateAudience = false
            };
        })
        .AddCookie(GoogleAuthenticationDefaults.ExternalCookieScheme, options =>
        {
            options.Cookie.Name = GoogleAuthenticationDefaults.ExternalCookieName;
            options.Cookie.Path = "/api/Auth/google";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            options.SlidingExpiration = false;
        })
        .AddOpenIdConnect(GoogleAuthenticationDefaults.Scheme, options =>
        {
            options.Authority = "https://accounts.google.com";
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.CallbackPath = callbackPath;
            options.SignInScheme = GoogleAuthenticationDefaults.ExternalCookieScheme;
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.RequireHttpsMetadata = true;
            options.ProtocolValidator.RequireNonce = true;
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidIssuers = ["https://accounts.google.com", "accounts.google.com"],
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                NameClaimType = "name"
            };
            options.EventsType = typeof(GoogleOpenIdConnectEvents);
        });
    }
}