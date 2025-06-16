using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class CreateScheduleTccUseCase(ITccGateway tccGateway)
{
    public async Task<ResultPattern<string>> Execute(ScheduleTccDTO data)
    {
        var tcc = await tccGateway.FindTccScheduling(data.IdTcc);
        if (tcc is null)
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        if (tcc.TccSchedule is not null)
            return ResultPattern<string>.FailureResult("TCC já possui agendamento de defesa", 409);

        try
        {
            var tccSchedule = TccScheduleFactory.CreateTccSchedule(data);
            tcc.TccSchedule = tccSchedule;
            await tccGateway.Update(tcc);
        }
        catch (Exception)
        {
            return ResultPattern<string>.FailureResult("Erro ao criar agendamento de defesa do TCC", 500);
        }
        return ResultPattern<string>.SuccessResult("Agendamento de defesa do TCC criado com sucesso.");
    }
}
