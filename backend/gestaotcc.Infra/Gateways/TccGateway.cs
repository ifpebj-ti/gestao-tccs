using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccCancellation;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Enums;
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

    public async Task UpdateSemesterId(long tccId, long semesterId)
    {
        await context.Tccs
            .Where(t => t.Id == tccId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.SemesterId, semesterId));
    }

    public async Task<List<TccInviteEntity>> FindAllInviteTcc()
    {
        return await context.TccInvites
            .Where(x =>
                x.IsValidCode &&
                (x.Tcc == null || (x.Tcc.Status != StatusTccType.CANCELED.ToString()
                && x.Tcc.Status != StatusTccType.COMPLETED.ToString())))
            .ToListAsync();
    }

    public async Task<TccInviteEntity?> FindInviteTccByEmail(string email)
    {
        return await context.TccInvites.FirstOrDefaultAsync(x => x.Email == email);
    }

    public async Task SaveTccInvite(TccInviteEntity tccInvite)
    {
        context.TccInvites.Add(tccInvite);
        await context.SaveChangesAsync();
    }

    public async Task UpdateTccInvite(TccInviteEntity tccInvite)
    {
        context.Update(tccInvite);
        await context.SaveChangesAsync();
    }

    public async Task<TccEntity?> FindTccById(long id)
    {
        return await context.Tccs
            .Include(x => x.ParticipantInvitations)
            .Include(x => x.TccInvites)
            .Include(x => x.TccCancellation)
            .Include(x => x.TccSchedule)
            .Include(x => x.BankingMembers)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
                    .ThenInclude(u => u.CampiCourse)
                        .ThenInclude(cc => cc!.Course)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
                    .ThenInclude(u => u.CampiCourse)
                        .ThenInclude(cc => cc!.Campi)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
                    .ThenInclude(u => u.Profile)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.Profile)
            .Include(x => x.Documents)
                .ThenInclude(x => x.Signatures)
            .Include(x => x.Documents)
                .ThenInclude(x => x.DocumentType)
            .Include(x => x.AdvisorChanges)
            .Include(x => x.Semester)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<TccEntity>> FindAllTccByFilter(TccFilterDTO tccFilter, long campiCourseId)
    {
        var query = context.Tccs.AsQueryable();

        if(campiCourseId != 0)
            query = query.Where(x => x.UserTccs.Any(ut => ut.User.CampiCourse!.Id == campiCourseId));

        if(tccFilter.UserId.HasValue)
            query = query.Where(x => x.UserTccs.Any(y => y.UserId == tccFilter.UserId));

        if(!string.IsNullOrEmpty(tccFilter.StatusTcc))
            query = query.Where(x => x.Status.ToLower() == tccFilter.StatusTcc.ToLower());

        if (tccFilter.SemesterId.HasValue)
            query = query.Where(x => x.SemesterId == tccFilter.SemesterId.Value);

        return await query
            .Include(x => x.Semester)
            .Include(x => x.BankingMembers)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
                    .ThenInclude(x => x.Profile)
            .Include(x => x.Documents)
                .ThenInclude(x => x.DocumentType)
            .Include(x => x.Documents)
                .ThenInclude(x => x.Signatures)
            .ToListAsync();
    }

    public async Task<gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity?> FindBankingMemberByToken(string token)
    {
        return await context.TccBankingMembers.FirstOrDefaultAsync(x => x.AccessToken == token);
    }

    public async Task UpdateBankingMember(gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity member)
    {
        context.TccBankingMembers.Update(member);
        await context.SaveChangesAsync();

    }
    public async Task<TccEntity?> FindTccInformations(long id)
    {
        return await context.Tccs
            .Include(x => x.ParticipantInvitations)
            .Include(x => x.TccCancellation)
            .Include(x => x.TccSchedule)
            .Include(x => x.TccInvites)
            .Include(x => x.BankingMembers)
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
            .Include(x => x.Semester)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<TccEntity?> FindTccScheduling(long id)
    {
        return await context.Tccs
            .Include(x => x.TccSchedule)
            .Include(x => x.BankingMembers)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.User)
            .Include(x => x.UserTccs)
                .ThenInclude(x => x.Profile)
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
            .Include(x => x.UserTccs)
                .ThenInclude(ut => ut.User)
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

    public async Task<gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity?> FindPendingAdvisorChange(long tccId)
    {
        return await context.TccAdvisorChanges
            .Include(x => x.RequesterUser)
            .Include(x => x.CurrentAdvisor)
            .Include(x => x.SuggestedAdvisor)
            .Include(x => x.Document)
                .ThenInclude(d => d!.Signatures)
            .Where(x => x.TccId == tccId && (x.Status == "PENDING_SIGNATURES" || x.Status == "PENDING_COORDINATION"))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task SaveAdvisorChange(gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity advisorChange)
    {
        await context.TccAdvisorChanges.AddAsync(advisorChange);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAdvisorChange(gestaotcc.Domain.Entities.TccAdvisorChange.TccAdvisorChangeEntity advisorChange)
    {
        context.TccAdvisorChanges.Update(advisorChange);
        await context.SaveChangesAsync();
    }
}
