using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.User;
using gestaotcc.WebApi.ResponseModels.CampiCourse;
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
    private string _phone = string.Empty;
    private string? _userClass;
    private string? _shift;
    private string? _titration;
    private ICollection<ProfileResponse> _profile = new List<ProfileResponse>();
    private CourseResponse _course = null!;
    private CampiCourseResponse _campiCourse = null!;

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

    public UserResponseBuilder WithPhone(string phone)
    {
        _phone = phone;
        return this;
    }

    public UserResponseBuilder WithUserClass(string? userClass)
    {
        _userClass = userClass;
        return this;
    }

    public UserResponseBuilder WithShift(string? shift)
    {
        _shift = shift;
        return this;
    }

    public UserResponseBuilder WithTitration(string? titration)
    {
        _titration = titration;
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

    public UserResponseBuilder WithCampiCourses(CampiCourseResponse campiCourse)
    {
        _campiCourse = campiCourse;
        return this;
    }

    public UserResponse Build()
    {
        return new UserResponse(
            _id, 
            _name, 
            _email, 
            _registration, 
            _cpf, 
            _siape, 
            _password, 
            _status, 
            _phone, 
            _userClass, 
            _shift, 
            _titration, 
            _profile, 
            _campiCourse);
    }
}