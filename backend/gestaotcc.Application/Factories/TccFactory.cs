using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;

namespace gestaotcc.Application.Factories;

public class TccFactory
{
    public static TccEntity CreateTcc(CreateTccDTO data, List<UserEntity> users, List<string> userInvites)
    {
        var random = new Random();

        var invites = userInvites.Select(u =>
        {
            var chars = "ABCDEFGHIJKLMONPQRSTUVWXYZ123456789";

            var code = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return TccInviteFactory.CreateTccInvite(u, code);
        }).ToList();

        var tcc = new TccEntityBuilder()
            .WithTitle(data.Title)
            .WithSummary(data.Summary)
            .WithStatus(invites.Count > 0 ? StatusTccType.PROPOSAL_REGISTRATION.ToString() : StatusTccType.START_AND_ORGANIZATION.ToString())
            .WithTccInvites(invites)
            .WithCreationDate(DateTime.UtcNow)
            .Build();

        tcc.UserTccs = users.Select(u => new UserTccEntityBuilder()
                .WithTcc(tcc)
                .WithUser(u)
                .WithBindingDate(DateTime.UtcNow)
            .Build())
            .ToList();

        return tcc;
    }

    public static TccEntity UpdateUsersTcc(TccEntity tcc, UserEntity user)
    {
        var alreadyAdded = tcc.UserTccs.Any(ut => ut.User.Id == user.Id);
        
        if (!alreadyAdded)
        {
            tcc.UserTccs.Add(new UserTccEntityBuilder()
                .WithUser(user)
                .WithTcc(tcc)
                .WithBindingDate(DateTime.UtcNow)
                .Build());
        }

        return tcc;
    }
}