using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Entities.TccInvite;

namespace gestaotcc.Application.Gateways;

public interface ITccGateway
{
    Task Save(TccEntity tcc);
    Task Update(TccEntity tcc);
    Task<List<TccInviteEntity>> FindAllInviteTcc();
    Task<TccInviteEntity?> FindInviteTccByEmail(string email);
    Task UpdateTccInvite(TccInviteEntity tccInvite);
    Task<TccEntity?> FindTccById(long id);
    Task<TccEntity?> FindTccWorkflow(long? tccId, long userId);
    Task<List<TccEntity>> FindAllTccByFilter(TccFilterDTO tccFilter);
    Task<TccEntity?> FindTccCancellation(long id);
    Task<TccEntity?> FindTccScheduling(long id);
    Task<TccEntity?> FindTccInformations(long id);
}