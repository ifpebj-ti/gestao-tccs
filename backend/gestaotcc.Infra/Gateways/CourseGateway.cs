using gestaotcc.Application.Gateways;
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
}
