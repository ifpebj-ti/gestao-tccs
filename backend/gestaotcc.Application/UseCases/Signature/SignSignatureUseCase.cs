using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;

namespace gestaotcc.Application.UseCases.Signature;

public class SignSignatureUseCase(IDocumentTypeGateway documentTypeGateway, ITccGateway tccGateway, IMinioGateway minioGateway)
{
    private readonly Dictionary<StepTccType, int> _stepSignatureOrderMap = new()
    {
        { StepTccType.PROPOSAL_REGISTRATION, 1 },
        { StepTccType.START_AND_ORGANIZATION, 2 },
        { StepTccType.DEVELOPMENT_AND_MONITORING, 3 },
        { StepTccType.PREPARATION_FOR_PRESENTATION, 4 },
        { StepTccType.PRESENTATION_AND_EVALUATION, 5 },
    };

    public async Task<ResultPattern<string>> Execute(SignSignatureDTO data)
    {
        if (!IsValidFile(data))
        {
            return InvalidFileResult();
        }

        var tcc = await tccGateway.FindTccById(data.TccId);
        var allDocumentTypes = await documentTypeGateway.FindAll();

        var user = GetUserInTcc(tcc, data.UserId);
        if (UserAlreadySigned(tcc, data.DocumentId, user.Id))
        {
            return InvalidFileResult();
        }

        var document = tcc.Documents.First(d => d.Id == data.DocumentId);
        
        if (!UserCanSignDocument(user, tcc, document))
            return InvalidFileResult();
        
        document.Signatures.Add(SignatureFactory.CreateSignature(user));

        if (!Enum.TryParse<StepTccType>(tcc.Step, out var tccStep)) return InvalidFileResult();
        if (!_stepSignatureOrderMap.TryGetValue(tccStep, out var currentOrder)) return InvalidFileResult();

        var documentTypesInStep = allDocumentTypes
            .Where(dt => dt.SignatureOrder == currentOrder)
            .ToList();

        var documentsInStep = tcc.Documents
            .Where(d => documentTypesInStep.Any(dt => dt.Id == d.DocumentTypeId))
            .ToList();

        if (AllDocumentsInStepAreSigned(tcc, documentsInStep))
        {
            tcc.Step = GetNextStep(tccStep);
        }

        await tccGateway.Update(tcc);
        await minioGateway.Send(document.FileName, data.File, data.FileContentType);

        return ResultPattern<string>.SuccessResult();
    }

    private static bool IsValidFile(SignSignatureDTO data)
    {
        return data.FileSize <= 5 && data.FileContentType == "application/pdf";
    }

    private static ResultPattern<string> InvalidFileResult()
    {
        return ResultPattern<string>.FailureResult(
            "Erro ao realizar upload. Por favor verifique o tamanho, o tipo e se já enviou o arquivo enviado e tente novamente.",
            409
        );
    }

    private static UserEntity GetUserInTcc(TccEntity tcc, long userId)
    {
        return tcc.UserTccs.First(ut => ut.UserId == userId).User;
    }

    private static bool UserAlreadySigned(TccEntity tcc, long documentId, long userId)
    {
        return tcc.Documents
            .Any(doc => doc.Signatures.Any(sig => sig.UserId == userId && sig.DocumentId == documentId));
    }

    private bool AllDocumentsInStepAreSigned(TccEntity tcc, List<DocumentEntity> documents)
    {
        foreach (var document in documents)
        {
            var docType = document.DocumentType;
            var method = Enum.Parse<MethoSignatureType>(docType.MethodSignature);
            var acceptedRoles = docType.Profiles.Select(p => p.Role).ToHashSet();

            // Filtra somente usuários com perfil aceito no tipo de documento
            var validUserTccs = tcc.UserTccs
                .Where(ut => acceptedRoles.Contains(ut.Profile.Role))
                .ToList();
            
            if (method == MethoSignatureType.NOT_ONLY_DOCS)
            {
                var expectedUserIds = new HashSet<long>();

                foreach (var ut in validUserTccs)
                {
                    var role = ut.Profile.Role;
                    var userId = ut.User.Id;

                    if (role == RoleType.STUDENT.ToString())
                    {
                        // Apenas o dono do documento (estudante) deve assinar este documento
                        if (document.UserId.HasValue && document.UserId == userId)
                        {
                            expectedUserIds.Add(userId);
                        }
                    }
                    else
                    {
                        // ADVISOR, etc. devem assinar todos os documentos do tipo
                        expectedUserIds.Add(userId);
                    }
                }

                var signedUserIds = document.Signatures.Select(s => s.User.Id).ToHashSet();

                // Se faltar alguém, ainda não pode mudar o step
                if (!expectedUserIds.IsSubsetOf(signedUserIds))
                {
                    return false;
                }
            }
            else // ONLY_DOCS
            {
                var expectedUserIds = validUserTccs.Select(ut => ut.User.Id).ToHashSet();
                var signedUserIds = document.Signatures.Select(s => s.User.Id).ToHashSet();

                if (!expectedUserIds.IsSubsetOf(signedUserIds))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private string GetNextStep(StepTccType currentStep)
    {
        var nextOrder = _stepSignatureOrderMap[currentStep] + 1;
        var nextStep = _stepSignatureOrderMap
            .FirstOrDefault(kv => kv.Value == nextOrder).Key;

        return nextStep.ToString();
    }
    
    private static bool UserCanSignDocument(UserEntity user, TccEntity tcc, DocumentEntity document)
    {
        var userTcc = tcc.UserTccs.FirstOrDefault(ut => ut.User.Id == user.Id);
        if (userTcc == null) return false;

        var profile = userTcc.Profile;
        var allowedRoles = document.DocumentType.Profiles.Select(p => p.Role).ToHashSet();

        if (!allowedRoles.Contains(profile.Role))
            return false;

        var method = Enum.Parse<MethoSignatureType>(document.DocumentType.MethodSignature);

        if (method == MethoSignatureType.NOT_ONLY_DOCS && profile.Role == RoleType.STUDENT.ToString())
        {
            // Verifica se o estudante está assinando o documento que pertence a ele
            return document.UserId == user.Id;
        }

        return true; // ADVISOR ou ONLY_DOCS ou estudantes autorizados corretamente
    }

}
