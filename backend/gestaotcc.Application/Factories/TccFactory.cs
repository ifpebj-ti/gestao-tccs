using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Signature;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;

namespace gestaotcc.Application.Factories;

public class TccFactory
{
    public static TccEntity CreateTcc(CreateTccDTO data, List<UserEntity> users, List<string> userInvites, List<DocumentTypeEntity> documentTypes)
    {
        var random = new Random();

        var invites = userInvites.Select(u =>
        {
            var chars = "ABCDEFGHIJKLMONPQRSTUVWXYZ123456789";
            var code = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return TccInviteFactory.CreateTccInvite(u, code);
        }).ToList();

        var documents = new List<DocumentEntity>();

        foreach (var docType in documentTypes)
        {
            var acceptedRoles = docType.Profiles.Select(p => p.Role).ToHashSet();
            var method = Enum.Parse<MethoSignatureType>(docType.MethodSignature);

            var usersWithAcceptedProfile = users
                .Where(user => user.Profile.Any(p => acceptedRoles.Contains(p.Role)))
                .ToList();

            if (method == MethoSignatureType.ONLY_DOCS)
            {
                if (docType.Profiles.Count > 1)
                {
                    // Cria 1 documento com User = null
                    documents.Add(DocumentFactory.CreateDocument(docType, null));
                }
                else if (docType.Profiles.Count == 1)
                {
                    var profileRole = docType.Profiles.First().Role;
                    var user = usersWithAcceptedProfile.FirstOrDefault(u => u.Profile.Any(p => p.Role == profileRole));
                    if (user != null)
                    {
                        documents.Add(DocumentFactory.CreateDocument(docType, user));
                    }
                }
            }
            else if (method == MethoSignatureType.NOT_ONLY_DOCS)
            {
                foreach (var user in usersWithAcceptedProfile)
                {
                    var profile = user.Profile.First();
                    
                    if(profile.Role != RoleType.STUDENT.ToString() && docType.Profiles.Count > 1)
                        continue;
                    
                    documents.Add(DocumentFactory.CreateDocument(docType, user));
                }
            }
        }

        var tcc = new TccEntityBuilder()
            .WithTitle(data.Title)
            .WithSummary(data.Summary)
            .WithStatus(StatusTccType.IN_PROGRESS.ToString())
            .WithStep(invites.Count > 0 ? StepTccType.PROPOSAL_REGISTRATION.ToString() : StepTccType.START_AND_ORGANIZATION.ToString())
            .WithTccInvites(invites)
            .WithDocuments(documents)
            .WithCreationDate(DateTime.UtcNow)
            .Build();

        var studentUsers = users.Where(user => user.Profile.Any(p => p.Role == RoleType.STUDENT.ToString()));

        tcc.UserTccs = users.Select(user => new UserTccEntityBuilder()
                .WithTcc(tcc)
                .WithUser(user)
                .WithProfile(
                    studentUsers.Contains(user)
                        ? user.Profile.First(p => p.Role == RoleType.STUDENT.ToString())
                        : user.Profile.First(p => p.Role == RoleType.ADVISOR.ToString()))
                .WithBindingDate(DateTime.UtcNow)
            .Build())
            .ToList();

        return tcc;
    }


    public static TccEntity UpdateUsersTccToCreateUser(TccEntity tcc, UserEntity user, ProfileEntity profile)
    {
        var alreadyAdded = tcc.UserTccs.Any(ut => ut.UserId == user.Id);
        
        if (!alreadyAdded)
        {
            var invite = tcc.TccInvites.FirstOrDefault(inv => inv.Email == user.Email);
            invite.IsValidCode = false;
            
            tcc.UserTccs.Add(new UserTccEntityBuilder()
                .WithUser(user)
                .WithTcc(tcc)
                .WithProfile(profile)
                .WithBindingDate(DateTime.UtcNow)
                .Build());
        }

        return tcc;
    }
    
    public static TccEntity UpdateUsersTccToCreateBanking(TccEntity tcc, UserEntity user, ProfileEntity profile)
    {
        var alreadyAdded = tcc.UserTccs.Any(ut => ut.UserId == user.Id);
        
        if (!alreadyAdded)
        {
            tcc.UserTccs.Add(new UserTccEntityBuilder()
                .WithUser(user)
                .WithTcc(tcc)
                .WithProfile(profile)
                .WithBindingDate(DateTime.UtcNow)
                .Build());
        }

        return tcc;
    }

    public static FindAllTccByFilterDTO CreateFindAllTccByStatusOrUserIdDTO(TccEntity tcc)
    {
        var studentRole = RoleType.STUDENT.ToString();

        var studentNames = tcc.UserTccs
            .Where(ut => ut.Profile.Role == studentRole)
            .Select(ut => ut.User.Name)
            .ToList();

        if (!studentNames.Any())
            studentNames = tcc.TccInvites.Select(x => x.Email).ToList();

        return new FindAllTccByFilterDTO(tcc.Id, studentNames);
    }
}