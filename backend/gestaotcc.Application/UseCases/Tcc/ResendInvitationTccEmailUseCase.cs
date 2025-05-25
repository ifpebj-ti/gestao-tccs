using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ResendInvitationTccEmailUseCase(IUserGateway userGateway, ITccGateway tccGateway, IEmailGateway emailGateway)
{
    public async Task<ResultPattern<bool>> Execute()
    {
        var tccInvites = await tccGateway.FindAllInviteTcc();
        
        foreach (var item in tccInvites)
        {
            var emailDto = EmailFactory.CreateSendEmailDTO(item, "RESEND-INVITE-TCC");
            await emailGateway.Send(emailDto);
        }

        return ResultPattern<bool>.SuccessResult();
    }
}