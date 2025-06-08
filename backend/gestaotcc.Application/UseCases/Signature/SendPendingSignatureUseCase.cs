using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Tcc;
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
        var documentTypes = await documentTypeGateway.FindAll();

        var usersToNotify = new Dictionary<string, SendPendingSignatureDTO>();

        foreach (var tcc in tccs)
        {
            if (!Enum.TryParse<StepTccType>(tcc.Step, out var tccStep)) continue;
            if (tccStep == StepTccType.PROPOSAL_REGISTRATION) continue;

            ProcessTcc(tcc, tccStep, documentTypes, usersToNotify);
        }

        await SendNotifications(usersToNotify);

        return ResultPattern<string>.SuccessResult();
    }

    private void ProcessTcc(
        TccEntity tcc,
        StepTccType tccStep,
        List<DocumentTypeEntity> allDocumentTypes,
        Dictionary<string, SendPendingSignatureDTO> usersToNotify)
    {
        var currentStepOrder = _stepSignatureOrderMap[tccStep];

        var expectedDocTypes = allDocumentTypes
            .Where(dt => dt.SignatureOrder == currentStepOrder)
            .ToList();

        var documents = tcc.Documents
            .Where(d => d.DocumentType.SignatureOrder == currentStepOrder)
            .ToList();

        var orderedUsers = tcc.UserTccs
            .OrderBy(ut => ut.User.Profile.MinBy(p => p.Id).Id)
            .ThenBy(ut => ut.Id)
            .ToList();

        if (!documents.Any())
        {
            var firstUser = orderedUsers.First().User;
            AddOrUpdateUser(usersToNotify, firstUser.Email, firstUser.Name, tcc.Title!, expectedDocTypes.Select(dt => dt.Name));
            return;
        }

        foreach (var docType in expectedDocTypes)
        {
            HandleDocumentType(docType, documents, orderedUsers, tcc.Title!, usersToNotify);
        }
    }

    private void HandleDocumentType(
        DocumentTypeEntity docType,
        List<DocumentEntity> documents,
        List<UserTccEntity> orderedUsers,
        string tccTitle,
        Dictionary<string, SendPendingSignatureDTO> usersToNotify)
    {
        var document = documents.FirstOrDefault(d => d.DocumentType.Id == docType.Id);

        if (document == null)
        {
            var firstUser = orderedUsers.First().User;
            AddOrUpdateUser(usersToNotify, firstUser.Email, firstUser.Name, tccTitle, new List<string> { docType.Name });
            return;
        }

        foreach (var userTcc in orderedUsers)
        {
            var user = userTcc.User;

            if (document.Signatures.Any(s => s.UserId == user.Id))
                continue;

            var index = orderedUsers.IndexOf(userTcc);
            var allPreviousSigned = orderedUsers.Take(index).All(prev =>
                document.Signatures.Any(sig => sig.UserId == prev.User.Id));

            if (allPreviousSigned)
            {
                AddOrUpdateUser(usersToNotify, user.Email, user.Name, tccTitle, new List<string> { docType.Name });
            }

            break;
        }
    }

    private async Task SendNotifications(Dictionary<string, SendPendingSignatureDTO> usersToNotify)
    {
        foreach (var dto in usersToNotify.Values)
        {
            var emailDto = EmailFactory.CreateSendEmailDTO(dto);
            await emailGateway.Send(emailDto);
        }
    }

    private void AddOrUpdateUser(
        Dictionary<string, SendPendingSignatureDTO> map,
        string email,
        string name,
        string tccTitle,
        IEnumerable<string> docNames)
    {
        if (map.ContainsKey(email))
        {
            var existing = map[email];
            var updatedList = existing.DocumentNames.Union(docNames).Distinct().ToList();
            map[email] = new SendPendingSignatureDTO(email, name, updatedList, tccTitle);
        }
        else
        {
            map[email] = new SendPendingSignatureDTO(email, name, docNames.ToList(), tccTitle);
        }
    }
}
