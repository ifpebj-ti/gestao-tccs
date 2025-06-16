using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class EditScheduleTccUseCase(ITccGateway tccGateway)
{
    public async Task<ResultPattern<string>> Execute(ScheduleTccDTO data)
    {
        var tcc = await tccGateway.FindTccScheduling(data.IdTcc);
        if (tcc is null)
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        if (tcc.TccSchedule is null)
            return ResultPattern<string>.FailureResult("TCC ainda não possui agendamento de defesa", 409);
        try
        {
            if (data.ScheduleDate is not null && data.ScheduleTime is not null)
                tcc.TccSchedule.ScheduledDate = DateTime.SpecifyKind(data.ScheduleDate.Value.ToDateTime(data.ScheduleTime.Value), DateTimeKind.Utc);
            if (data.ScheduleLocation is not null)
                tcc.TccSchedule.Location = data.ScheduleLocation;
            await tccGateway.Update(tcc);
        }
        catch (Exception)
        {
            return ResultPattern<string>.FailureResult("Erro ao editar agendamento de defesa do TCC", 500);
        }
        return ResultPattern<string>.SuccessResult("Agendamento de defesa do TCC editado com sucesso.");
    }
}
