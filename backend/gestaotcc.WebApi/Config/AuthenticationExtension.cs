using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace gestaotcc.WebApi.Config;

public static class AuthenticationExtension
{
    public static void AddAuthenticationExtension(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JWT_SETTINGS");
        var key = jwtSettings.GetValue<string>("TOKEN_KEY");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults
                .AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = false;
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key!)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Garante que a expiração seja aplicada imediatamente
                ValidateIssuer = false,
                ValidateAudience = false
            };
        });
    }
}