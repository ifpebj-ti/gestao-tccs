using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Profile;

public class ProfileEntity
{
    public long Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public ICollection<UserEntity> Users { get; set; } = null!;
    public ProfileEntity() { }

    public ProfileEntity(long id, string role)
    {
        Id = id;
        Role = role;
    }
}