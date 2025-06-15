using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;
public class SendPendingSignatureUseCase(
    ITccGateway tccGateway,
    IEmailGateway emailGateway,
    IDocumentTypeGateway documentTypeGateway)
{
    private readonly Dictionary<StepTccType, int> _stepSignatureOrderMap = new()
    {
        { StepTccType.PROPOSAL_REGISTRATION, 1 },
        { StepTccType.START_AND_ORGANIZATION, 2 },
        { StepTccType.DEVELOPMENT_AND_MONITORING, 3 },
        { StepTccType.PREPARATION_FOR_PRESENTATION, 4 },
        { StepTccType.PRESENTATION_AND_EVALUATION, 5 },
        { StepTccType.FINALIZATION_AND_PUBLICATION, 6 }
    };

    public async Task<ResultPattern<string>> Execute()
    {
        var tccs = await tccGateway.FindAllTccByFilter("IN_PROGRESS");
        if (!tccs.Any())
            return ResultPattern<string>.SuccessResult();
        var documentTypes = await documentTypeGateway.FindAll();

        var usersToNotify = new Dictionary<string, SendPendingSignatureDTO>();

        foreach (var tcc in tccs)
        {
            if (!Enum.TryParse<StepTccType>(tcc.Step, out var tccStep)) continue;
            if (tccStep == StepTccType.PROPOSAL_REGISTRATION) continue;

            var currentOrder = _stepSignatureOrderMap[tccStep];
            var userTccs = tcc.UserTccs.ToList();

            var expectedDocTypes = GetExpectedDocumentTypes(documentTypes, userTccs, currentOrder);
            var documentsInStep = tcc.Documents.Where(d => d.DocumentType.SignatureOrder == currentOrder).ToList();
            var orderedUsers = tcc.UserTccs
                .OrderBy(ut => ut.Profile.Id)
                .ThenBy(ut => ut.Id)
                .ToList();

            if (!documentsInStep.Any())
            {
                NotifyUser(usersToNotify, orderedUsers.First().User, tcc.Title!, expectedDocTypes.Select(dt => dt.Name));
                continue;
            }

            foreach (var docType in expectedDocTypes)
            {
                HandleSignatureWorkflow(docType, documentsInStep, orderedUsers, tcc.Title!, usersToNotify);
            }
        }

        await NotifyAllUsers(usersToNotify);
        return ResultPattern<string>.SuccessResult();
    }

    private List<DocumentTypeEntity> GetExpectedDocumentTypes(
        List<DocumentTypeEntity> allDocTypes,
        List<UserTccEntity> userTccs,
        int stepOrder)
    {
        return allDocTypes
            .Where(dt => dt.SignatureOrder == stepOrder &&
                         dt.Profiles.Any(p => userTccs.Any(u => u.Profile.Id == p.Id)))
            .ToList();
    }

    private void HandleSignatureWorkflow(
        DocumentTypeEntity docType,
        List<DocumentEntity> stepDocuments,
        List<UserTccEntity> orderedUsers,
        string tccTitle,
        Dictionary<string, SendPendingSignatureDTO> usersToNotify)
    {
        var document = stepDocuments.FirstOrDefault(d => d.DocumentType.Id == docType.Id);

        if (document == null)
        {
            NotifyUser(usersToNotify, orderedUsers.First().User, tccTitle, new List<string> { docType.Name });
            return;
        }

        for (int i = 0; i < orderedUsers.Count; i++)
        {
            var user = orderedUsers[i].User;

            if (document.Signatures.Any(s => s.UserId == user.Id)) continue;

            var allPreviousSigned = orderedUsers.Take(i)
                .All(prev => document.Signatures.Any(sig => sig.UserId == prev.User.Id));

            if (allPreviousSigned)
            {
                NotifyUser(usersToNotify, user, tccTitle, new List<string> { docType.Name });
            }

            break; // após encontrar o usuário responsável, não verifica os próximos
        }
    }

    private void NotifyUser(
        Dictionary<string, SendPendingSignatureDTO> usersToNotify,
        UserEntity user,
        string tccTitle,
        IEnumerable<string> docNames)
    {
        if (usersToNotify.TryGetValue(user.Email, out var existing))
        {
            var updatedDocs = existing.DocumentNames.Union(docNames).Distinct().ToList();
            usersToNotify[user.Email] = new SendPendingSignatureDTO(user.Email, user.Name, updatedDocs, tccTitle);
        }
        else
        {
            usersToNotify[user.Email] = new SendPendingSignatureDTO(user.Email, user.Name, docNames.ToList(), tccTitle);
        }
    }

    private async Task NotifyAllUsers(Dictionary<string, SendPendingSignatureDTO> usersToNotify)
    {
        foreach (var dto in usersToNotify.Values)
        {
            var emailDto = EmailFactory.CreateSendEmailDTO(dto);
            await emailGateway.Send(emailDto);
        }
    }
}
