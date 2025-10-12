using gestaotcc.Domain.Entities.TccInvite;

namespace gestaotcc.Application.Factories;

public class TccInviteFactory
{
    public static TccInviteEntity CreateTccInvite(string email, string code, long campiId, long courseId)
    {
        return new TccInviteEntityBuilder()
            .WithCode(code)
            .WithEmail(email)
            .WithCampiId(campiId)
            .WithCourseId(courseId)
            .Build();
    }
}