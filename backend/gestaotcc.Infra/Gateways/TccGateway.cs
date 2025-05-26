using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Gateways;

public class TccGateway(AppDbContext context) : ITccGateway
{
    public async Task Save(TccEntity tcc)
    {
        context.Tccs.Add(tcc);
        await context.SaveChangesAsync();
    }

    public async Task Update(TccEntity tcc)
    {
        context.Tccs.Update(tcc);
        await context.SaveChangesAsync();
    }

    public async Task<List<TccInviteEntity>> FindAllInviteTcc()
    {
        return await context.TccInvites.Where(x => x.IsValidCode).ToListAsync();
    }

    public async Task<TccInviteEntity?> FindInviteTccByEmail(string email)
    {
        return await context.TccInvites.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task UpdateTccInvite(TccInviteEntity tccInvite)
    {
        context.Update(tccInvite);
        await context.SaveChangesAsync();
    }

    public async Task<TccEntity?> FindTccById(long id)
    {
        return await context.Tccs
            .Include(x => x.TccInvites)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}