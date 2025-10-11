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
    IDocumentTypeGateway documentTypeGateway,
    IAppLoggerGateway<FindAllPendingSignaturesUseCase> logger)
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

    public async Task<ResultPattern<List<FindAllPendingSignatureDTO>>> Execute(long? userId, long campiId)
    {
        logger.LogInformation("Iniciando busca por assinaturas pendentes. UserId: {UserId}", userId.HasValue ? userId.Value.ToString() : "GLOBAL");
        
        var tccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(userId, StatusTccType.IN_PROGRESS.ToString()), campiId);
        if (!tccs.Any())
        {
            logger.LogInformation("Nenhum TCC em andamento encontrado para o filtro aplicado. Retornando lista vazia.");
            return ResultPattern<List<FindAllPendingSignatureDTO>>.SuccessResult(new());
        }
        
        logger.LogInformation("{TccCount} TCCs em andamento encontrados.", tccs.Count);

        var documentTypes = await documentTypeGateway.FindAll();
        logger.LogInformation("{DocTypeCount} tipos de documento carregados.", documentTypes.Count);
        
        var results = new List<FindAllPendingSignatureDTO>();

        foreach (var tcc in tccs)
        {
            logger.LogDebug("Processando TccId: {TccId} na etapa: {TccStep}", tcc.Id, tcc.Step);

            if (!Enum.TryParse<StepTccType>(tcc.Step, out var tccStep))
            {
                logger.LogDebug("TccId {TccId} pulado: Etapa '{TccStep}' é inválida.", tcc.Id, tcc.Step);
                continue;
            }
            if (!_stepSignatureOrderMap.TryGetValue(tccStep, out var currentOrder))
            {
                logger.LogDebug("TccId {TccId} pulado: Etapa '{TccStep}' não mapeada para ordem de assinatura.", tcc.Id, tcc.Step);
                continue;
            }
            if (tccStep == StepTccType.PROPOSAL_REGISTRATION)
            {
                logger.LogDebug("TccId {TccId} pulado: Etapa é PROPOSAL_REGISTRATION.", tcc.Id);
                continue;
            }

            var userTccs = tcc.UserTccs.ToList();
            var studentNames = GetStudentNames(tcc, userTccs);
            var expectedDocTypes = GetExpectedDocumentTypes(documentTypes, userTccs, currentOrder);
            var documentsInStep = tcc.Documents.Where(d => d.DocumentType.SignatureOrder == currentOrder).ToList();

            var pendingDetails = new List<FindAllPendingSignatureDetailsDTO>();

            foreach (var docType in expectedDocTypes)
            {
                var methodType = Enum.Parse<MethoSignatureType>(docType.MethodSignature);
                var docs = documentsInStep.Where(d => d.DocumentTypeId == docType.Id).ToList();

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
                            var user = userTccs.FirstOrDefault(u => u.User.Id == doc.UserId);
                            if (user != null && doc.Signatures.All(s => s.UserId != user.User.Id))
                            {
                                pendingDetails.Add(CreateDetail(doc, docType, user, methodType));
                            }
                        }
                        else
                        {
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
                                break;
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

                            var isEligible = docType.Profiles.Any(p => p.Id == currentUser.Profile.Id);
                            if (!isEligible) continue;

                            var currentProfileIndex = _signatureQueueByProfile.IndexOf(currentUser.Profile.Role);

                            var allPreviousProfilesSigned = orderedUserTccs
                                .Where(u =>
                                    _signatureQueueByProfile.IndexOf(u.Profile.Role) < currentProfileIndex &&
                                    docType.Profiles.Any(p => p.Id == u.Profile.Id))
                                .All(prev =>
                                    doc.Signatures.Any(sig => sig.UserId == prev.User.Id));

                            if (allPreviousProfilesSigned)
                            {
                                pendingDetails.Add(CreateDetail(doc, docType, currentUser, methodType));
                                break;
                            }
                        }
                    }
                }
            }
            
            logger.LogDebug("TccId {TccId}: Encontrados {PendingCount} detalhes de assinatura pendente (antes do filtro).", tcc.Id, pendingDetails.Count);

            if (userId.HasValue)
            {
                var originalCount = pendingDetails.Count;
                pendingDetails = pendingDetails
                    .Where(pd => pd.UserDetails.Any(u => u.UserId == userId.Value))
                    .ToList();
                logger.LogInformation("TccId {TccId}: Filtrando para UserId {UserId}. Contagem de pendências foi de {OriginalCount} para {FilteredCount}.", tcc.Id, userId.Value, originalCount, pendingDetails.Count);
            }

            if (pendingDetails.Any())
            {
                logger.LogInformation("TccId {TccId} possui {PendingDetailsCount} assinaturas pendentes e será adicionado ao resultado.", tcc.Id, pendingDetails.Count);
                results.Add(new FindAllPendingSignatureDTO(
                    tcc.Id,
                    studentNames,
                    pendingDetails));
            }
        }

        logger.LogInformation("Busca por assinaturas pendentes concluída. Retornando {ResultCount} TCCs com pendências.", results.Count);
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