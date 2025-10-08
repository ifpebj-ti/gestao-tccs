using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.UserTcc;

namespace gestaotcc.Domain.Entities.User;

public class UserEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Registration { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string? SIAPE { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ICollection<ProfileEntity> Profile { get; set; } = null!;
    public CampiCourseEntity? CampiCourse { get; set; }
    public long? CampiCourseId { get; set; }
    public AccessCodeEntity AccessCode { get; set; } = null!;
    public ICollection<UserTccEntity> UserTccs { get; set; } = null!;
    public ICollection<SignatureEntity> Signatures { get; set; } = null!;
    public ICollection<DocumentEntity>? Documents { get; set; } = null!;
    public UserEntity() { }

    public UserEntity(
        long id, 
        string name, 
        string email,
        string registration,
        string cpf,
        string siape,
        string password, 
        string status, 
        ICollection<ProfileEntity> profile,
        CampiCourseEntity campiCourse,
        AccessCodeEntity accessCode, 
        ICollection<UserTccEntity> userTccs,
        ICollection<SignatureEntity> signatures)
    {
        Id = id;
        Name = name;
        Email = email;
        Registration = registration;
        CPF = cpf;
        SIAPE = siape;
        Password = password;
        Status = status;
        Profile = profile;
        CampiCourse = campiCourse;
        AccessCode = accessCode;
        UserTccs = userTccs;
        Signatures = signatures;
    }
}