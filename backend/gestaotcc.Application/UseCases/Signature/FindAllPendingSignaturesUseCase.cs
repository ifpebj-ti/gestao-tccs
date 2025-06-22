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
    };

    private readonly List<string> _signatureQueueByProfile = new()
    {
        RoleType.ADVISOR.ToString(),
        RoleType.BANKING.ToString(),
        RoleType.STUDENT.ToString()
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

            var pendingDetails = new List<FindAllPendingSignatureDetailsDTO>();

            foreach (var docType in expectedDocTypes)
            {
                var methodType = Enum.Parse<MethoSignatureType>(docType.MethodSignature);
                var docs = documentsInStep.Where(d => d.DocumentTypeId == docType.Id).ToList();

                // Ordenar UserTccs por perfil e ID, de acordo com a fila
                var orderedUserTccs = userTccs
                    .Where(u => docType.Profiles.Any(p => p.Id == u.Profile.Id))
                    .OrderBy(u => _signatureQueueByProfile.IndexOf(u.Profile.Role))
                    .ThenBy(u => u.Id)
                    .ToList();

                if (methodType == MethoSignatureType.ONLY_DOCS)
                {
                    foreach (var doc in docs)
                    {
                        if (doc.UserId != null)
                        {
                            // Documento individual
                            var user = userTccs.FirstOrDefault(u => u.User.Id == doc.UserId);
                            if (user != null && doc.Signatures.All(s => s.UserId != user.User.Id))
                            {
                                pendingDetails.Add(CreateDetail(doc, docType, user, methodType));
                            }
                        }
                        else
                        {
                            // Documento compartilhado com perfil (fila por perfil)
                            foreach (var currentUser in orderedUserTccs)
                            {
                                var alreadySigned = doc.Signatures.Any(s => s.UserId == currentUser.User.Id);
                                if (alreadySigned) continue;

                                var index = orderedUserTccs.IndexOf(currentUser);
                                var allPreviousSigned = orderedUserTccs.Take(index)
                                    .All(prev => doc.Signatures.Any(s => s.UserId == prev.User.Id));

                                if (allPreviousSigned)
                                {
                                    pendingDetails.Add(CreateDetail(doc, docType, currentUser, methodType));
                                }

                                break; // só um usuário pode assinar por vez
                            }
                        }
                    }
                }
                else if (methodType == MethoSignatureType.NOT_ONLY_DOCS)
                {
                    foreach (var doc in docs)
                    {
                        foreach (var currentUser in orderedUserTccs)
                        {
                            var alreadySigned = doc.Signatures.Any(s => s.UserId == currentUser.User.Id);
                            if (alreadySigned) continue;

                            // Verifica se esse usuário está autorizado a assinar esse tipo de documento
                            var isEligible = docType.Profiles.Any(p => p.Id == currentUser.Profile.Id);
                            if (!isEligible) continue;

                            // Verifica se os usuários anteriores na fila já assinaram
                            var currentProfileIndex = _signatureQueueByProfile.IndexOf(currentUser.Profile.Role);

                            var allPreviousProfilesSigned = orderedUserTccs
                                .Where(u =>
                                    _signatureQueueByProfile.IndexOf(u.Profile.Role) < currentProfileIndex &&
                                    docType.Profiles.Any(p => p.Id == u.Profile.Id)) // apenas perfis autorizados
                                .All(prev =>
                                    doc.Signatures.Any(sig => sig.UserId == prev.User.Id));

                            if (allPreviousProfilesSigned)
                            {
                                pendingDetails.Add(CreateDetail(doc, docType, currentUser, methodType));
                                break; // Apenas um usuário por vez da fila deve assinar
                            }
                        }
                    }
                }
            }

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

    private static FindAllPendingSignatureDetailsDTO CreateDetail(
        DocumentEntity doc,
        DocumentTypeEntity docType,
        UserTccEntity userTcc,
        MethoSignatureType MethoSignature)
    {
        return new FindAllPendingSignatureDetailsDTO(
            doc.Id,
            docType.Name,
            new List<FindAllPendignSignatureUserDetailsDTO>
            {
                new(
                    userTcc.User.Id, 
                    userTcc.User.Name, 
                    userTcc.Profile.Role,
                    MethoSignature == MethoSignatureType.NOT_ONLY_DOCS ? doc.User!.Name : null,
                    MethoSignature == MethoSignatureType.NOT_ONLY_DOCS ? doc.User!.Id : null)
            });
    }
}



