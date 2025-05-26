using gestaotcc.Domain.Dtos.Profile;
using gestaotcc.Domain.Entities.Profile;

namespace gestaotcc.Application.Factories;
public class ProfileFactory
{
    public static FindAllProfilesDTO CreateFindAllProfilesDTO(ProfileEntity profile)
    {
        return new FindAllProfilesDTO(
            profile.Id,
            profile.Role
        );
    }
}
