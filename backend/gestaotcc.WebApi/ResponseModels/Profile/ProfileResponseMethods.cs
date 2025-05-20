using gestaotcc.Domain.Entities.Profile;

namespace gestaotcc.WebApi.ResponseModels.Profile;

public class ProfileResponseMethods
{
    public static List<ProfileResponse> CreateProfileResponse(ICollection<ProfileEntity> profiles)
    {
        return profiles.Select(p => new ProfileResponseBuilder()
            .WithId(p.Id)
            .WithRole(p.Role)
            .Build())
            .ToList();
    }
}
