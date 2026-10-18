using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.TccAdvisorChange;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

using gestaotcc.Domain.Dtos.Signature;

namespace gestaotcc.Application.UseCases.Tcc;

public class RequestAdvisorDepartureUseCase(
    ITccGateway tccGateway,
    IUserGateway userGateway,
    IDocumentTypeGateway documentTypeGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<RequestAdvisorDepartureUseCase> logger
)
{
    public async Task<ResultPattern<string>> Execute(RequestAdvisorDepartureDTO request, long requesterUserId)
    {
        logger.LogInformation("Iniciando solicitação de afastamento de orientação para TccId {TccId} pelo docente {UserId}", request.TccId, requesterUserId);

        var tcc = await tccGateway.FindTccById(request.TccId);
        if (tcc is null)
        {
            logger.LogWarning("TCC não encontrado para o Id: {TccId}", request.TccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado.", 404);
        }

        var pendingChange = await tccGateway.FindPendingAdvisorChange(request.TccId);
        if (pendingChange is not null)
        {
            logger.LogWarning("Já existe uma solicitação pendente para o TCC {TccId}", request.TccId);
            return ResultPattern<string>.FailureResult("Já existe uma solicitação de substituição ou afastamento de orientador em andamento para este TCC.", 409);
        }

        var currentAdvisorUt = tcc.UserTccs.FirstOrDefault(ut =>
            ut.Profile.Role == RoleType.ADVISOR.ToString() ||
            ut.Profile.Role == RoleType.COORDINATOR.ToString() ||
            ut.Profile.Role == RoleType.SUPERVISOR.ToString()
        );

        if (currentAdvisorUt is null)
        {
            logger.LogWarning("Orientador atual não encontrado no TCC {TccId}", request.TccId);
            return ResultPattern<string>.FailureResult("Orientador atual não encontrado no TCC.", 400);
        }

        if (currentAdvisorUt.UserId != requesterUserId)
        {
            logger.LogWarning("Usuário {UserId} tentou solicitar afastamento sem ser o orientador do TCC {TccId}", requesterUserId, request.TccId);
            return ResultPattern<string>.FailureResult("Apenas o orientador atual do TCC pode solicitar o seu afastamento.", 403);
        }

        if (request.SuggestedAdvisorId == requesterUserId)
        {
            return ResultPattern<string>.FailureResult("O orientador substituto não pode ser o próprio solicitante.", 400);
        }

        var suggestedAdvisor = await userGateway.FindById(request.SuggestedAdvisorId);
        if (suggestedAdvisor is null)
        {
            return ResultPattern<string>.FailureResult("Orientador substituto sugerido não encontrado.", 404);
        }

        try
        {
            var docTypes = await documentTypeGateway.FindAll();
            var targetDocType = docTypes.FirstOrDefault(dt =>
                dt.Name.Contains("ANEXO V -") || dt.Name.Contains("AFASTAMENTO"));

            Domain.Entities.Document.DocumentEntity? generatedDocument = null;
            if (targetDocType != null)
            {
                generatedDocument = DocumentFactory.CreateDocument(targetDocType, tcc.Title ?? string.Empty, null);
                generatedDocument.Tcc = tcc;
                generatedDocument.TccId = tcc.Id;
                tcc.Documents.Add(generatedDocument);
                await tccGateway.Update(tcc);
            }

            var advisorDeparture = new TccAdvisorChangeEntity
            {
                TccId = tcc.Id,
                RequesterUserId = requesterUserId,
                RequesterRole = "ADVISOR",
                CurrentAdvisorId = currentAdvisorUt.UserId,
                SuggestedAdvisorId = request.SuggestedAdvisorId,
                Reason = request.Reason.Trim(),
                Status = "PENDING_SIGNATURES",
                DocumentId = generatedDocument?.Id,
                CreatedAt = DateTime.UtcNow
            };

            await tccGateway.SaveAdvisorChange(advisorDeparture);

            if (targetDocType != null)
            {
                var advisorUser = currentAdvisorUt.User ?? await userGateway.FindById(requesterUserId);
                if (advisorUser != null)
                {
                    try
                    {
                        var details = new List<SendPendingSignatureDetailsDTO> { new SendPendingSignatureDetailsDTO(targetDocType.Name, null) };
                        var emailDto = EmailFactory.CreateSendEmailDTO(new SendPendingSignatureDTO(advisorUser.Email, advisorUser.Name, details, tcc.Title ?? string.Empty));
                        await emailGateway.Send(emailDto);
                        logger.LogInformation("E-mail de notificação de assinatura de afastamento enviado para o orientador {Email}", advisorUser.Email);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Erro ao enviar e-mail de notificação de afastamento para o orientador {Email}", advisorUser.Email);
                    }
                }
            }

            logger.LogInformation("Solicitação de afastamento criada com sucesso para TCC {TccId}. ChangeId: {ChangeId}, DocumentId: {DocId}", tcc.Id, advisorDeparture.Id, generatedDocument?.Id);
            return ResultPattern<string>.SuccessResult("Solicitação de afastamento enviada com sucesso.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao registrar solicitação de afastamento para TccId {TccId}", request.TccId);
            return ResultPattern<string>.FailureResult("Erro interno ao registrar solicitação de afastamento.", 500);
        }
    }
}
