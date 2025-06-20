using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class FindAllPendingSignaturesUseCase(
    ITccGateway tccGateway,
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

    public async Task<ResultPattern<List<FindAllPendingSignatureDTO>>> Execute(long? userId)
    {
        var tccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(userId, StatusTccType.IN_PROGRESS.ToString()));
        if (!tccs.Any())
            return ResultPattern<List<FindAllPendingSignatureDTO>>.SuccessResult(new());

        var documentTypes = await documentTypeGateway.FindAll();
        var results = new List<FindAllPendingSignatureDTO>();

        foreach (var tcc in tccs)
        {
            if (!Enum.TryParse<StepTccType>(tcc.Step, out var tccStep)) continue;
            if (!_stepSignatureOrderMap.TryGetValue(tccStep, out var currentOrder)) continue;
            if (tccStep == StepTccType.PROPOSAL_REGISTRATION) continue;

            var userTccs = tcc.UserTccs.ToList();
            var studentNames = GetStudentNames(tcc, userTccs);

            var expectedDocTypes = GetExpectedDocumentTypes(documentTypes, userTccs, currentOrder);
            var documentsInStep = tcc.Documents.Where(d => d.DocumentType.SignatureOrder == currentOrder).ToList();
            var orderedUsers = userTccs.OrderBy(ut => ut.Profile.Id).ThenBy(ut => ut.Id).ToList();

            var pendingDetails = new List<FindAllPendingSignatureDetailsDTO>();

            foreach (var docType in expectedDocTypes)
            {
                var detail = HandleSignatureWorkflow(docType, documentsInStep, orderedUsers);
                if (detail is not null)
                    pendingDetails.Add(detail);
            }
            
            // Filtrando apenas as assinaturas pendentes do usuário
            if (userId.HasValue)
            {
                pendingDetails = pendingDetails
                    .Where(pd => pd.UserDetails.Any(u => u.UserId == userId.Value))
                    .ToList();
            }

            if (pendingDetails.Any())
            {
                results.Add(new FindAllPendingSignatureDTO(
                    tcc.Id,
                    studentNames,
                    pendingDetails));
            }
        }

        return ResultPattern<List<FindAllPendingSignatureDTO>>.SuccessResult(results);
    }


    private static List<string> GetStudentNames(Domain.Entities.Tcc.TccEntity tcc, List<UserTccEntity> userTccs)
    {
        var studentNames = userTccs
            .Where(ut => ut.Profile.Role == RoleType.STUDENT.ToString())
            .Select(ut => ut.User.Name)
            .ToList();

        return studentNames.Any() ? studentNames : tcc.TccInvites.Select(x => x.Email).ToList();
    }

    private static List<DocumentTypeEntity> GetExpectedDocumentTypes(
        List<DocumentTypeEntity> allDocTypes,
        List<UserTccEntity> userTccs,
        int stepOrder)
    {
        return allDocTypes
            .Where(dt => dt.SignatureOrder == stepOrder &&
                         dt.Profiles.Any(p => userTccs.Any(u => u.Profile.Id == p.Id)))
            .ToList();
    }

    private FindAllPendingSignatureDetailsDTO? HandleSignatureWorkflow(
        DocumentTypeEntity docType,
        List<DocumentEntity> stepDocuments,
        List<UserTccEntity> orderedUsers)
    {
        var document = stepDocuments.FirstOrDefault(d => d.DocumentType.Id == docType.Id);

        if (document == null)
        {
            var user = orderedUsers.First().User;
            return new FindAllPendingSignatureDetailsDTO(
                docType.Id,
                docType.Name,
                new List<FindAllPendignSignatureUserDetailsDTO>
                {
                    new(user.Id, user.Name, orderedUsers.First().Profile.Role)
                });
        }

        for (int i = 0; i < orderedUsers.Count; i++)
        {
            var user = orderedUsers[i].User;

            if (document.Signatures.Any(s => s.UserId == user.Id)) continue;

            var allPreviousSigned = orderedUsers.Take(i)
                .All(prev => document.Signatures.Any(sig => sig.UserId == prev.User.Id));

            if (allPreviousSigned)
            {
                return new FindAllPendingSignatureDetailsDTO(
                    docType.Id,
                    docType.Name,
                    new List<FindAllPendignSignatureUserDetailsDTO>
                    {
                        new(user.Id, user.Name, orderedUsers[i].Profile.Role)
                    });
            }

            break;
        }

        return null;
    }
}

