using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;

namespace gestaotcc.Application.Gateways;

public interface ITccGateway
{
    Task Save(TccEntity tcc);
    Task<List<TccInviteEntity>> FindAllInviteTcc();
    Task<TccInviteEntity?> FindInviteTccByEmail(string email);
    Task UpdateTccInvite(TccInviteEntity tccInvite);
    Task<TccEntity?> FindTccById(long id);
}