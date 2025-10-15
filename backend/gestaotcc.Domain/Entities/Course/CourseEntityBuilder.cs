using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Course;

public class CourseEntityBuilder
{
    private long _id;
    private string _name = string.Empty;
    private string _level = string.Empty;

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

    public CourseEntityBuilder WithDescription(string level)
    {
        _level = level;
        return this;
    }

    public CourseEntity Build()
    {
        return new CourseEntity(_id, _name, _level);
    }
}