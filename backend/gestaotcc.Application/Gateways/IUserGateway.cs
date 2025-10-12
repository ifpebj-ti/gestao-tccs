
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Gateways;
public interface IUserGateway
{
    Task<UserEntity?> FindByEmail(string email);
    Task<UserEntity?> FindById(long id);
    Task Save(UserEntity user);
    Task Update(UserEntity user);
    Task<List<UserEntity>> FindAllByEmail(List<string> emails);
    Task<List<UserEntity>> FindAllByFilter(UserFilterDTO filter, long campiCourseId);
}
