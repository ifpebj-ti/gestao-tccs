using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Gateways;
public class ProfileGateway(AppDbContext context): IProfileGateway
{
    public async Task<List<ProfileEntity>> FindByRole(List<string> role)
    {
        var profiles = await context.Profiles
            .Where(p => role.Contains(p.Role))
            .ToListAsync();
        return profiles;
    }

    public async Task<List<ProfileEntity>> FindAll()
    {
        return await context.Profiles.ToListAsync();
    }
}
