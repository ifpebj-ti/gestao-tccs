using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.Helpers;

public static class TccUploadAuthorization
{
    public static bool Allows(UserEntity user, TccEntity tcc, bool allowStudent = true)
    {
        var roles = user.Profile.Select(p => p.Role).ToArray();
        if (roles.Contains("ADMIN")) return true;
        if (tcc.UserTccs.Any(link => link.UserId == user.Id &&
            (link.Profile.Role == "ADVISOR" || allowStudent && link.Profile.Role == "STUDENT") &&
            roles.Contains(link.Profile.Role))) return true;
        return roles.Any(role => role is "COORDINATOR" or "SUPERVISOR") && user.CampiCourseId is > 0 &&
            tcc.UserTccs.Any(link => link.Profile.Role == "STUDENT" && link.User?.CampiCourseId == user.CampiCourseId);
    }
}
