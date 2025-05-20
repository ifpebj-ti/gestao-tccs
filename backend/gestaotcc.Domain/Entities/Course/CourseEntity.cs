using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Course;

public class CourseEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<UserEntity> Users { get; set; } = null!;
    public CourseEntity() { }

    public CourseEntity(long id, string name, string description, ICollection<UserEntity> users)
    {
        Id = id;
        Name = name;
        Description = description;
        Users = users;
    }
}