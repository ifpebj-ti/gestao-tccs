using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindTccWorkflowUseCase(ITccGateway tccGateway, IDocumentTypeGateway documentTypeGateway, IAppLoggerGateway<FindTccWorkflowUseCase> logger)
{
    public async Task<ResultPattern<FindTccWorkflowDTO>> Execute(long? tccId, long userId)
    {
        logger.LogInformation("Iniciando busca de workflow para TccId: {TccId}, UserId: {UserId}", tccId, userId);
        var tcc = await tccGateway.FindTccWorkflow(tccId, userId);
        if (tcc is null)
        {
            logger.LogWarning("Falha na busca de workflow: TCC não encontrado para o filtro TccId: {TccId}, UserId: {UserId}", tccId, userId);
            return ResultPattern<FindTccWorkflowDTO>.FailureResult("Erro ao buscar workflow", 404);
        }
        
        logger.LogInformation("TCC encontrado. Carregando todos os tipos de documento.");
        var allDocumentsType = await documentTypeGateway.FindAll();

        var dto = CreateWorkflowDto(tcc!, allDocumentsType);
        
        logger.LogInformation("Busca de workflow concluída com sucesso para TccId: {TccId}", tcc.Id);
        return ResultPattern<FindTccWorkflowDTO>.SuccessResult(dto);
    }

    private FindTccWorkflowDTO CreateWorkflowDto(TccEntity tcc, List<DocumentTypeEntity> allDocumentsType)
    {
        var stepSignatureOrderMap = new Dictionary<StepTccType, int>
        {
            { StepTccType.PROPOSAL_REGISTRATION, 1 },
            { StepTccType.START_AND_ORGANIZATION, 2 },
            { StepTccType.DEVELOPMENT_AND_MONITORING, 3 },
            { StepTccType.PREPARATION_FOR_PRESENTATION, 4 },
            { StepTccType.PRESENTATION_AND_EVALUATION, 5 }
        };
        
        logger.LogInformation("Criando DTO de workflow para TccId: {TccId} na etapa: {TccStep}", tcc.Id, tcc.Step);

        if (!Enum.TryParse<StepTccType>(tcc.Step.ToString(), out var tccStep) ||
            !stepSignatureOrderMap.TryGetValue(tccStep, out var signatureOrder))
        {
            logger.LogError("Etapa inválida ou não mapeada para o TccId {TccId}: '{TccStep}'. Não é possível criar o workflow.", tcc.Id, tcc.Step);
            return null!;
        }

        var userTccs = FindUsersForStep(tcc, signatureOrder);
        logger.LogDebug("Encontrados {UserCount} usuários relevantes para a etapa {TccStep} do TccId {TccId}.", userTccs.Count, tccStep, tcc.Id);

        if (tccStep == StepTccType.PROPOSAL_REGISTRATION)
        {
            logger.LogInformation("TccId {TccId} está na etapa de registro. Executando lógica de convites de estudantes.", tcc.Id);
            userTccs = tcc.UserTccs.Where(ut => ut.Profile.Role == RoleType.STUDENT.ToString()).ToList();
            var details = CreateStudentRegistrationDetails(tcc, userTccs);
            return new FindTccWorkflowDTO(tcc.Id, signatureOrder, new List<string>(), new List<FindTccWorkflowSignatureDTO>()
            {
                new FindTccWorkflowSignatureDTO(1, "CADASTRO DE ESTUDANTES", null, details)
            });
        }

        var workflowSignatures = allDocumentsType
            .Where(x => x.SignatureOrder == signatureOrder)
            .Select(documentType => CreateSignatureDto(documentType, tcc.Documents, userTccs))
            .OrderBy(x => x.DocumentId)
            .ToList();
        
        logger.LogInformation("Workflow para TccId {TccId} criado com {SignatureCount} tipos de assinatura para a etapa atual.", tcc.Id, workflowSignatures.Count);
        var studentNames = tcc.UserTccs.Select(x => x.User.Name).ToList();

        return new FindTccWorkflowDTO(tcc.Id, signatureOrder, studentNames, workflowSignatures);
    }

    private List<UserTccEntity> FindUsersForStep(TccEntity tcc, int signatureOrder)
    {
        return tcc.UserTccs
            .Where(x => x.Profile.DocumentTypes
                .Any(documentType => documentType.SignatureOrder == signatureOrder))
            .ToList();
    }

    private List<FindTccWorkflowSignatureDetailsNotOnlyDocsDTO> CreateStudentRegistrationDetails(TccEntity tcc, List<UserTccEntity> userTccs)
    {
        return tcc.TccInvites
            .Select(x =>
            {
                var user = userTccs.FirstOrDefault(u => u.User.Email == x.Email);
                var accepted = user is not null;
                return new FindTccWorkflowSignatureDetailsNotOnlyDocsDTO(
                    user?.Id ?? 0,
                    RoleType.STUDENT.ToString(),
                    x.Email,
                    accepted,
                    new List<FindTccWorkflowSignatureDetailsNotOnlyDocsOtherSignaturesDTO>());
            })
            .OrderBy(x => x.UserName)
            .ToList();
    }

    private FindTccWorkflowSignatureDTO CreateSignatureDto(
        DocumentTypeEntity documentType,
        ICollection<DocumentEntity> allDocuments,
        List<UserTccEntity> userTccs)
    {
        logger.LogDebug("Criando DTO de assinatura para DocumentType: '{DocTypeName}' ({DocTypeId})", documentType.Name, documentType.Id);
        var methodType = Enum.TryParse<MethoSignatureType>(documentType.MethodSignature, out var result)
            ? result
            : MethoSignatureType.ONLY_DOCS;

        var documents = allDocuments
            .Where(d => d.DocumentTypeId == documentType.Id)
            .ToList();
        
        logger.LogDebug("Método de assinatura para '{DocTypeName}' é {MethodType}. Documentos encontrados: {DocCount}", documentType.Name, methodType, documents.Count);

        if (methodType == MethoSignatureType.ONLY_DOCS)
        {
            var details = new List<FindTccWorkflowSignatureDetailsOnlyDocsDTO>();

            foreach (var doc in documents)
            {
                if (doc.User is null)
                {
                    foreach (var userTcc in userTccs)
                    {
                        var isSigned = doc.Signatures.Any(s => s.User.Id == userTcc.User.Id);
                        details.Add(new FindTccWorkflowSignatureDetailsOnlyDocsDTO(
                            userTcc.User.Id,
                            userTcc.Profile.Role,
                            userTcc.User.Name,
                            isSigned
                        ));
                    }
                }
                else
                {
                    var user = userTccs.FirstOrDefault(u => u.User.Id == doc.User.Id);
                    var isSigned = doc.Signatures.Any(s => s.User.Id == doc.User.Id);
                    details.Add(new FindTccWorkflowSignatureDetailsOnlyDocsDTO(
                        doc.User.Id,
                        user?.Profile.Role ?? string.Empty,
                        doc.User.Name,
                        isSigned
                    ));
                }
            }
            
            logger.LogDebug("Gerados {DetailCount} detalhes de assinatura para '{DocTypeName}' (ONLY_DOCS).", details.Count, documentType.Name);
            return new FindTccWorkflowSignatureDTO(
                documentType.Id,
                documentType.Name,
                details.OrderBy(x => x.UserName).ToList(),
                null
            );
        }

        var mainUsers = userTccs.Where(x => x.Profile.Role == RoleType.STUDENT.ToString()).ToList();
        var otherUsers = userTccs.Where(x => x.Profile.Role == RoleType.ADVISOR.ToString() || x.Profile.Role == RoleType.BANKING.ToString()).ToList();

        var detailsNotOnly = mainUsers.Select(main =>
        {
            var mainDoc = documents.FirstOrDefault(d => d.User?.Id == main.User.Id);
            var isSigned = mainDoc?.Signatures.Any(s => s.User.Id == main.User.Id) ?? false;

            var others = otherUsers.Select(other =>
            {
                var otherDoc = documents.FirstOrDefault(d => d.User?.Id == main.User.Id);
                var advisorSigned = otherDoc?.Signatures.Any(s => s.User.Id == other.User.Id) ?? false;
                return new FindTccWorkflowSignatureDetailsNotOnlyDocsOtherSignaturesDTO(
                    other.User.Id,
                    other.Profile.Role,
                    other.User.Name,
                    advisorSigned);
            }).ToList();

            return new FindTccWorkflowSignatureDetailsNotOnlyDocsDTO(
                main.User.Id,
                main.Profile.Role,
                main.User.Name,
                isSigned,
                others);
        })
        .OrderBy(x => x.UserName)
        .ToList();
        
        logger.LogDebug("Gerados {DetailCount} detalhes de assinatura para '{DocTypeName}' (NOT_ONLY_DOCS).", detailsNotOnly.Count, documentType.Name);
        return new FindTccWorkflowSignatureDTO(documentType.Id, documentType.Name, null, detailsNotOnly);
    }
}