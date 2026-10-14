using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.Profile;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class SubmitTccProposalUseCase(
    IUserGateway userGateway,
    ITccGateway tccGateway,
    IProfileGateway profileGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<SubmitTccProposalUseCase> logger,
    IProposalGateway? proposals = null)
{
    public async Task<ResultPattern<long>> Execute(SubmitTccProposalDTO data, long studentUserId)
    {
        var notifications = new List<gestaotcc.Domain.Dtos.Email.SendEmailDTO>();
        var result = proposals == null ? await Submit(data, studentUserId, notifications)
            : await proposals.Atomic(() => Submit(data, studentUserId, notifications));
        // Delivery happens after the commit, so SMTP does not hold the proposal transaction lock.
        if (result.IsSuccess)
            foreach (var notification in notifications)
            {
                try
                {
                    var delivery = await emailGateway.Send(notification);
                    if (delivery?.IsFailure == true) logger.LogWarning("Falha ao entregar notificação da proposta {TccId}.", result.Data);
                }
                catch (Exception) { logger.LogWarning("Falha ao entregar notificação da proposta {TccId}.", result.Data); }
            }
        return result;
    }

    private async Task<ResultPattern<long>> Submit(SubmitTccProposalDTO data, long studentUserId, List<gestaotcc.Domain.Dtos.Email.SendEmailDTO> notifications)
    {
        logger.LogInformation("Iniciando submissão de proposta de TCC pelo estudante UserId: {StudentId}, OrientadorId: {AdvisorId}", studentUserId, data.AdvisorId);

        var student = await userGateway.FindById(studentUserId);
        if (student is null)
        {
            logger.LogWarning("Estudante com Id {StudentId} não encontrado.", studentUserId);
            return ResultPattern<long>.FailureResult("Estudante não encontrado", 404);
        }

        var isStudent = student.Profile.Any(p => p.Role == RoleType.STUDENT.ToString());
        if (!isStudent)
        {
            logger.LogWarning("Usuário com Id {StudentId} não possui perfil de estudante.", studentUserId);
            return ResultPattern<long>.FailureResult("Usuário não possui permissão de discente", 403);
        }

        var advisor = await userGateway.FindById(data.AdvisorId);
        if (advisor is null)
        {
            logger.LogWarning("Orientador com Id {AdvisorId} não encontrado.", data.AdvisorId);
            return ResultPattern<long>.FailureResult("Orientador não encontrado", 404);
        }

        var isAdvisor = advisor.Profile.Any(p => p.Role == RoleType.ADVISOR.ToString() || p.Role == RoleType.COORDINATOR.ToString() || p.Role == RoleType.SUPERVISOR.ToString());
        if (!isAdvisor)
        {
            logger.LogWarning("Usuário com Id {AdvisorId} não possui perfil de orientador.", data.AdvisorId);
            return ResultPattern<long>.FailureResult("O usuário selecionado não é um orientador válido", 400);
        }

        var existingTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(studentUserId, null), 0);
        var hasActiveProposalOrTcc = existingTccs.Any(ProposalParticipantsUseCase.Active);

        if (hasActiveProposalOrTcc)
        {
            logger.LogWarning("Estudante UserId: {StudentId} já possui TCC em andamento ou proposta pendente.", studentUserId);
            return ResultPattern<long>.FailureResult("Você já possui um TCC em andamento ou uma proposta pendente de aprovação.", 409);
        }

        var studentProfile = student.Profile.First(p => p.Role == RoleType.STUDENT.ToString());
        var participantEmails = data.ParticipantEmails ?? [];
        if (participantEmails.Count > 3 || participantEmails.Any(string.IsNullOrWhiteSpace) ||
            participantEmails.Select(x => x.Trim().ToLowerInvariant()).Distinct().Count() != participantEmails.Count)
            return ResultPattern<long>.FailureResult("Informe até três participantes distintos.", 400);
        var participants = new List<(string Email, UserEntity? User)>();
        foreach (var email in participantEmails)
        {
            var participant = await userGateway.FindProposalParticipantByEmail(email.Trim().ToLowerInvariant());
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (!ProposalParticipantsUseCase.Eligible(student) || student.CampiCourseId == null ||
                normalizedEmail == student.Email.Trim().ToLowerInvariant() || participant?.Id == studentUserId)
                return ResultPattern<long>.FailureResult("Informe outro integrante e confira seu cadastro de curso.", 400);
            if (participant != null)
            {
                if (!ProposalParticipantsUseCase.Eligible(participant) || participant.CampiCourseId != student.CampiCourseId)
                    return ResultPattern<long>.FailureResult("Os integrantes devem ser estudantes ativos do mesmo campus e curso.", 400);
                var participantTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(participant.Id, null), 0);
                if (participantTccs.Any(ProposalParticipantsUseCase.Active))
                    return ResultPattern<long>.FailureResult("Um participante já possui TCC em andamento ou proposta pendente.", 409);
            }
            participants.Add((normalizedEmail, participant));
        }
        var advisorProfile = advisor.Profile.First(p => p.Role == RoleType.ADVISOR.ToString() || p.Role == RoleType.COORDINATOR.ToString() || p.Role == RoleType.SUPERVISOR.ToString());

        var tcc = new TccEntityBuilder()
            .WithTitle(data.Title)
            .WithSummary(data.Summary)
            .WithStatus(participants.Count == 0 ? "PENDING_APPROVAL" : "AWAITING_PARTICIPANTS")
            .WithStep(StepTccType.PROPOSAL_REGISTRATION.ToString())
            .WithCreationDate(DateTime.UtcNow)
            .Build();

        tcc.UserTccs = new List<UserTccEntity>
        {
            new UserTccEntityBuilder()
                .WithTcc(tcc)
                .WithUser(student)
                .WithProfile(studentProfile)
                .WithBindingDate(DateTime.UtcNow)
                .Build(),
            new UserTccEntityBuilder()
                .WithTcc(tcc)
                .WithUser(advisor)
                .WithProfile(advisorProfile)
                .WithBindingDate(DateTime.UtcNow)
                .Build()
        };

        tcc.ParticipantInvitations = participants.Select(x => new ProposalParticipantEntity { Tcc = tcc, Email = x.Email, User = x.User, UserId = x.User?.Id }).ToList();
        await tccGateway.Save(tcc);
        logger.LogInformation("Proposta de TCC {TccId} criada com sucesso e aguardando aprovação do orientador {AdvisorId}.", tcc.Id, advisor.Id);

        if (participants.Count == 0)
            notifications.Add(EmailFactory.CreateSendEmailDTO(advisor, "ADD-USER-TCC"));
        foreach (var participant in participants)
            notifications.Add(new("", "Convite para proposta de TCC", participant.Email, "PROPOSAL-PARTICIPANT", new()));

        return ResultPattern<long>.SuccessResult(tcc.Id);
    }
}
