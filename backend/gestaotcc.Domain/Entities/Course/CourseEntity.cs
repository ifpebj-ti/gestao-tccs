using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.Course;

public class CourseEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public ICollection<CampiCourseEntity> CampiCourses { get; set; } = null!;
    public CourseEntity() { }

    public CourseEntity(long id, string name, string level)
    {
        Id = id;
        Name = name;
        Level = level;
    }
}