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
using gestaotcc.Domain.Utils;

namespace gestaotcc.Infra.Gateways;
public class UserGateway(AppDbContext context) : IUserGateway
{
    public Task<UserEntity?> FindProposalParticipantByEmail(string email) => context.Users
        .Include(x => x.Profile).FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());

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
                .ThenInclude(x => x.Campi)
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x.Course)
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

    // Atualização própria altera somente esta coluna, preservando edições administrativas concorrentes.
    public async Task<bool> UpdatePhone(long userId, string phone)
    {
        var affected = await context.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.Phone, phone));
        return affected == 1;
    }

    public async Task<bool> UpdateStudentProfile(long userId, UpdateStudentProfileDTO data, string shift)
    {
        var affected = await context.Users
            .Where(user => user.Id == userId && user.Profile.Any(profile => profile.Role == "STUDENT"))
            .ExecuteUpdateAsync(update => update
                .SetProperty(user => user.Name, data.Name)
                .SetProperty(user => user.Phone, data.Phone)
                .SetProperty(user => user.CPF, data.Cpf)
                .SetProperty(user => user.Registration, data.Registration)
                .SetProperty(user => user.UserClass, data.UserClass)
                .SetProperty(user => user.Shift, shift));
        return affected == 1;
    }

    public async Task<bool> UpdateOwnPassword(long userId, string previousHash, string newHash)
    {
        var affected = await context.Users
            .Where(user => user.Id == userId && user.Password == previousHash && user.Status == "ACTIVE")
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.Password, newHash));
        return affected == 1;
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
    public async Task<List<UserEntity>> FindAllByFilter(UserFilterDTO filter, long campiCourseId)
    {
        var query = context.Users.AsQueryable();
        query = query.Where(x => x.CampiCourse!.Id == campiCourseId);

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

    public async Task<PagedResult<List<UserEntity>>> FindAll(int pageNumber, int pageSize)
    {
        var totalRecords = await context.Users.CountAsync();

        var users = await context.Users
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x.Campi)
            .Include(x => x.CampiCourse)
                .ThenInclude(x => x.Course)
            .Include(x => x.Profile)
            .OrderBy(x => x.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<List<UserEntity>>
        {
            TotalRecords = totalRecords,
            Pages = users
        };
    }
}
