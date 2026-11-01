using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Entities.TccInvite;

namespace gestaotcc.Application.Gateways;

public interface ITccGateway
{
    Task Save(TccEntity tcc);
    Task Update(TccEntity tcc);
    Task UpdateSemesterId(long tccId, long semesterId);
    Task<List<TccInviteEntity>> FindAllInviteTcc();
    Task<TccInviteEntity?> FindInviteTccByEmail(string email);
    Task SaveTccInvite(TccInviteEntity tccInvite);
    Task UpdateTccInvite(TccInviteEntity tccInvite);
    Task<TccEntity?> FindTccById(long id);
    Task<TccEntity?> FindTccWorkflow(long? tccId, long userId);
    Task<List<TccEntity>> FindAllTccByFilter(TccFilterDTO tccFilter, long campiCourseId);
    Task<TccEntity?> FindTccCancellation(long id);
    Task<TccEntity?> FindTccScheduling(long id);
    Task<TccEntity?> FindTccInformations(long id);
    Task<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity?> FindBankingMemberByToken(string token);
    Task UpdateBankingMember(gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity member);
    Task<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity?> FindPendingAdvisorChange(long tccId);
    Task SaveAdvisorChange(gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity advisorChange);
    Task UpdateAdvisorChange(gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity advisorChange);
}