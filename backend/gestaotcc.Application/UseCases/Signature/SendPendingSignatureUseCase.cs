using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Dtos.Tcc;
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
    };

    private readonly List<string> _signatureQueueByProfile = new()
    {
        RoleType.ADVISOR.ToString(),
        RoleType.BANKING.ToString(),
        RoleType.STUDENT.ToString()
    };

    public async Task<ResultPattern<string>> Execute()
    {
        var tccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(null, "IN_PROGRESS"));
        if (!tccs.Any())
            return ResultPattern<string>.SuccessResult();

        var documentTypes = await documentTypeGateway.FindAll();
        var usersToNotify = new Dictionary<string, SendPendingSignatureDTO>();

        foreach (var tcc in tccs)
        {
            if (!Enum.TryParse<StepTccType>(tcc.Step, out var tccStep)) continue;
            if (!_stepSignatureOrderMap.TryGetValue(tccStep, out var currentOrder)) continue;
            if (tccStep == StepTccType.PROPOSAL_REGISTRATION) continue;

            var userTccs = tcc.UserTccs.ToList();
            var expectedDocTypes = GetExpectedDocumentTypes(documentTypes, userTccs, currentOrder);
            var documentsInStep = tcc.Documents.Where(d => d.DocumentType.SignatureOrder == currentOrder).ToList();

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
                            // Documento individual vinculado a um usuário
                            var user = userTccs.FirstOrDefault(u => u.User.Id == doc.UserId);
                            if (user != null && doc.Signatures.All(s => s.UserId != user.User.Id))
                            {
                                NotifyUser(usersToNotify, user.User, tcc.Title!, docType.Name, doc.User!.Name);
                            }
                        }
                        else
                        {
                            // Documento compartilhado (UserId == null) - fila por perfil
                            foreach (var currentUser in orderedUserTccs)
                            {
                                var alreadySigned = doc.Signatures.Any(s => s.UserId == currentUser.User.Id);
                                if (alreadySigned) continue;

                                var index = orderedUserTccs.IndexOf(currentUser);
                                var allPreviousSigned = orderedUserTccs.Take(index)
                                    .All(prev => doc.Signatures.Any(s => s.UserId == prev.User.Id));

                                if (allPreviousSigned)
                                {
                                    NotifyUser(usersToNotify, currentUser.User, tcc.Title!, docType.Name, "Doc-Compatilhado");
                                }

                                break; // Só um usuário pode ser notificado por vez
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

                            // Verifica se o usuário está autorizado para esse documento
                            var isEligible = docType.Profiles.Any(p => p.Id == currentUser.Profile.Id);
                            if (!isEligible) continue;

                            // Verifica se os perfis anteriores já assinaram
                            var currentProfileIndex = _signatureQueueByProfile.IndexOf(currentUser.Profile.Role);

                            var allPreviousProfilesSigned = orderedUserTccs
                                .Where(u =>
                                    _signatureQueueByProfile.IndexOf(u.Profile.Role) < currentProfileIndex &&
                                    docType.Profiles.Any(p => p.Id == u.Profile.Id))
                                .All(prev => doc.Signatures.Any(sig => sig.UserId == prev.User.Id));

                            if (allPreviousProfilesSigned)
                            {
                                NotifyUser(usersToNotify, currentUser.User, tcc.Title!, docType.Name, doc.User?.Name);
                                break; // Apenas um usuário por vez da fila deve ser notificado
                            }
                        }
                    }
                }
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

    private void NotifyUser(
        Dictionary<string, SendPendingSignatureDTO> usersToNotify,
        UserEntity user,
        string tccTitle,
        string documentName,
        string? nameDocumentOwner)
    {
        var detail = new SendPendingSignatureDetailsDTO(documentName, nameDocumentOwner);

        if (usersToNotify.TryGetValue(user.Email, out var existing))
        {
            var updatedDetails = existing.Details.Concat(new[] { detail })
                .GroupBy(d => d.DocumentName)
                .Select(g => g.First()) // evitar duplicados
                .ToList();

            usersToNotify[user.Email] = new SendPendingSignatureDTO(user.Email, user.Name, updatedDetails, tccTitle);
        }
        else
        {
            usersToNotify[user.Email] = new SendPendingSignatureDTO(user.Email, user.Name, new List<SendPendingSignatureDetailsDTO> { detail }, tccTitle);
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
