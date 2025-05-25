using gestaotcc.WebApi.ResponseModels.Course;
using gestaotcc.WebApi.ResponseModels.Profile;

namespace gestaotcc.WebApi.ResponseModels.User;

public class UserResponseBuilder
{
    private long _id;
    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _registration = string.Empty;
    private string _cpf = string.Empty;
    private string _siape = string.Empty;
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

    public UserResponseBuilder WithRegistration(string registration)
    {
        _registration = registration;
        return this;
    }

    public UserResponseBuilder WithCpf(string cpf)
    {
        _cpf = cpf;
        return this;
    }

    public UserResponseBuilder WithSiape(string siape)
    {
        _siape = siape;
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
        return new UserResponse(_id, _name, _email, _registration, _cpf, _siape, _password, _status, _profile, _course);
    }
}