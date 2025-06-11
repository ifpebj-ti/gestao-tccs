using gestaotcc.Domain.Entities.Profile;

namespace gestaotcc.Application.Gateways;
public interface IProfileGateway
{
    Task<List<ProfileEntity>> FindByRole(List<string> role);
    Task<ProfileEntity?> FindByRole(string role);
    Task<List<ProfileEntity>> FindAll();
}
