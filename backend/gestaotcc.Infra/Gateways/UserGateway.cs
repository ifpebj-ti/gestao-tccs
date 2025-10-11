using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using gestaotcc.Domain.Dtos.User;

namespace gestaotcc.Infra.Gateways;
public class UserGateway(AppDbContext context) : IUserGateway
{
    //verificar
    public async Task<UserEntity?> FindByEmail(string email)
    {
        return await context.Users
            .Include(x => x.Profile)
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x!.Campi)
            .Include(x => x.AccessCode)
            .FirstOrDefaultAsync(x => x.Email == email);
    }

    //verificar
    public async Task<UserEntity?> FindById(long id)
    {
        return await context.Users
            .Include(x => x.Profile)
            .Include(x => x.CampiCourse)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task Save(UserEntity user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task Update(UserEntity user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task<List<UserEntity>> FindAllByEmail(List<string> emails)
    {
        return await context.Users
            .Include(x => x.Profile)
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x!.Campi)
            .Where(x => emails.Contains(x.Email))
            .ToListAsync();
    }

    //verificar
    public async Task<List<UserEntity>> FindAllByFilter(UserFilterDTO filter, long campiId)
    {
        var query = context.Users.AsQueryable();
        query = query.Where(x => x.CampiCourse!.Id == campiId);

        if (!string.IsNullOrEmpty(filter.Name))
            query = query.Where(u => u.Name.Contains(filter.Name));

        if (!string.IsNullOrEmpty(filter.Email))
            query = query.Where(u => u.Email.Contains(filter.Email));
        
        if (!string.IsNullOrEmpty(filter.Profile))
            query = query.Where(u => u.Profile.Any(x => x.Role == filter.Profile));

        if (!string.IsNullOrEmpty(filter.Registration))
            query = query.Where(u => u.Registration.Contains(filter.Registration));


        return await query
            .Include(x => x.Profile)
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x!.Course)
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x!.Campi)
            .ToListAsync();
    }
}
