using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.Gateways;

public interface IProposalGateway
{
    Task<ResultPattern<T>> Atomic<T>(Func<Task<ResultPattern<T>>> action);
    Task<List<ProposalParticipantEntity>> PendingFor(long userId);
    Task<ProposalParticipantEntity?> Find(long invitationId);
    Task<List<ProposalParticipantEntity>> DeclinedFor(long userId);
}
