using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.UserTcc;

namespace gestaotcc.Domain.Entities.User;

public class UserEntityBuilder
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
    private ICollection<ProfileEntity> _profile = new List<ProfileEntity>();
    private AccessCodeEntity _accessCode = null!;
    private CampiCourseEntity _campiCourse = null!;
    private ICollection<UserTccEntity> _userTccs = new List<UserTccEntity>();
    private ICollection<SignatureEntity> _signatures = new List<SignatureEntity>();

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

    public UserEntityBuilder WithRegistration(string registration)
    {
        _registration = registration;
        return this;
    }

    public UserEntityBuilder WithCpf(string cpf)
    {
        _cpf = cpf;
        return this;
    }

    public UserEntityBuilder WithSiape(string siape)
    {
        _siape = siape;
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

    public UserEntityBuilder WithPhone(string phone)
    {
        _phone = phone;
        return this;
    }

    public UserEntityBuilder WithUserClass(string? userClass)
    {
        _userClass = userClass;
        return this;
    }

    public UserEntityBuilder WithShift(string? shift)
    {
        _shift = shift;
        return this;
    }

    public UserEntityBuilder WithTitration(string? titration)
    {
        _titration = titration;
        return this;
    }
    public UserEntityBuilder WithProfile(ICollection<ProfileEntity> profile)
    {
        _profile = profile;
        return this;
    }

    public UserEntityBuilder WithCampiCourse(CampiCourseEntity campiCourse)
    {
        _campiCourse = campiCourse;
        return this;
    }
    
    public UserEntityBuilder WithAccessCode(AccessCodeEntity accessCode)
    {
        _accessCode = accessCode;
        return this;
    }

    public UserEntityBuilder WithUserTcc(ICollection<UserTccEntity> userTccs)
    {
        _userTccs = userTccs;
        return this;
    }

    public UserEntityBuilder WithSignature(ICollection<SignatureEntity> signatures)
    {
        _signatures = signatures;
        return this;
    }
    public UserEntity Build()
    {
        return new UserEntity(
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
            _campiCourse, 
            _accessCode, 
            _userTccs, 
            _signatures);
    }
}