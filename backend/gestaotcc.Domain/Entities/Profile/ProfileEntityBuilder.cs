using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Profile;

public class ProfileEntityBuilder
{
    private long _id;
    private string _role = string.Empty;

    public ProfileEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public ProfileEntityBuilder WithRole(string role)
    {
        _role = role;
        return this;
    }
    

    public ProfileEntity Build()
    {
        return new ProfileEntity(_id, _role);
    }
}