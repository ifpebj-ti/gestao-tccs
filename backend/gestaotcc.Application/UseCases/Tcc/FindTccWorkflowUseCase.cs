using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindTccWorkflowUseCase(ITccGateway tccGateway, IDocumentTypeGateway documentTypeGateway)
{
    public async Task<ResultPattern<FindTccWorkflowDTO>> Execute(long? tccId, long userId)
    {
        var tcc = await tccGateway.FindTccWorkflow(tccId, userId);
        if (tcc is null)
            return ResultPattern<FindTccWorkflowDTO>.FailureResult("Erro ao buscar workflow", 404);
        var allDocumentsType = await documentTypeGateway.FindAll();

        var dto = CreateWorkflowDto(tcc!, allDocumentsType);
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

        if (!Enum.TryParse<StepTccType>(tcc.Step.ToString(), out var tccStep) ||
            !stepSignatureOrderMap.TryGetValue(tccStep, out var signatureOrder))
            return null!;

        var userTccs = FindUsersForStep(tcc, signatureOrder);

        if (tccStep == StepTccType.PROPOSAL_REGISTRATION)
        {
            var details = CreateStudentRegistrationDetails(tcc, userTccs);
            return new FindTccWorkflowDTO(tcc.Id, signatureOrder, new List<string>(), tcc.TccInvites
                .Select(x => new FindTccWorkflowSignatureDTO(1, "CADASTRO DE ESTUDANTES", null, details))
                .ToList());
        }

        var workflowSignatures = allDocumentsType
            .Where(x => x.SignatureOrder == signatureOrder)
            .Select(documentType => CreateSignatureDto(documentType, tcc.Documents, userTccs))
            .OrderBy(x => x.DocumentId)
            .ToList();

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
        var methodType = Enum.TryParse<MethoSignatureType>(documentType.MethodSignature, out var result)
            ? result
            : MethoSignatureType.ONLY_DOCS;

        var documents = allDocuments
            .Where(d => d.DocumentTypeId == documentType.Id)
            .ToList();

        if (methodType == MethoSignatureType.ONLY_DOCS)
        {
            var details = new List<FindTccWorkflowSignatureDetailsOnlyDocsDTO>();

            foreach (var doc in documents)
            {
                if (doc.User is null)
                {
                    // Documento compartilhado: adicionar todos os usuários esperados
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
                    // Documento vinculado a usuário específico
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

        return new FindTccWorkflowSignatureDTO(documentType.Id, documentType.Name, null, detailsNotOnly);
    }
}
