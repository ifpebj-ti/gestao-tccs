namespace gestaotcc.Domain.Entities.AccessCode;

public class AccessCodeEntityBuilder
{
    private long _id;
    private string _code = string.Empty;
    private bool _isActive = true;
    private bool _isUserUpdatePassword = false;
    private DateTime _expirationDate;

    public AccessCodeEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public AccessCodeEntityBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public AccessCodeEntityBuilder WithIsActive(bool isActive)
    {
        _isActive = isActive;
        return this;
    }

    public AccessCodeEntityBuilder WithIsUserUpdatePassword(bool isUserUpdatePassword)
    {
        _isUserUpdatePassword = isUserUpdatePassword;
        return this;
    }

    public AccessCodeEntityBuilder WithExpirationDate(DateTime expirationDate)
    {
        _expirationDate = expirationDate;
        return this;
    }
    

    public AccessCodeEntity Build()
    {
        return new AccessCodeEntity(_id, _code, _isActive, _isUserUpdatePassword, _expirationDate);
    }
}