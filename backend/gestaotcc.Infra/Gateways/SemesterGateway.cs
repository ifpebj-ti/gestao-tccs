using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Semester;
using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Gateways;

public class SemesterGateway(AppDbContext context) : ISemesterGateway
{
    public async Task<List<SemesterEntity>> FindAll()
    {
        return await context.Semesters.OrderByDescending(s => s.StartDate).ToListAsync();
    }

    public async Task<List<SemesterEntity>> FindActive()
    {
        return await context.Semesters.Where(s => s.IsActive).OrderByDescending(s => s.StartDate).ToListAsync();
    }

    public async Task<SemesterEntity?> FindById(long id)
    {
        return await context.Semesters.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<SemesterEntity> Insert(SemesterEntity semester)
    {
        context.Semesters.Add(semester);
        await context.SaveChangesAsync();
        return semester;
    }

    public async Task<SemesterEntity> Update(SemesterEntity semester)
    {
        context.Semesters.Update(semester);
        await context.SaveChangesAsync();
        return semester;
    }

    public async Task Delete(long id)
    {
        var semester = await FindById(id);
        if (semester != null)
        {
            context.Semesters.Remove(semester);
            await context.SaveChangesAsync();
        }
    }
}
