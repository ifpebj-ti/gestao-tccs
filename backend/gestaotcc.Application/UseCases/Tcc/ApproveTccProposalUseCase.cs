using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ApproveTccProposalUseCase(
    ITccGateway tccGateway,
    IDocumentTypeGateway documentTypeGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<ApproveTccProposalUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(long tccId, long advisorUserId)
    {
        logger.LogInformation("Iniciando aprovação de proposta de TCC {TccId} pelo orientador {AdvisorId}", tccId, advisorUserId);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("TCC com Id {TccId} não encontrado.", tccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }

        var advisorUserTcc = tcc.UserTccs.FirstOrDefault(ut => ut.UserId == advisorUserId);
        if (advisorUserTcc is null)
        {
            logger.LogWarning("Usuário {AdvisorId} não está vinculado como orientador do TCC {TccId}.", advisorUserId, tccId);
            return ResultPattern<string>.FailureResult("Você não é o orientador desta proposta", 403);
        }

        if (tcc.Status != StatusTccType.PENDING_APPROVAL.ToString())
        {
            logger.LogWarning("TCC {TccId} com status {Status} não está pendente de aprovação.", tccId, tcc.Status);
            return ResultPattern<string>.FailureResult("A proposta não está pendente de aprovação", 409);
        }

        tcc.Status = StatusTccType.IN_PROGRESS.ToString();
        tcc.Step = StepTccType.START_AND_ORGANIZATION.ToString();
        tcc.RejectionReason = null;

        if (tcc.Documents.Count == 0)
        {
            var documentTypes = await documentTypeGateway.FindAll();
            GenerateDocumentsForTcc(tcc, documentTypes);
        }

        await tccGateway.Update(tcc);
        logger.LogInformation("Proposta de TCC {TccId} aprovada com sucesso.", tccId);

        var studentUserTcc = tcc.UserTccs.FirstOrDefault(ut => ut.Profile.Role == RoleType.STUDENT.ToString());
        if (studentUserTcc?.User != null)
        {
            try
            {
                var emailDto = EmailFactory.CreateSendEmailDTO(studentUserTcc.User, "ADD-USER-TCC");
                await emailGateway.Send(emailDto);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Erro ao enviar e-mail de aprovação para o estudante {StudentEmail}: {ErrorMessage}", studentUserTcc.User.Email, ex.Message);
            }
        }

        return ResultPattern<string>.SuccessResult("Proposta de TCC aprovada com sucesso!");
    }

    private void GenerateDocumentsForTcc(TccEntity tcc, List<DocumentTypeEntity> documentTypes)
    {
        // Garante que cada UserEntity tenha seu Profile preenchido a partir do UserTccEntity
        foreach (var ut in tcc.UserTccs)
        {
            if (ut.User != null && ut.Profile != null)
            {
                if (ut.User.Profile == null)
                {
                    ut.User.Profile = new List<ProfileEntity>();
                }
                if (!ut.User.Profile.Any(p => p.Role == ut.Profile.Role))
                {
                    ut.User.Profile.Add(ut.Profile);
                }
            }
        }

        var users = tcc.UserTccs
            .Where(ut => ut.User != null)
            .Select(ut => ut.User)
            .DistinctBy(u => u.Id)
            .ToList();

        foreach (var docType in documentTypes)
        {
            if (docType.Name != null && docType.Name.Contains("ANEXO II - TERMO DE COMPROMISSO DE ORIENTAÇÃO VOLUNTÁRIA"))
                continue;

            var acceptedRoles = docType.Profiles.Select(p => p.Role).ToHashSet();
            var method = Enum.Parse<MethoSignatureType>(docType.MethodSignature);

            var usersWithAcceptedProfile = users
                .Where(user => user.Profile != null && user.Profile.Any(p => acceptedRoles.Contains(p.Role)))
                .ToList();

            if (method == MethoSignatureType.ONLY_DOCS)
            {
                if (docType.Profiles.Count > 1)
                {
                    tcc.Documents.Add(DocumentFactory.CreateDocument(docType, tcc.Title ?? "", null));
                }
                else if (docType.Profiles.Count == 1)
                {
                    var profileRole = docType.Profiles.First().Role;
                    var user = usersWithAcceptedProfile.FirstOrDefault(u => u.Profile != null && u.Profile.Any(p => p.Role == profileRole));
                    if (user != null)
                    {
                        tcc.Documents.Add(DocumentFactory.CreateDocument(docType, tcc.Title ?? "", user));
                    }
                }
            }
            else if (method == MethoSignatureType.NOT_ONLY_DOCS)
            {
                foreach (var user in usersWithAcceptedProfile)
                {
                    var profile = user.Profile?.FirstOrDefault();
                    if (profile == null) continue;

                    if (profile.Role != RoleType.STUDENT.ToString() && docType.Profiles.Count > 1)
                        continue;

                    tcc.Documents.Add(DocumentFactory.CreateDocument(docType, tcc.Title ?? "", user));
                }
            }
        }
    }
}
