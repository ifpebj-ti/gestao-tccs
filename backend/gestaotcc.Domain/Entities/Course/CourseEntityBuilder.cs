using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Course;

public class CourseEntityBuilder
{
    private long _id;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private ICollection<UserEntity> _users = new List<UserEntity>();

    public CourseEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public CourseEntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CourseEntityBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CourseEntityBuilder WithUsers(ICollection<UserEntity> users)
    {
        _users = users;
        return this;
    }

    public CourseEntity Build()
    {
        return new CourseEntity(_id, _name, _description, _users);
    }
}