using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;

namespace gestaotcc.Application.Factories;

public class UserTccFactory
{
    public static UserTccEntity CreateUserTcc(UserEntity user, TccEntity tcc, ProfileEntity profile)
    {
        return new UserTccEntityBuilder()
            .WithTcc(tcc)
            .WithUser(user)
            .WithProfile(profile)
            .Build();
    }
}