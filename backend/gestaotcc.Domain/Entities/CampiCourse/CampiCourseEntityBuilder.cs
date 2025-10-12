using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.CampiCourse;

public class CampiCourseEntityBuilder
{
    private long _id;
    private CampiEntity _campi = null!;
    private CourseEntity _course = null!;
    private ICollection<UserEntity> _users = null!;
    private long _campiId;
    private long _courseId;
    
    public CampiCourseEntityBuilder WithId(long id)
    {
        _id = id;
        return this;
    }

    public CampiCourseEntityBuilder WithCampiId(long campiId)
    {
        _campiId = campiId;
        return this;
    }

    public CampiCourseEntityBuilder WithCourseId(long courseId)
    {
        _courseId = courseId;
        return this;
    }

    public CampiCourseEntityBuilder WithCampi(CampiEntity campi)
    {
        _campi = campi;
        return this;
    }

    public CampiCourseEntityBuilder WithCourse(CourseEntity course)
    {
        _course = course;
        return this;
    }

    public CampiCourseEntityBuilder WithUser(ICollection<UserEntity> users)
    {
        _users = users;
        return this;
    }

    public CampiCourseEntity Build()
    {
        return new CampiCourseEntity(_id, _campi, _course, _users, _campiId, _courseId);
    }
}