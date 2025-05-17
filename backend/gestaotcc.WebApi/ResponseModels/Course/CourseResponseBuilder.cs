namespace gestaotcc.WebApi.ResponseModels.Course;

public class CourseResponseBuilder
{
    private long _id;
    private string _name = string.Empty;
    private string _description = string.Empty;

    public CourseResponseBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public CourseResponseBuilder WithName(string role)
    {
        _name = role;
        return this;
    }

    public CourseResponseBuilder WithDescription(string role)
    {
        _description = role;
        return this;
    }

    public CourseResponse Build()
    {
        return new CourseResponse(_id, _name, _description);
    }
}
