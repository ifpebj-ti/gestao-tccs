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

    public async Task<List<TccInviteEntity>> FindAllInviteTcc()
    {
        return await context.TccInvites.ToListAsync();
    }

    public async Task<TccEntity?> FindTccById(long id)
    {
        return await context.Tccs
            .Include(x => x.TccInvites)
            .FirstOrDefaultAsync(x => x.Id == id);
    }
}