using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Helpers;

public static class TccReadAuthorization
{
    public static bool Allows(UserEntity user, TccEntity tcc)
    {
        if (user.Status != "ACTIVE") return false;

        var roles = user.Profile.Select(profile => profile.Role).ToHashSet();
        if (roles.Contains("ADMIN")) return true;
        if (tcc.UserTccs.Any(link => link.UserId == user.Id && roles.Contains(link.Profile.Role))) return true;

        return roles.Any(role => role is "COORDINATOR" or "SUPERVISOR" or "LIBRARY") &&
            user.CampiCourseId is > 0 &&
            tcc.UserTccs.Any(link => link.Profile.Role == "STUDENT" && link.User?.CampiCourseId == user.CampiCourseId);
    }
}
