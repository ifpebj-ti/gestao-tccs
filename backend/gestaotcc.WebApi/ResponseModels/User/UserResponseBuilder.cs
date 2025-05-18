using gestaotcc.WebApi.ResponseModels.Course;
using gestaotcc.WebApi.ResponseModels.Profile;

namespace gestaotcc.WebApi.ResponseModels.User;

public class UserResponseBuilder
{
    private long _id;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _status = string.Empty;
    private ICollection<ProfileResponse> _profile = new List<ProfileResponse>();
    private CourseResponse _course = null!;

    public UserResponseBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public UserResponseBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UserResponseBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserResponseBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public UserResponseBuilder WithStatus(string status)
    {
        _status = status;
        return this;
    }

    public UserResponseBuilder WithProfile(ICollection<ProfileResponse> profile)
    {
        _profile = profile;
        return this;
    }

    public UserResponseBuilder WithCourse(CourseResponse course)
    {
        _course = course;
        return this;
    }

    public UserResponse Build()
    {
        return new UserResponse(_id, _name, _email, _password, _status, _profile, _course);
    }
}