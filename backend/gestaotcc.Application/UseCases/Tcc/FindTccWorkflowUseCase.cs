using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class FindTccWorkflowUseCase(ITccGateway tccGateway, IDocumentTypeGateway documentTypeGateway)
{
    public async Task<ResultPattern<FindTccWorkflowDTO>> Execute(long tccId, long userId)
    {
        var tcc = await tccGateway.FindTccWorkflow(tccId, userId);
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
            { StepTccType.PRESENTATION_AND_EVALUATION, 5 },
            { StepTccType.FINALIZATION_AND_PUBLICATION, 6 }
        };

        if (!Enum.TryParse<StepTccType>(tcc.Step.ToString(), out var tccStep) ||
            !stepSignatureOrderMap.TryGetValue(tccStep, out var signatureOrder))
            return null!;

        // Pegar apenas os usuários que podem assinar os documentos do step (signatureOrder) atual
        var users = FindUsersForStep(tcc, signatureOrder);

        // Para caso estejamos no step de cadastro de estudantes
        if (tccStep == StepTccType.PROPOSAL_REGISTRATION)
        {
            var details = CreateStudentRegistrationDetails(tcc, users);
            return new FindTccWorkflowDTO(tcc.Id, signatureOrder, tcc.TccInvites
                .Select(x => new FindTccWorkflowSignatureDTO(1, "CADASTRO DE ESTUDANTES", details))
                .ToList());
        }

        var documentsType = tcc.Documents
            .Where(x => x.DocumentType.SignatureOrder == signatureOrder)
            .Select(x => x.DocumentType)
            .Distinct()
            .ToList();

        // Para caso não tenha nenhum documento assinado. Logo isSigned é falso para todos
        if (!documentsType.Any())
        {
            var details = CreateUnsignedUserDetails(users);
            var signatures = allDocumentsType
                .Where(x => x.SignatureOrder == signatureOrder)
                .Select(x => new FindTccWorkflowSignatureDTO(x.Id, x.Name, details))
                .OrderBy(x => x.DocumentId)
                .ToList();

            return new FindTccWorkflowDTO(tcc.Id, signatureOrder, signatures);
        }

        var workflowSignatures = allDocumentsType
            .Where(x => x.SignatureOrder == signatureOrder)
            .Select(documentType => CreateSignatureDto(documentType, documentsType, users))
            .OrderBy(x => x.DocumentId)
            .ToList();

        return new FindTccWorkflowDTO(tcc.Id, signatureOrder, workflowSignatures);
    }

    private List<UserEntity> FindUsersForStep(TccEntity tcc, int signatureOrder)
    {
        return tcc.UserTccs
            .Where(x => x.User.Profile
                .Any(p => p.DocumentTypes
                    .Any(d => d.SignatureOrder == signatureOrder)))
            .Select(u => u.User)
            .ToList();
    }

    private List<FindTccWorkflowSignatureDetailsDTO> CreateStudentRegistrationDetails(TccEntity tcc, List<UserEntity> users)
    {
        return tcc.TccInvites
            .Select(x =>
            {
                var user = users.FirstOrDefault(u => u.Email == x.Email);
                var accepted = user is not null;
                return new FindTccWorkflowSignatureDetailsDTO(
                    user?.Id ?? 0,
                    RoleType.STUDENT.ToString(),
                    x.Email,
                    accepted
                );
            })
            .OrderBy(x => x.UserName)
            .ToList();
    }

    private List<FindTccWorkflowSignatureDetailsDTO> CreateUnsignedUserDetails(List<UserEntity> users)
    {
        return users
            .Select(user =>
            {
                var role = user.Profile.MinBy(p => p.Id).Role;
                return new FindTccWorkflowSignatureDetailsDTO(user.Id, role, user.Name, false);
            })
            .OrderBy(x => x.UserName)
            .ToList();
    }

    private FindTccWorkflowSignatureDTO CreateSignatureDto(
        DocumentTypeEntity documentType,
        List<DocumentTypeEntity> tccDocumentsType,
        List<UserEntity> users)
    {
        List<FindTccWorkflowSignatureDetailsDTO> details;

        var isAssociated = tccDocumentsType.Any(d => d.Id == documentType.Id);
        var hasDocuments = documentType.Documents?.Any() == true;

        // Construindo o objeto para caso haja documento assinado.
        if (isAssociated && hasDocuments)
        {
            details = documentType.Documents
                .SelectMany(d => d.Signatures)
                .Select(signature =>
                {
                    var role = signature.User.Profile.MinBy(p => p.Id).Role;
                    var isSigned = users.Contains(signature.User);
                    return new FindTccWorkflowSignatureDetailsDTO(
                        signature.User.Id,
                        role,
                        signature.User.Name,
                        isSigned
                    );
                })
                .ToList();

            // Adicionando usuários que não assinaram esse documento
            foreach (var user in users)
            {
                var alreadyExists = details.Any(d => d.UserId == user.Id);
                if (!alreadyExists)
                {
                    var role = user.Profile.MinBy(p => p.Id).Role;
                    details.Add(new FindTccWorkflowSignatureDetailsDTO(
                        user.Id,
                        role,
                        user.Name,
                        false
                    ));
                }
            }
        }
        // Construindo o objeto para caso não haja documento assinado.
        else
        {
            details = CreateUnsignedUserDetails(users);
        }

        var orderedDetails = details.OrderBy(x => x.UserName).ToList();
        return new FindTccWorkflowSignatureDTO(documentType.Id, documentType.Name, orderedDetails);
    }
}