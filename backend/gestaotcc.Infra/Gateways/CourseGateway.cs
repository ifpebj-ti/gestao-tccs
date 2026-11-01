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
    public async Task<CampiEntity> FindCampiById(long id)
    {
        var campi = await context.Campi.FirstOrDefaultAsync(c => c.Id == id);
        if (campi is null)
            throw new Exception("Campus não encontrado");
        return campi;
    }

    public async Task<CampiEntity> InsertCampi(CampiEntity campi)
    {
        await context.Campi.AddAsync(campi);
        await context.SaveChangesAsync();
        return campi;
    }

    public async Task<CampiEntity> UpdateCampi(CampiEntity campi)
    {
        context.Campi.Update(campi);
        await context.SaveChangesAsync();
        return campi;
    }

    public async Task DeleteCampi(long id)
    {
        var campi = await FindCampiById(id);
        context.Campi.Remove(campi);
        await context.SaveChangesAsync();
    }

    public async Task<CourseEntity> FindCourseById(long id)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == id);
        if (course is null)
            throw new Exception("Curso não encontrado");
        return course;
    }

    public async Task<CourseEntity> InsertCourse(CourseEntity course, long campiId)
    {
        await context.Courses.AddAsync(course);
        await context.SaveChangesAsync();
        var campiCourse = new gestaotcc.Domain.Entities.CampiCourse.CampiCourseEntity(0, null!, null!, null!, campiId, course.Id);
        await context.CampiCourses.AddAsync(campiCourse);
        await context.SaveChangesAsync();
        return course;
    }

    public async Task<CourseEntity> UpdateCourse(CourseEntity course)
    {
        context.Courses.Update(course);
        await context.SaveChangesAsync();
        return course;
    }

    public async Task DeleteCourse(long id)
    {
        var course = await FindCourseById(id);
        context.Courses.Remove(course);
        await context.SaveChangesAsync();
    }
}
