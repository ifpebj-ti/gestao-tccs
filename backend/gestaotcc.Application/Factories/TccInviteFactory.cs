using gestaotcc.Domain.Entities.TccInvite;

namespace gestaotcc.Application.Factories;

public class TccInviteFactory
{
    public static TccInviteEntity CreateTccInvite(string email, string code)
    {
        return new TccInviteEntityBuilder()
            .WithCode(code)
            .WithEmail(email)
            .Build();
    }
}