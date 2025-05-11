using gestaotcc.Domain.Entities.AccessCode;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.Profile;

namespace gestaotcc.Domain.Entities.User;

public class UserEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ICollection<ProfileEntity> Profile { get; set; } = null!;
    public CourseEntity Course { get; set; } = null!;
    public long CourseId { get; set; }
    public AccessCodeEntity AccessCode { get; set; } = null!;
    public UserEntity() { }

    public UserEntity(long id, string name, string email, string password, string status, ICollection<ProfileEntity> profile, CourseEntity course, AccessCodeEntity accessCode)
    {
        Id = id;
        Name = name;
        Email = email;
        Password = password;
        Status = status;
        Profile = profile;
        Course = course;
        AccessCode = accessCode;
    }
}