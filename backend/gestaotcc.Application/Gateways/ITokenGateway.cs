using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Gateways;
public interface ITokenGateway
{
    string? CreateAccessToken(UserEntity user);
    DecodedIdTokenDTO DecodeToken(string idToken);
}
