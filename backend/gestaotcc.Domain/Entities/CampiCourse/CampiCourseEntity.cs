using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Domain.Entities.CampiCourse;

public class CampiCourseEntity
{
    public long Id { get; set; }
    public long CampiId { get; set; }
    public CampiEntity Campi { get; set; } = null!;
    public long CourseId { get; set; }
    public CourseEntity Course { get; set; } = null!;
    public ICollection<UserEntity> Users { get; set; } = null!;
    public CampiCourseEntity() {}

    public CampiCourseEntity(long id, CampiEntity campi, CourseEntity course, ICollection<UserEntity> users, long campiId, long courseId)
    {
        Id = id;
        Campi = campi;
        Course = course;
        Users = users;
        CampiId = campiId;
        CourseId = courseId;
    }
}