using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;

namespace gestaotcc.Domain.Entities.User;

public class UserEntityBuilder
{
    private long _id;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _status = string.Empty;
    private ICollection<ProfileEntity> _profile = new List<ProfileEntity>();
    private CourseEntity _course = null!;
    private AccessCodeEntity _accessCode = null!;

    public UserEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public UserEntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserEntityBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserEntityBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public UserEntityBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public UserEntityBuilder WithProfile(ICollection<ProfileEntity> profile)
    {
        _profile = profile;
        return this;
    }

    public UserEntityBuilder WithCourse(CourseEntity course)
    {
        _course = course;
        return this;
    }
    
    public UserEntityBuilder WithAccessCode(AccessCodeEntity accessCode)
    {
        _accessCode = accessCode;
        return this;
    }

    public UserEntity Build()
    {
        return new UserEntity(_id, _name, _email, _password, _status, _profile, _course, _accessCode);
    }
}