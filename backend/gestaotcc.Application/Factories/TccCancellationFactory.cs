using gestaotcc.Domain.Entities.TccCancellation;

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
}
