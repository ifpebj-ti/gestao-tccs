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
    IAppLoggerGateway<SubmitTccProposalUseCase> logger)
{
    public async Task<ResultPattern<long>> Execute(SubmitTccProposalDTO data, long studentUserId)
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
        var hasActiveProposalOrTcc = existingTccs.Any(t => 
            t.Status == StatusTccType.IN_PROGRESS.ToString() || 
            t.Status == StatusTccType.PENDING_APPROVAL.ToString());

        if (hasActiveProposalOrTcc)
        {
            logger.LogWarning("Estudante UserId: {StudentId} já possui TCC em andamento ou proposta pendente.", studentUserId);
            return ResultPattern<long>.FailureResult("Você já possui um TCC em andamento ou uma proposta pendente de aprovação.", 409);
        }

        var studentProfile = student.Profile.First(p => p.Role == RoleType.STUDENT.ToString());
        var advisorProfile = advisor.Profile.First(p => p.Role == RoleType.ADVISOR.ToString() || p.Role == RoleType.COORDINATOR.ToString() || p.Role == RoleType.SUPERVISOR.ToString());

        var tcc = new TccEntityBuilder()
            .WithTitle(data.Title)
            .WithSummary(data.Summary)
            .WithStatus(StatusTccType.PENDING_APPROVAL.ToString())
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

        await tccGateway.Save(tcc);
        logger.LogInformation("Proposta de TCC {TccId} criada com sucesso e aguardando aprovação do orientador {AdvisorId}.", tcc.Id, advisor.Id);

        try
        {
            var emailDto = EmailFactory.CreateSendEmailDTO(advisor, "ADD-USER-TCC");
            await emailGateway.Send(emailDto);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Erro ao enviar notificação de proposta para o orientador {AdvisorEmail}: {ErrorMessage}", advisor.Email, ex.Message);
        }

        return ResultPattern<long>.SuccessResult(tcc.Id);
    }
}
