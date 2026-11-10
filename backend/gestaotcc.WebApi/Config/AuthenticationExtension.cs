using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using gestaotcc.Application.Gateways;
using gestaotcc.Infra.Security;

namespace gestaotcc.WebApi.Config;

public static class AuthenticationExtension
{
    public static void AddAuthenticationExtension(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JWT_SETTINGS");
        var key = jwtSettings.GetValue<string>("TOKEN_KEY");
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32 ||
            key == "GestaoTCCs2024SecretKeyForJWT123456")
            throw new InvalidOperationException("Configure JWT_SETTINGS__TOKEN_KEY com pelo menos 32 bytes.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults
                .AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = true;
            x.SaveToken = false;
            x.IncludeErrorDetails = false;
            x.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (!long.TryParse(context.Principal?.FindFirst("userId")?.Value, out var userId) || userId <= 0)
                    { context.Fail("Sessão inválida."); return; }
                    var users = context.HttpContext.RequestServices.GetRequiredService<IUserGateway>();
                    var user = await users.FindById(userId);
                    var stamp = context.Principal?.FindFirst("securityStamp")?.Value;
                    if (user is null || user.Status != "ACTIVE" || stamp is null || stamp.Length != 64 ||
                        context.Principal?.FindFirst("campiCourseId")?.Value != (user.CampiCourseId?.ToString() ?? "") ||
                        !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(stamp),
                            Encoding.ASCII.GetBytes(AccessTokenStamp.Create(user.Id, user.Password, key))))
                    { context.Fail("Sessão inválida."); return; }
                    var currentRoles = user.Profile.Select(profile => profile.Role).Distinct().OrderBy(role => role, StringComparer.Ordinal);
                    var tokenRoles = context.Principal!.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().OrderBy(role => role, StringComparer.Ordinal);
                    if (!currentRoles.SequenceEqual(tokenRoles)) context.Fail("Sessão inválida.");
                }
            };
            x.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Garante que a expiração seja aplicada imediatamente
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.GetValue<string>("ISSUER") ?? "gradusflow",
                ValidateAudience = true,
                ValidAudience = jwtSettings.GetValue<string>("AUDIENCE") ?? "gradusflow.web"
            };
        });
    }
}
