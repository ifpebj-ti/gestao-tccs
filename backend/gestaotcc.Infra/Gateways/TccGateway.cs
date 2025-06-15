using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
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
            .Include(x => x.TccCancellation)
            .Include(x => x.UserTccs)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TccEntity?> FindTccCancellation(long id)
    {
        return await context.Tccs
            .Include(x => x.TccCancellation)
            .Include(x => x.UserTccs)
            .ThenInclude(x => x.User)
            .ThenInclude(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TccEntity?> FindTccWorkflow(long tccId, long userId)
    {
        return await context.Tccs
            .Include(x => x.TccInvites)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.Profile)
                        .ThenInclude(x => x.DocumentTypes)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Documents)
                .ThenInclude(x => x.DocumentType)
                    .ThenInclude(dt => dt.Documents)
                        .ThenInclude(doc => doc.Signatures)
            .FirstOrDefaultAsync(x => x.Id == tccId || x.UserTccs.Any(x => x.UserId == userId));
    }

    public async Task<List<TccEntity>> FindAllTccByFilter(string filter)
    {
        bool isUserId = long.TryParse(filter, out long userId);

        return await context.Tccs
            .Where(x => x.Status.ToLower() == filter.ToLower() || 
                        (isUserId && x.UserTccs.Any(u => u.UserId == userId)))
            .Include(x => x.UserTccs).ThenInclude(x => x.User).ThenInclude(x => x.Profile)
            .Include(x => x.Documents).ThenInclude(x => x.DocumentType)
            .Include(x => x.Documents).ThenInclude(x => x.Signatures)
            .ToListAsync();
    }
}