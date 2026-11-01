using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Errors;
using gestaotcc.Infra.Database;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.Infra.Gateways;

public class ProposalGateway(AppDbContext context) : IProposalGateway
{
    // A database transaction lock covers validation AND persistence across API instances.
    // Read committed ensures a request waiting on the lock sees the previous commit.
    public async Task<ResultPattern<T>> Atomic<T>(Func<Task<ResultPattern<T>>> action)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(20261013, 1)");
        var result = await action();
        if (result.IsSuccess) await transaction.CommitAsync();
        else { await transaction.RollbackAsync(); context.ChangeTracker.Clear(); }
        return result;
    }

    public Task<List<ProposalParticipantEntity>> PendingFor(long userId) =>
        context.ProposalParticipants.AsNoTrackingWithIdentityResolution().Include(x => x.Tcc).ThenInclude(x => x.UserTccs).ThenInclude(x => x.User)
            .Include(x => x.Tcc).ThenInclude(x => x.UserTccs).ThenInclude(x => x.Profile)
            .Include(x => x.Tcc).ThenInclude(x => x.ParticipantInvitations)
            .Where(x => (x.UserId == null || x.UserId == userId) && context.Users.Any(u => u.Id == userId && u.Email.ToLower() == x.Email)
                && x.Status == "PENDING" && (x.Tcc.Status == "AWAITING_PARTICIPANTS" || x.Tcc.Status == "PENDING_APPROVAL")).ToListAsync();

    public Task<List<ProposalParticipantEntity>> DeclinedFor(long userId) =>
        context.ProposalParticipants.AsNoTracking().Include(x => x.Tcc)
            .Where(x => x.Status == "DECLINED" && x.NotificationDismissedAt == null &&
                x.Tcc.UserTccs.Any(u => u.UserId == userId && u.Profile.Role == "STUDENT" &&
                    !x.Tcc.ParticipantInvitations.Any(i => i.UserId == userId)))
            .OrderByDescending(x => x.RespondedAt).ToListAsync();

    public Task<ProposalParticipantEntity?> Find(long invitationId) =>
        context.ProposalParticipants.FirstOrDefaultAsync(x => x.Id == invitationId);
}
