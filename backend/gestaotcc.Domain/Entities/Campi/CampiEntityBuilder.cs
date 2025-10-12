using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;

namespace gestaotcc.Domain.Entities.Campi;

public class CampiEntityBuilder
{
    private long _id;
    private string _name = string.Empty;
    private ICollection<CampiCourseEntity> _campiCourses = new List<CampiCourseEntity>();

    public CampiEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public CampiEntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CampiEntityBuilder WithCourses(ICollection<CampiCourseEntity> campiCourses)
    {
        _campiCourses = campiCourses;
        return this;
    }

    public CampiEntity Build()
    {
        return new CampiEntity(_id, _name, _campiCourses);
    }
}