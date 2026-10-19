using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ApproveAdvisorChangeUseCase(
    ITccGateway tccGateway, IUserGateway userGateway, IEmailGateway emailGateway,
    IAppLoggerGateway<ApproveAdvisorChangeUseCase> logger)
{
    public async Task<ResultPattern<bool>> Execute(long tccId, long coordinatorId)
    {
        if (tccId <= 0 || coordinatorId <= 0) return ResultPattern<bool>.FailureResult("Parâmetros inválidos.", 400);
        var coordinator = await userGateway.FindById(coordinatorId);
        if (coordinator == null || coordinator.Status != "ACTIVE") return ResultPattern<bool>.FailureResult("Usuário não autorizado ou inativo.", 401);
        if (!coordinator.Profile.Any(p => p.Role == "COORDINATOR")) return ResultPattern<bool>.FailureResult("Apenas a Coordenação pode homologar a substituição.", 403);

        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc == null) return ResultPattern<bool>.FailureResult("TCC não encontrado.", 404);
        var courseIds = tcc.UserTccs.Where(x => x.Profile.Role == "STUDENT").Select(x => x.User.CampiCourseId).Distinct().ToList();
        if (!coordinator.CampiCourseId.HasValue || !courseIds.Contains(coordinator.CampiCourseId)) return ResultPattern<bool>.FailureResult("Acesso não autorizado para o curso deste TCC.", 403);

        var change = (await tccGateway.FindPendingAdvisorChange(tccId));
        if (change == null || change.Status != "PENDING_COORDINATION") return ResultPattern<bool>.FailureResult("Não há solicitação aguardando homologação.", 409);
        if (!change.SuggestedAdvisorId.HasValue) return ResultPattern<bool>.FailureResult("A solicitação não informa o novo orientador.", 409);
        var replacement = await userGateway.FindById(change.SuggestedAdvisorId.Value);
        if (replacement == null || replacement.Status != "ACTIVE" || !replacement.Profile.Any(p => p.Role == "ADVISOR"))
            return ResultPattern<bool>.FailureResult("O orientador indicado não está ativo ou não possui perfil de orientador.", 409);
        if (!replacement.CampiCourseId.HasValue || !courseIds.Contains(replacement.CampiCourseId))
            return ResultPattern<bool>.FailureResult("O novo orientador não pertence ao curso deste TCC.", 409);

        var oldLink = tcc.UserTccs.FirstOrDefault(x => x.UserId == change.CurrentAdvisorId && x.Profile.Role is "ADVISOR" or "COORDINATOR" or "SUPERVISOR");
        if (oldLink == null) return ResultPattern<bool>.FailureResult("O orientador atual não está vinculado ao TCC.", 409);
        tcc.UserTccs.Remove(oldLink);
        if (!tcc.UserTccs.Any(x => x.UserId == replacement.Id && x.Profile.Role == "ADVISOR"))
        {
            var advisorProfile = replacement.Profile.First(p => p.Role == "ADVISOR");
            tcc.UserTccs.Add(new UserTccEntity(replacement, tcc, advisorProfile, DateTime.UtcNow)
            { UserId = replacement.Id, TccId = tcc.Id, ProfileId = advisorProfile.Id });
        }
        change.Status = "APPROVED";
        change.ReviewedAt = DateTime.UtcNow;
        change.ReviewedByUserId = coordinatorId;
        change.ReviewNotes = null;
        await tccGateway.Update(tcc);

        try
        {
            var recipients = tcc.UserTccs.Select(x => x.User).Append(change.CurrentAdvisor).Append(replacement)
                .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Email)).DistinctBy(x => x!.Email, StringComparer.OrdinalIgnoreCase);
            foreach (var recipient in recipients)
            {
                var dto = new SendEmailDTO("", $"Substituição de orientação homologada - {tcc.Title}", recipient!.Email,
                    "ADVISOR-CHANGE-APPROVED", new Dictionary<string, object> {
                        ["recipientName"] = recipient.Name, ["tccTitle"] = tcc.Title ?? "",
                        ["requestType"] = change.RequesterRole == "ADVISOR" ? "afastamento do orientador" : "substituição de orientador",
                        ["oldAdvisorName"] = change.CurrentAdvisor?.Name ?? "Orientador anterior",
                        ["newAdvisorName"] = replacement.Name,
                        ["studentNames"] = string.Join(", ", tcc.UserTccs.Where(x => x.Profile.Role == "STUDENT").Select(x => x.User.Name))
                    });
                var sendResult = await emailGateway.Send(dto);
                if (sendResult.IsFailure)
                    logger.LogWarning("Falha ao enviar aviso de homologação para {RecipientEmail}: {Error}", recipient.Email, sendResult.ErrorDetails?.Detail ?? sendResult.Message);
            }
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao notificar homologação de substituição do TCC {TccId}.", tccId); }
        return ResultPattern<bool>.SuccessResult(true);
    }
}

public class RejectAdvisorChangeUseCase(
    ITccGateway tccGateway,
    IUserGateway userGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<RejectAdvisorChangeUseCase> logger)
{
    public async Task<ResultPattern<bool>> Execute(long tccId, long coordinatorId, RejectAdvisorChangeDTO dto)
    {
        if (tccId <= 0 || coordinatorId <= 0 || string.IsNullOrWhiteSpace(dto?.ReviewNotes) || dto.ReviewNotes.Trim().Length > 2000)
            return ResultPattern<bool>.FailureResult("Informe um parecer de recusa com até 2000 caracteres.", 400);
        var coordinator = await userGateway.FindById(coordinatorId);
        if (coordinator == null || coordinator.Status != "ACTIVE") return ResultPattern<bool>.FailureResult("Usuário não autorizado ou inativo.", 401);
        if (!coordinator.Profile.Any(p => p.Role == "COORDINATOR")) return ResultPattern<bool>.FailureResult("Apenas a Coordenação pode recusar a substituição.", 403);
        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc == null) return ResultPattern<bool>.FailureResult("TCC não encontrado.", 404);
        var courseIds = tcc.UserTccs.Where(x => x.Profile.Role == "STUDENT").Select(x => x.User.CampiCourseId).Distinct().ToList();
        if (!coordinator.CampiCourseId.HasValue || !courseIds.Contains(coordinator.CampiCourseId)) return ResultPattern<bool>.FailureResult("Acesso não autorizado para o curso deste TCC.", 403);
        var change = await tccGateway.FindPendingAdvisorChange(tccId);
        if (change == null || change.Status != "PENDING_COORDINATION") return ResultPattern<bool>.FailureResult("Não há solicitação aguardando homologação.", 409);
        change.Status = "REJECTED";
        change.ReviewNotes = dto.ReviewNotes.Trim();
        change.ReviewedAt = DateTime.UtcNow;
        change.ReviewedByUserId = coordinatorId;
        await tccGateway.UpdateAdvisorChange(change);

        try
        {
            var studentNames = string.Join(", ", tcc.UserTccs
                .Where(link => link.Profile.Role == "STUDENT")
                .Select(link => link.User.Name ?? link.User.Email ?? "Estudante"));
            var requesterRole = change.RequesterRole == "ADVISOR" ? "afastamento do orientador" : "substituição de orientador";
            var recipients = tcc.UserTccs.Select(link => link.User)
                .Append(change.RequesterUser)
                .Append(change.CurrentAdvisor)
                .Append(change.SuggestedAdvisor)
                .Where(recipient => recipient != null && !string.IsNullOrWhiteSpace(recipient.Email))
                .DistinctBy(recipient => recipient!.Email, StringComparer.OrdinalIgnoreCase);

            foreach (var recipient in recipients)
            {
                var emailDto = new SendEmailDTO(
                    "",
                    $"Solicitação de {requesterRole} recusada - {tcc.Title}",
                    recipient!.Email,
                    "ADVISOR-CHANGE-REJECTED",
                    new Dictionary<string, object>
                    {
                        ["recipientName"] = recipient.Name ?? "",
                        ["tccTitle"] = tcc.Title ?? "",
                        ["requestType"] = requesterRole,
                        ["requesterName"] = change.RequesterUser?.Name ?? "",
                        ["currentAdvisorName"] = change.CurrentAdvisor?.Name ?? "",
                        ["suggestedAdvisorName"] = change.SuggestedAdvisor?.Name ?? "Não informado",
                        ["studentNames"] = studentNames,
                        ["reviewNotes"] = change.ReviewNotes ?? ""
                    });
                try
                {
                    var sendResult = await emailGateway.Send(emailDto);
                    if (sendResult.IsFailure)
                        logger.LogWarning("Falha ao enviar aviso de recusa para {RecipientEmail}: {Error}", recipient.Email, sendResult.ErrorDetails?.Detail ?? sendResult.Message);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao enviar aviso de recusa para {RecipientEmail}.", recipient.Email);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao notificar recusa da solicitação de alteração do orientador do TCC {TccId}.", tccId);
        }
        return ResultPattern<bool>.SuccessResult(true);
    }
}
