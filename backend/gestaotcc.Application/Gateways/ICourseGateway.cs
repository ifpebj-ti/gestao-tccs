using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;

namespace gestaotcc.Application.Gateways;
public interface ICourseGateway
{
    Task<CourseEntity> FindByName(string name);
    Task<CampiCourseEntity> FindByCampiAndCourseId(long campiId, long courseId);
}
