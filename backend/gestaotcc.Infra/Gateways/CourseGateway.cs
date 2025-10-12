using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Campi;
using gestaotcc.Domain.Entities.CampiCourse;
using gestaotcc.Domain.Entities.Course;
using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Gateways;
public class CourseGateway(AppDbContext context) : ICourseGateway
{
    public async Task<CourseEntity> FindByName(string name)
    {
        var course = await context.Courses
            .FirstOrDefaultAsync(x => x.Name == name);

        if (course is null)
            throw new Exception("Curso não encontrado");

        return course;
    }

    public async Task<CampiCourseEntity> FindByCampiAndCourseId(long campiId, long courseId)
    {
        return (await context.CampiCourses.FirstOrDefaultAsync(cc => cc.CampiId == campiId && cc.CourseId == courseId))!;
    }

    public async Task<List<CampiEntity>> FindAllCampis()
    {
        return await context.Campi
            .Include(c => c.CampiCourses)
                .ThenInclude(cc => cc.Course)
            .ToListAsync();
    }

    public async Task<List<CourseEntity>> FindAllCoursesByCampiCourseId(long campiCourseId)
    {
        var campiCourse = await context.CampiCourses.FirstOrDefaultAsync(cc => cc.Id == campiCourseId);
        
        return await context.Courses
            .Where(c => c.CampiCourses
                .Any(cc => cc.CampiId == campiCourse!.CampiId))
            .ToListAsync();
    }
}
