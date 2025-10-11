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

        var keyEncoded = Encoding.ASCII.GetBytes(key);
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("userId", user.Id.ToString()),
                new Claim("campiId", user.CampiCourseId.ToString()!)
            }.Concat(user.Profile.Select(p => new Claim(ClaimTypes.Role, p.Role)))
            ),
            Expires = DateTime.UtcNow.AddHours(expirationTime),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyEncoded), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
