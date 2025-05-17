
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Gateways;
public interface IUserGateway
{
    Task Save(UserEntity user);
    Task<UserEntity?> FindByEmail(string email);
}
