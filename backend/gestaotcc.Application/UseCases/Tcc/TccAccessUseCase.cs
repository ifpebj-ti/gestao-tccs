using gestaotcc.Application.Gateways;
using gestaotcc.Application.Helpers;

namespace gestaotcc.Application.UseCases.Tcc;

public class TccAccessUseCase(ITccGateway tccGateway, IUserGateway userGateway)
{
    public async Task<bool> CanRead(long tccId, long userId)
    {
        var user = await userGateway.FindById(userId);
        return user is { Status: "ACTIVE" } && await tccGateway.FindTccById(tccId) is { } tcc &&
            TccReadAuthorization.Allows(user, tcc);
    }

    public async Task<bool> CanManage(long tccId, long userId)
    {
        var user = await userGateway.FindById(userId);
        return user is { Status: "ACTIVE" } && await tccGateway.FindTccById(tccId) is { } tcc &&
            TccUploadAuthorization.Allows(user, tcc, allowStudent: false);
    }

    public async Task<bool> CanRequestCancellation(long tccId, long userId)
    {
        var user = await userGateway.FindById(userId);
        return user is { Status: "ACTIVE" } && user.Profile.Any(profile => profile.Role == "STUDENT") &&
            await tccGateway.FindTccById(tccId) is { } tcc &&
            tcc.UserTccs.Any(link => link.UserId == userId && link.Profile.Role == "STUDENT");
    }
}
