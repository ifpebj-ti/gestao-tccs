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
    public async Task<UserEntity?> FindByEmail(string email)
    {
        return await context.Users
            .Include(x => x.Profile)
            .Include(x => x.Course)
            .Include(x => x.AccessCode)
            .FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task<UserEntity?> FindById(long id)
    {
        return await context.Users
            .Include(x => x.Profile)
            .Include(x => x.Course)
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
        return await context.Users.Where(x => emails.Contains(x.Email)).ToListAsync();
    }

    public async Task<List<UserEntity>> FindAllByFilter(UserFilterDTO filter)
    {
        var query = context.Users.AsQueryable();

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
            .Include(x => x.Course)
            .ToListAsync();
    }
}
