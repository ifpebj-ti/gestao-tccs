using gestaotcc.Domain.Entities.Course;

namespace gestaotcc.Application.Gateways;
public interface ICourseGateway
{
    Task<CourseEntity> FindByName(string name);
}
