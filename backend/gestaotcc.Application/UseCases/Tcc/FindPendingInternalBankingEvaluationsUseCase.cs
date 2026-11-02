using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindPendingInternalBankingEvaluationsUseCase(ITccGateway tccGateway)
{
    public async Task<List<PendingInternalBankingEvaluationDTO>> Execute(long userId)
    {
        var members = await tccGateway.FindPendingInternalBankingMembers(userId);
        return members.Select(member => new PendingInternalBankingEvaluationDTO(
            member.TccId,
            member.Tcc.Title,
            member.Tcc.TccSchedule!.ScheduledDate,
            member.Tcc.TccSchedule.Location,
            member.Tcc.UserTccs
                .Where(userTcc => userTcc.Profile?.Role == "STUDENT")
                .Select(userTcc => userTcc.User.Name)
                .Distinct()
                .ToList()))
            .ToList();
    }
}
