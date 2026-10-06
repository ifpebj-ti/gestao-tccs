using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;

namespace gestaotcc.Application.Gateways;
public interface ICourseGateway
{
    Task<CourseEntity> FindByName(string name);
    Task<CampiCourseEntity> FindByCampiAndCourseId(long campiId, long courseId);
    Task<List<CampiEntity>> FindAllCampis();
    Task<List<CourseEntity>> FindAllCoursesByCampiCourseId(long campiCourseId);
    Task<CampiEntity> FindCampiById(long id);
    Task<CampiEntity> InsertCampi(CampiEntity campi);
    Task<CampiEntity> UpdateCampi(CampiEntity campi);
    Task DeleteCampi(long id);

    Task<CourseEntity> FindCourseById(long id);
    Task<CourseEntity> InsertCourse(CourseEntity course, long campiId);
    Task<CourseEntity> UpdateCourse(CourseEntity course);
    Task DeleteCourse(long id);
}
