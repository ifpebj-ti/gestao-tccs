using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class SendScheduleEmailUseCase(ITccGateway tccGateway, IEmailGateway emailGateway)
{
    public async Task<ResultPattern<string>> Execute(long tccId)
    {
        var tcc = await tccGateway.FindTccScheduling(tccId);
        if (tcc is null)
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        if (tcc.TccSchedule is null)
            return ResultPattern<string>.FailureResult("TCC não possui agendamento de defesa", 409);
        try
        {
            foreach (var userTcc in tcc.UserTccs)
            {
                var emailDto = EmailFactory.CreateSendEmailDTO(userTcc.User, tcc, tcc.TccSchedule);
                await emailGateway.Send(emailDto);
            }
        }
        catch (Exception)
        {
            return ResultPattern<string>.FailureResult("Erro ao enviar email de agendamento de defesa do TCC", 500);
        }
        return ResultPattern<string>.SuccessResult("Emails de agendamento de defesa do TCC enviados com sucesso.");
    }
}
