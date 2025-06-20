using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
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
            .WithStatus(StatusTccType.IN_PROGRESS.ToString())
            .WithStep(invites.Count > 0 ? StepTccType.PROPOSAL_REGISTRATION.ToString() : StepTccType.START_AND_ORGANIZATION.ToString())
            .WithTccInvites(invites)
            .WithCreationDate(DateTime.UtcNow)
            .Build();

        var studentUsers =
            users.Where(user => user.Profile.Any(profile => profile.Role == RoleType.STUDENT.ToString()));
        
        tcc.UserTccs = users.Select(u => new UserTccEntityBuilder()
                .WithTcc(tcc)
                .WithUser(u)
                .WithProfile(
                    studentUsers.Contains(u) 
                        ? u.Profile.FirstOrDefault(profile => profile.Role == RoleType.STUDENT.ToString())! 
                        : u.Profile.FirstOrDefault(profile => profile.Role == RoleType.ADVISOR.ToString())!
                        )
                .WithBindingDate(DateTime.UtcNow)
            .Build())
            .ToList();

        return tcc;
    }

    public static TccEntity UpdateUsersTcc(TccEntity tcc, UserEntity user, ProfileEntity profile)
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

    public static FindTccWorkflowDTO CreateFindTccWorkflowDTO(TccEntity tcc, List<DocumentTypeEntity> allDocumentsType)
    {

        var stepSignatureOrderMap = new Dictionary<StepTccType, int>
        {
            { StepTccType.PROPOSAL_REGISTRATION, 1 },
            { StepTccType.START_AND_ORGANIZATION, 2 },
            { StepTccType.DEVELOPMENT_AND_MONITORING, 3 },
            { StepTccType.PREPARATION_FOR_PRESENTATION, 4 },
            { StepTccType.PRESENTATION_AND_EVALUATION, 5 },
            { StepTccType.FINALIZATION_AND_PUBLICATION, 6 }
        };

        FindTccWorkflowDTO workflow = null!;
        List<FindTccWorkflowSignatureDetailsDTO> details;
        if (Enum.TryParse<StepTccType>(tcc.Step.ToString(), out var tccStep)
            && stepSignatureOrderMap.TryGetValue(tccStep, out var signatureOrder))
        {
            var users = tcc.UserTccs
                .Where(x => x.User.Profile
                    .Any(x => x.DocumentTypes
                        .Any(x => x.SignatureOrder == signatureOrder)))
                .Select(u => u.User)
                .ToList();

            // Em caso de cadastro de estudante
            if (tccStep == StepTccType.PROPOSAL_REGISTRATION)
            {
                details = tcc.TccInvites.Select(x =>
                    {
                        var user = users.FirstOrDefault(user => x.Email == user.Email);
                        var acceptedInvitation = user is not null;
                        return new FindTccWorkflowSignatureDetailsDTO(
                            user.Id, 
                            RoleType.STUDENT.ToString(), 
                            x.Email,
                            acceptedInvitation);
                    })
                    .OrderBy(x => x.UserName)
                    .ToList();
                
                workflow = new FindTccWorkflowDTO(tcc.Id, signatureOrder, tcc.TccInvites
                    .Select(x => new FindTccWorkflowSignatureDTO(1, "CADASTRO DE ESTUDANTES", details))
                    .ToList());

                return workflow;
            }

            var documentsType = tcc.Documents
                .Where(x => x.DocumentType.SignatureOrder == signatureOrder)
                .Select(x => x.DocumentType)
                .Distinct()
                .ToList();

            // Em caso de não ter nenhum documento associado a um tcc
            if (!documentsType.Any())
            {
                details = users
                    .Select(user =>
                    {
                        var userProfile = user.Profile.MinBy(p => p.Id).Role;
                        return new FindTccWorkflowSignatureDetailsDTO(user.Id, userProfile, user.Name, false);
                    })
                    .OrderBy(x => x.UserName)
                    .ToList();
                workflow = new FindTccWorkflowDTO(tcc.Id, signatureOrder, allDocumentsType
                    .Where(x => x.SignatureOrder == signatureOrder)
                    .Select(x => new FindTccWorkflowSignatureDTO(x.Id, x.Name, details))
                    .OrderBy(x => x.DocumentId)
                    .ToList());
                
                return workflow;
            }
            
            var workflowSignatures = allDocumentsType
                .Where(x => x.SignatureOrder == signatureOrder)
                .Select(documentType =>
                {
                    List<FindTccWorkflowSignatureDetailsDTO> details;

                    // Verifica se esse documento está associado ao TCC
                    var isDocumentInTcc = documentsType.Any(dt => dt.Id == documentType.Id);

                    if (isDocumentInTcc && documentType.Documents != null && documentType.Documents.Any())
                    {
                        details = documentType.Documents
                            .SelectMany(document => document.Signatures)
                            .Select(signature =>
                            {
                                var userProfile = signature.User.Profile.MinBy(p => p.Id).Role;
                                var isSigned = users.Contains(signature.User);
                                return new FindTccWorkflowSignatureDetailsDTO(
                                    signature.User.Id,
                                    userProfile,
                                    signature.User.Name,
                                    isSigned
                                );
                            })
                            .ToList();

                        // Para cada usuário que ainda não está no details, adiciona com IsSigned = false
                        foreach (var user in users)
                        {
                            var userProfile = user.Profile.MinBy(p => p.Id).Role;
                            var alreadyExists = details.Any(d => d.UserId == user.Id);

                            if (!alreadyExists)
                            {
                                details.Add(new FindTccWorkflowSignatureDetailsDTO(
                                    user.Id,
                                    userProfile,
                                    user.Name,
                                    false
                                ));
                            }
                        }
                    }
                    else
                    {
                        // Se o documento NÃO está associado ao TCC, todos os users com IsSigned = false
                        details = users
                            .Select(user =>
                            {
                                var userProfile = user.Profile.MinBy(p => p.Id).Role;
                                return new FindTccWorkflowSignatureDetailsDTO(
                                    user.Id,
                                    userProfile,
                                    user.Name,
                                    false
                                );
                            })
                            .ToList();
                    }

                    var orderedDetails = details.OrderBy(x => x.UserName).ToList();

                    return new FindTccWorkflowSignatureDTO(
                        documentType.Id,
                        documentType.Name,
                        orderedDetails
                    );
                })
                .OrderBy(x => x.DocumentId)
                .ToList();

            
            workflow = new FindTccWorkflowDTO(
                tcc.Id,
                (long)tccStep,
                workflowSignatures
            );
        }

        return workflow;
    }
}