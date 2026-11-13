using gestaotcc.Domain.Entities.Tcc;

namespace gestaotcc.Application.UseCases.Tcc;

internal static class TccAdvisorAuthorization
{
    public static bool IsAdvisorOf(TccEntity tcc, long userId) =>
        tcc.UserTccs.Any(member =>
            member.UserId == userId &&
            string.Equals(member.Profile?.Role, "ADVISOR", StringComparison.OrdinalIgnoreCase));
}
