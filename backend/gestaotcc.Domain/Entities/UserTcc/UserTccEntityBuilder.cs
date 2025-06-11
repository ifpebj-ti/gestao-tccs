using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.UserTcc;

public class UserTccEntityBuilder
{
    private long _id;
    private UserEntity _user = null!;
    private TccEntity _tcc = null!;
    private ProfileEntity _profile = null!;
    private DateTime _bindingDate;

    public UserTccEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public UserTccEntityBuilder WithUser(UserEntity user)
    {
        _user = user;
        return this;
    }

    public UserTccEntityBuilder WithTcc(TccEntity tcc)
    {
        _tcc = tcc;
        return this;
    }

    public UserTccEntityBuilder WithProfile(ProfileEntity profile)
    {
        _profile = profile;
        return this;
    }

    public UserTccEntityBuilder WithBindingDate(DateTime bindingDate)
    {
        _bindingDate = bindingDate;
        return this;
    }

    public UserTccEntity Build()
    {
        return new UserTccEntity(_user, _tcc, _profile, _bindingDate);
    }
}