using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;

namespace gestaotcc.Domain.Entities.Campi;

public class CampiEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<CampiCourseEntity> CampiCourses { get; set; } = new List<CampiCourseEntity>();
    public CampiEntity() {}

    public CampiEntity(long id, string name, ICollection<CampiCourseEntity> campiCourses)
    {
        Id = id;
        Name = name;
        CampiCourses = campiCourses;
    }
}