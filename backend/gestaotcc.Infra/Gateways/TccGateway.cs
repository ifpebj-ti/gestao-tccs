using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
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
                .ThenInclude(x => x.User)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Documents)
                .ThenInclude(x => x.Signatures)
            .Include(x => x.Documents)
                .ThenInclude(x => x.DocumentType)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    // verificar
    public async Task<TccEntity?> FindTccInformations(long id)
    {
        return await context.Tccs
            .Include(x => x.TccCancellation)
            .Include(x => x.TccSchedule)
            .Include(x => x.TccInvites)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.Profile)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.CampiCourse)
                        .ThenInclude(x => x!.Course)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.CampiCourse)
                        .ThenInclude(x => x!.Campi)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TccEntity?> FindTccScheduling(long id)
    {
        return await context.Tccs
            .Include(x => x.TccSchedule)
            .Include(x => x.UserTccs)
            .ThenInclude(x => x.User)
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

    public async Task<TccEntity?> FindTccWorkflow(long? tccId, long userId)
    {
        var query = context.Tccs
            .Include(x => x.TccInvites)
            .Include(x => x.UserTccs)
                .ThenInclude(ut => ut.User)
                    .ThenInclude(u => u.Profile)
                        .ThenInclude(p => p.DocumentTypes)
            .Include(x => x.UserTccs)
                .ThenInclude(ut => ut.Profile)
            .Include(x => x.Documents)
                .ThenInclude(d => d.DocumentType)
            .Include(x => x.Documents)
                .ThenInclude(d => d.Signatures)
            .AsQueryable();

        if (tccId.HasValue)
        {
            query = query.Where(x => x.Id == tccId.Value);
        }
        else
        {
            query = query.Where(x => x.UserTccs.Any(ut => ut.UserId == userId));
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<List<TccEntity>> FindAllTccByFilter(TccFilterDTO tccFilter, long campiId)
    {
        var query = context.Tccs.AsQueryable();
        
        if(campiId != 0)
            query = query.Where(x => x.UserTccs.Any(ut => ut.User.CampiCourse!.Id == campiId));
        
        if(!string.IsNullOrEmpty(tccFilter.UserId.ToString()))
            query = query.Where(x => x.UserTccs.Any(y => y.UserId == tccFilter.UserId));
        
        if(!string.IsNullOrEmpty(tccFilter.StatusTcc))
            query = query.Where(x => x.Status.ToLower() == tccFilter.StatusTcc.ToLower());
        
        return await query
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.Profile)
            .Include(x => x.Documents)
                .ThenInclude(x => x.DocumentType)
            .Include(x => x.Documents)
                .ThenInclude(x => x.Signatures)
            .ToListAsync();
    }
}