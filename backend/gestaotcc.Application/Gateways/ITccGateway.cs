using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;

namespace gestaotcc.Application.Gateways;

public interface ITccGateway
{
    Task Save(TccEntity tcc);
    Task<List<TccInviteEntity>> FindAllInviteTcc();
    Task<TccEntity?> FindTccById(long id);
}