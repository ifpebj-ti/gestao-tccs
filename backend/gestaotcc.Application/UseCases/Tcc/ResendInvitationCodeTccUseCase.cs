using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ResendInvitationCodeTccUseCase(IEmailGateway emailGateway, ITccGateway tccGateway)
{
    public async Task<ResultPattern<string>> Execute(string userEmail, long tccId)
    {
        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
            return ResultPattern<string>.FailureResult("Erro ao gerar novo código de acesso", 404);
        
        var userInvite = tcc.TccInvites.FirstOrDefault(x => x.Email == userEmail);
        if (userInvite is null)
            return ResultPattern<string>.FailureResult("Erro ao gerar novo código de acesso", 404);
        
        var random = new Random();
        
        var chars = "ABCDEFGHIJKLMONPQRSTUVWXYZ123456789";
        var code = new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        
        userInvite.Code = code;
        userInvite.IsValidCode = true;

        await tccGateway.UpdateTccInvite(userInvite);

        var emailDto = EmailFactory.CreateSendEmailDTO(userInvite, "INVITE-USER");
        await emailGateway.Send(emailDto);
        
        return ResultPattern<string>.SuccessResult();
    }
}