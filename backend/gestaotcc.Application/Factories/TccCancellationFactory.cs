using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Enums;

namespace gestaotcc.Application.Factories;
public class TccCancellationFactory
{
    public static TccCancellationEntity CreateTccCancellation(long tccId, string reason)
    {
        return new TccCancellationEntityBuilder()
            .WithTccId(tccId)
            .WithReason(reason)
            .Build();
    }

    public static FindTccCancellationDTO CreateFindTccCancellationDTO(TccEntity tcc)
    {
        return new FindTccCancellationDTO(
            tcc.Title!,
            tcc.UserTccs
                .Where(ut => ut.Profile.Role == RoleType.STUDENT.ToString())
                .Select(ut => ut.User.Name)
                .ToList(),
            tcc.UserTccs
                .Where(ut => ut.Profile.Role == RoleType.ADVISOR.ToString())
                .Select(ut => ut.User.Name)
                .FirstOrDefault()!,
            tcc.TccCancellation?.Reason ?? "Motivo de cancelamento não informado"
        );
    }
}
