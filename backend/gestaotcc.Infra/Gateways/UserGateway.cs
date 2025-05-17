using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public async Task Save(UserEntity user)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }
}
