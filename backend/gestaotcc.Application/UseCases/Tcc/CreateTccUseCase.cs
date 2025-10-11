using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class CreateTccUseCase(IUserGateway userGateway, ITccGateway tccGateway, IEmailGateway emailGateway, IDocumentTypeGateway documentTypeGateway, IAppLoggerGateway<CreateTccUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(CreateTccDTO data)
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
        var tcc = TccFactory.CreateTcc(data, usersNotInvite, usersInviteEmails, documentTypes, advisor.CampiCourse!.CampiId);

        logger.LogInformation("Salvando nova entidade TCC no banco de dados...");
        await tccGateway.Save(tcc);
        logger.LogInformation("Entidade TCC salva com sucesso.");
        
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