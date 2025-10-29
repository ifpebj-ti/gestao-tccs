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

public class SignSignatureUseCase(
    IDocumentTypeGateway documentTypeGateway, 
    ITccGateway tccGateway, 
    IMinioGateway minioGateway,
    IITextGateway iTextGateway,
    IGovGateway govGateway,
    IAppLoggerGateway<SignSignatureUseCase> logger)
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
        logger.LogInformation("Iniciando processo de assinatura de documento. TccId: {TccId}, DocumentId: {DocumentId}, UserId: {UserId}", data.TccId, data.DocumentId, data.UserId);

        if (!IsValidFile(data))
        {
            logger.LogWarning("Falha na assinatura para UserId {UserId}: Arquivo inválido. Tamanho: {FileSize}MB, ContentType: {ContentType}", data.UserId, data.FileSize, data.FileContentType);
            return InvalidFileResult();
        }

        var tcc = await tccGateway.FindTccById(data.TccId);
        var allDocumentTypes = await documentTypeGateway.FindAll();

        var user = GetUserInTcc(tcc, data.UserId);
        if (UserAlreadySigned(tcc, data.DocumentId, user.Id))
        {
            logger.LogWarning("Falha na assinatura para UserId {UserId}: Usuário já assinou o DocumentId {DocumentId}.", data.UserId, data.DocumentId);
            return InvalidFileResult();
        }

        var document = tcc.Documents.First(d => d.Id == data.DocumentId);
        
        if (!UserCanSignDocument(user, tcc, document))
        {
            logger.LogWarning("Falha na assinatura para UserId {UserId}: Usuário não tem permissão para assinar o DocumentId {DocumentId}.", data.UserId, data.DocumentId);
            return InvalidFileResult();
        }
        
        byte[] finalSignedPdfBytes;
        try
        {
            // PASSO A (IText): Preparar o PDF, criar placeholder, calcular o hash
            logger.LogInformation("Preparando PDF para assinatura envelopada (PAdES)...");
            var (hashB64, preparedPdf) = await iTextGateway.PrepareEnvelopedSignature(data.File);

            // PASSO B (Gov): Chamar a API de assinatura 'assinarPKCS7'
            logger.LogInformation("Enviando hash para API de assinatura Gov.br...");
            var signatureB64 = await govGateway.SignPkcs7(hashB64, data.AccessToken);
            if (string.IsNullOrEmpty(signatureB64))
            {
                logger.LogError("API Gov.br (assinarPKCS7) não retornou uma assinatura válida.");
                return ResultPattern<string>.FailureResult("Falha no serviço de assinatura do Gov.br.", 503); // Service Unavailable
            }

            // PASSO C (IText): Embutir a assinatura no PDF
            logger.LogInformation("Embutindo assinatura digital PKCS#7 no documento PDF...");
            finalSignedPdfBytes = await iTextGateway.EmbedEnvelopedSignature(preparedPdf, signatureB64);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro técnico durante o processo de assinatura digital. UserId: {UserId}, DocumentId: {DocumentId}", data.UserId, data.DocumentId);
            return ResultPattern<string>.FailureResult($"Erro técnico no processo de assinatura: {ex.Message}", 500);
        }
        
        logger.LogInformation("Validações bem-sucedidas. Adicionando assinatura do UserId {UserId} ao DocumentId {DocumentId}.", user.Id, document.Id);
        document.Signatures.Add(SignatureFactory.CreateSignature(user));

        if (!Enum.TryParse<StepTccType>(tcc.Step, out var tccStep))
        {
            logger.LogError("Falha na assinatura para TccId {TccId}: A etapa atual '{TccStep}' é inválida.", tcc.Id, tcc.Step);
            return InvalidFileResult();
        }
        if (!_stepSignatureOrderMap.TryGetValue(tccStep, out var currentOrder))
        {
            logger.LogError("Falha na assinatura para TccId {TccId}: A etapa '{TccStep}' não está mapeada para uma ordem de assinatura.", tcc.Id, tcc.Step);
            return InvalidFileResult();
        }

        var documentTypesInStep = allDocumentTypes
            .Where(dt => dt.SignatureOrder == currentOrder)
            .ToList();

        var documentsInStep = tcc.Documents
            .Where(d => documentTypesInStep.Any(dt => dt.Id == d.DocumentTypeId))
            .ToList();

        if (AllDocumentsInStepAreSigned(tcc, documentsInStep))
        {
            var oldStep = tcc.Step;
            tcc.Step = GetNextStep(tccStep);
            logger.LogInformation("Todas as assinaturas da etapa {OldStep} foram concluídas. Avançando TccId {TccId} para a etapa {NewStep}.", oldStep, tcc.Id, tcc.Step);
        }
        else
        {
            logger.LogInformation("Assinatura registrada para TccId {TccId}. Ainda existem pendências na etapa atual.", tcc.Id);
        }
        
        logger.LogInformation("Atualizando TCC no banco de dados. TccId: {TccId}", tcc.Id);
        await tccGateway.Update(tcc);
        
        logger.LogInformation("Enviando arquivo assinado para o Minio. FileName: {FileName}", document.FileName);
        await minioGateway.Send(document.FileName, finalSignedPdfBytes, data.FileContentType);
        
        logger.LogInformation("Processo de assinatura concluído com sucesso para UserId {UserId}, DocumentId {DocumentId}.", data.UserId, data.DocumentId);
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
                        if (document.UserId.HasValue && document.UserId == userId)
                        {
                            expectedUserIds.Add(userId);
                        }
                    }
                    else
                    {
                        expectedUserIds.Add(userId);
                    }
                }

                var signedUserIds = document.Signatures.Select(s => s.User.Id).ToHashSet();
                
                if (!expectedUserIds.IsSubsetOf(signedUserIds))
                {
                    return false;
                }
            }
            else
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
            return document.UserId == user.Id;
        }

        return true;
    }
}