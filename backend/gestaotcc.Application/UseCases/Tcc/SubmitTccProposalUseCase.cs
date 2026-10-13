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
    ISemesterGateway semesterGateway,
    IAppLoggerGateway<SubmitTccProposalUseCase> logger)
{
    public async Task<ResultPattern<long>> Execute(SubmitTccProposalDTO data, long studentUserId)
    {
        logger.LogInformation("Iniciando submissÃ£o de proposta de TCC pelo estudante UserId: {StudentId}, OrientadorId: {AdvisorId}", studentUserId, data.AdvisorId);

        var student = await userGateway.FindById(studentUserId);
        if (student is null)
        {
            logger.LogWarning("Estudante com Id {StudentId} nÃ£o encontrado.", studentUserId);
            return ResultPattern<long>.FailureResult("Estudante nÃ£o encontrado", 404);
        }

        var isStudent = student.Profile.Any(p => p.Role == RoleType.STUDENT.ToString());
        if (!isStudent)
        {
            logger.LogWarning("UsuÃ¡rio com Id {StudentId} nÃ£o possui perfil de estudante.", studentUserId);
            return ResultPattern<long>.FailureResult("UsuÃ¡rio nÃ£o possui permissÃ£o de discente", 403);
        }

        var advisor = await userGateway.FindById(data.AdvisorId);
        if (advisor is null)
        {
            logger.LogWarning("Orientador com Id {AdvisorId} nÃ£o encontrado.", data.AdvisorId);
            return ResultPattern<long>.FailureResult("Orientador nÃ£o encontrado", 404);
        }

        var isAdvisor = advisor.Profile.Any(p => p.Role == RoleType.ADVISOR.ToString() || p.Role == RoleType.COORDINATOR.ToString() || p.Role == RoleType.SUPERVISOR.ToString());
        if (!isAdvisor)
        {
            logger.LogWarning("UsuÃ¡rio com Id {AdvisorId} nÃ£o possui perfil de orientador.", data.AdvisorId);
            return ResultPattern<long>.FailureResult("O usuÃ¡rio selecionado nÃ£o Ã© um orientador vÃ¡lido", 400);
        }

        var existingTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(studentUserId, null, null), 0);
        var hasActiveProposalOrTcc = existingTccs.Any(t => 
            t.Status == StatusTccType.IN_PROGRESS.ToString() || 
            t.Status == StatusTccType.PENDING_APPROVAL.ToString());

        if (hasActiveProposalOrTcc)
        {
            logger.LogWarning("Estudante UserId: {StudentId} jÃ¡ possui TCC em andamento ou proposta pendente.", studentUserId);
            return ResultPattern<long>.FailureResult("VocÃª jÃ¡ possui um TCC em andamento ou uma proposta pendente de aprovaÃ§Ã£o.", 409);
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

        // Vincula a proposta ao semestre letivo ativo
        var activeSemester = (await semesterGateway.FindActive()).FirstOrDefault();
        if (activeSemester is null)
        {
            logger.LogWarning("Nenhum semestre letivo ativo encontrado ao submeter proposta de TCC.");
            return ResultPattern<long>.FailureResult("Nenhum semestre letivo ativo encontrado. Cadastre um semestre ativo antes de submeter uma proposta.", 422);
        }
        tcc.SemesterId = activeSemester.Id;
        tcc.Semester = activeSemester;

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
        await tccGateway.UpdateSemesterId(tcc.Id, activeSemester.Id);
        logger.LogInformation("Proposta de TCC {TccId} criada com sucesso vinculada ao semestre {SemesterId} e aguardando aprovaÃ§Ã£o do orientador {AdvisorId}.", tcc.Id, activeSemester.Id, advisor.Id);

        try
        {
            var emailDto = EmailFactory.CreateSendEmailDTO(advisor, "ADD-USER-TCC");
            await emailGateway.Send(emailDto);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Erro ao enviar notificaÃ§Ã£o de proposta para o orientador {AdvisorEmail}: {ErrorMessage}", advisor.Email, ex.Message);
        }

        return ResultPattern<long>.SuccessResult(tcc.Id);
    }
}




