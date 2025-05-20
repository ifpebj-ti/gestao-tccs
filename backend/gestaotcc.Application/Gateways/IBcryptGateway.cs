using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Gateways;
public interface IBcryptGateway
{
    bool VerifyHashPassword(UserEntity user, string password);
    string GenerateHashPassword(string password);
}
