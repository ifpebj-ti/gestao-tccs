using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class CreateTccUseCase(IUserGateway userGateway, ITccGateway tccGateway, IEmailGateway emailGateway, IDocumentTypeGateway documentTypeGateway, ISemesterGateway semesterGateway, IAppLoggerGateway<CreateTccUseCase> logger, IProposalGateway? proposals = null)
{
    public Task<ResultPattern<string>> Execute(CreateTccDTO data) =>
        proposals == null ? Create(data) : proposals.Atomic(() => Create(data));

    private async Task<ResultPattern<string>> Create(CreateTccDTO data)
    {
        logger.LogInformation("Iniciando criação de TCC. Título: {TccTitle}, OrientadorId: {AdvisorId}, Alunos: {StudentCount}", data.Title, data.AdvisorId, data.Students.Count);
        var studentEmails = data.Students.Select(s => s.StudentEmail).ToList();
            
        var users = await userGateway.FindAllByEmail(studentEmails);
        var advisor = await userGateway.FindById(data.AdvisorId);
        if (advisor is null)
        {
            logger.LogWarning("Falha na criação do TCC: Orientador com Id {AdvisorId} não encontrado.", data.AdvisorId);
            return ResultPattern<string>.FailureResult("Erro ao criar tcc", 404);
        }
        
        foreach (var student in users)
        {
            var existing = await tccGateway.FindAllTccByFilter(new TccFilterDTO(student.Id, null), 0);
            if (existing != null && existing.Any(ProposalParticipantsUseCase.Active))
                return ResultPattern<string>.FailureResult("Um estudante já possui proposta ou TCC ativo.", 409);
        }
        if (users.Count > 0 && users.Any(u => u.CampiCourse!.Id != advisor!.CampiCourse!.Id))
        {
            logger.LogWarning("Falha na criação do TCC: Orientador com Id {AdvisorId} e orientandos de campus diferentes.", data.AdvisorId);
            return ResultPattern<string>.FailureResult("Erro ao criar tcc", 404);
        }
        
        logger.LogInformation("Orientador encontrado: {AdvisorName} (Id: {AdvisorId}). {ExistingStudentCount} de {TotalStudentCount} alunos encontrados no sistema.", advisor.Name, advisor.Id, users.Count, data.Students.Count);
        var documentTypes = await documentTypeGateway.FindAll();
        var allEmails = users.Select(u => u.Email).ToHashSet();
        
        var usersInviteEmails = data.Students
            .Where(student => !allEmails.Contains(student.StudentEmail))
            .ToList();
        
        var usersNotInviteEmails = studentEmails
            .Where(email => allEmails.Contains(email))
            .ToList();
        
        logger.LogInformation("Segregação de usuários concluída. Existentes: {ExistingCount}, A Convidar: {InviteCount}", usersNotInviteEmails.Count, usersInviteEmails.Count);
        
        var usersNotInvite = users
            .Where(u => usersNotInviteEmails.Contains(u.Email))
            .ToList();
        
        usersNotInvite.Add(advisor);
        
        logger.LogInformation("Criando entidade TCC via factory...");
        var activeSemesters = await semesterGateway.FindActive();
        var activeSemester = activeSemesters.FirstOrDefault();
        if (activeSemester is null)
        {
            logger.LogWarning("Nenhum semestre letivo ativo encontrado. O TCC não pode ser criado sem um semestre ativo.");
            return ResultPattern<string>.FailureResult("Nenhum semestre letivo ativo encontrado. Cadastre um semestre ativo antes de criar um TCC.", 422);
        }

        var tcc = TccFactory.CreateTcc(data, usersNotInvite, usersInviteEmails, documentTypes, advisor.CampiCourse!.CampiId, activeSemester.Id);

        // Vincula a navegação de forma explícita para que o EF Core persista a FK com segurança
        tcc.Semester = activeSemester;
        logger.LogInformation("TCC vinculado ao semestre ativo: {SemesterName} (Id: {SemesterId})", activeSemester.Name, activeSemester.Id);

        logger.LogInformation("Salvando nova entidade TCC no banco de dados...");
        await tccGateway.Save(tcc);
        logger.LogInformation("Entidade TCC salva com sucesso. TCC Id: {TccId}", tcc.Id);

        // Garantia adicional: atualiza a FK diretamente no banco caso o rastreamento
        // do EF Core não tenha persistido a relação com o semestre.
        await tccGateway.UpdateSemesterId(tcc.Id, activeSemester.Id);
        logger.LogInformation("SemesterId {SemesterId} confirmado para TCC Id {TccId}.", activeSemester.Id, tcc.Id);

        
        logger.LogInformation("Enviando e-mails de notificação para {UserCount} usuários existentes...", tcc.UserTccs.Count);
        foreach (var item in tcc.UserTccs)
        {
            logger.LogDebug("Enviando e-mail 'ADD-USER-TCC' para o usuário: {UserEmail} (UserId: {UserId})", item.User.Email, item.User.Id);
            var emailDto = EmailFactory.CreateSendEmailDTO(item.User, "ADD-USER-TCC");
            await emailGateway.Send(emailDto);
        }
        
        if (usersInviteEmails.Count > 0)
        {
            logger.LogInformation("Enviando e-mails de convite para {UserCount} novos usuários...", tcc.TccInvites.Count);
            var emailDtoList = tcc.TccInvites.Select(x => EmailFactory.CreateSendEmailDTO(x, "INVITE-USER")).ToList();

            foreach (var sendEmailDto in emailDtoList)
            {
                logger.LogDebug("Enviando e-mail 'INVITE-USER' para o endereço: {UserEmail}", sendEmailDto.Recipient);
                await emailGateway.Send(sendEmailDto);
            }
        }

        logger.LogInformation("Criação do TCC e envio de notificações concluídos com sucesso.");
        return ResultPattern<string>.SuccessResult();
    }
}
