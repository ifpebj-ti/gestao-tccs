using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace gestaotcc.Infra.Gateways;
public class TokenGateway(IConfiguration configuration) : ITokenGateway
{
    public string? CreateAccessToken(UserEntity user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSettings = configuration.GetSection("JWT_SETTINGS");

        var key = jwtSettings.GetValue<string>("TOKEN_KEY");
        if (key is null) return null;

        var expirationTime = jwtSettings.GetValue<int>("ACCESSTOKEN_EXPIRATION_TIME");

        var keyEncoded = Encoding.UTF8.GetBytes(key);
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Issuer = jwtSettings.GetValue<string>("ISSUER") ?? "gradusflow",
            Audience = jwtSettings.GetValue<string>("AUDIENCE") ?? "gradusflow.web",
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim("securityStamp", gestaotcc.Infra.Security.AccessTokenStamp.Create(user.Id, user.Password, key)),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("userId", user.Id.ToString()),
                new Claim("campiCourseId", user.CampiCourseId.ToString()!)
            }.Concat(user.Profile.Select(p => new Claim(ClaimTypes.Role, p.Role)))
            ),
            // Preserve the existing setting's unit (hours); do not silently turn 8 hours into 8 minutes.
            Expires = DateTime.UtcNow.AddHours(expirationTime > 0 ? Math.Min(expirationTime, 8) : 1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyEncoded), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
