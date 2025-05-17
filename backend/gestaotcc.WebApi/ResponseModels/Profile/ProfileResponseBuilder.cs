namespace gestaotcc.WebApi.ResponseModels.Profile;

public class ProfileResponseBuilder
{
    private long _id;
    private string _role = string.Empty;

    public ProfileResponseBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public ProfileResponseBuilder WithRole(string role)
    {
        _role = role;
        return this;
    }

    public ProfileResponse Build()
    {
        return new ProfileResponse(_id, _role);
    }
}
