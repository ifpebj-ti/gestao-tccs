using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;

namespace gestaotcc.Application.Factories;

public class UserTccFactory
{
    public static UserTccEntity CreateUserTcc(UserEntity user, TccEntity tcc)
    {
        return new UserTccEntityBuilder()
            .WithTcc(tcc)
            .WithUser(user)
            .Build();
    }
}