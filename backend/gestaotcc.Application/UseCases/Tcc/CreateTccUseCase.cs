using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class CreateTccUseCase(IUserGateway userGateway, ITccGateway tccGateway, IEmailGateway emailGateway, IDocumentTypeGateway documentTypeGateway)
{
    public async Task<ResultPattern<string>> Execute(CreateTccDTO data)
    {
        var users = await userGateway.FindAllByEmail(data.StudentEmails);
        var advisor = await userGateway.FindById(data.AdvisorId);
        if (advisor is null)
            return ResultPattern<string>.FailureResult("Erro ao criar tcc", 404);
        var documentTypes = await documentTypeGateway.FindAll();

        var allEmails = users.Select(u => u.Email).ToHashSet();

        var usersInviteEmails = data.StudentEmails
            .Where(email => !allEmails.Contains(email))
            .ToList();
        
        var usersNotInviteEmails = data.StudentEmails
            .Where(email => allEmails.Contains(email))
            .ToList();
        
        var usersNotInvite = users
            .Where(u => usersNotInviteEmails.Contains(u.Email))
            .ToList();
        
        usersNotInvite.Add(advisor);
        
        var tcc = TccFactory.CreateTcc(data, usersNotInvite, usersInviteEmails, documentTypes);

        await tccGateway.Save(tcc);
        
        // Enviando email para usuários existentes
        foreach (var item in tcc.UserTccs)
        {
            var emailDto = EmailFactory.CreateSendEmailDTO(item.User, "ADD-USER-TCC");
            await emailGateway.Send(emailDto);
        }
        
        if (usersInviteEmails.Count > 0)
        {
            var emailDtoList = tcc.TccInvites.Select(x => EmailFactory.CreateSendEmailDTO(x, "INVITE-USER")).ToList();

            // Enviando email para usuário inexistentes
            foreach (var sendEmailDto in emailDtoList)
            {
                await emailGateway.Send(sendEmailDto);
            }
        }

        return ResultPattern<string>.SuccessResult();
    }
}