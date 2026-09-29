using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.TccSchedule;

namespace gestaotcc.Application.UseCases.Tcc;

public class SaveScheduleInfoUseCase(ITccGateway tccGateway, IAppLoggerGateway<SaveScheduleInfoUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(long tccId, ScheduleTccDTO data)
    {
        logger.LogInformation("Iniciando salvamento de informacoes de agendamento para TccId: {TccId}", tccId);

        var tcc = await tccGateway.FindTccScheduling(tccId);
        if (tcc is null)
        {
            logger.LogWarning("TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }

        try
        {
            if (tcc.TccSchedule is not null)
            {
                if (data.ScheduleDate is not null && data.ScheduleTime is not null)
                {
                    var newDateTime = DateTime.SpecifyKind(data.ScheduleDate.Value.ToDateTime(data.ScheduleTime.Value), DateTimeKind.Utc);
                    tcc.TccSchedule.ScheduledDate = newDateTime;
                }

                if (!string.IsNullOrWhiteSpace(data.ScheduleLocation))
                {
                    tcc.TccSchedule.Location = data.ScheduleLocation;
                }
            }
            else
            {
                if (data.ScheduleDate is null || data.ScheduleTime is null || string.IsNullOrWhiteSpace(data.ScheduleLocation))
                {
                    return ResultPattern<string>.FailureResult("Data, horário e local são obrigatórios para o primeiro agendamento", 400);
                }
                
                var newDateTime = DateTime.SpecifyKind(data.ScheduleDate.Value.ToDateTime(data.ScheduleTime.Value), DateTimeKind.Utc);
                var tccSchedule = new TccScheduleEntity(0, newDateTime, data.ScheduleLocation, tcc.Id);
                tcc.TccSchedule = tccSchedule;
            }
            
            await tccGateway.Update(tcc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao tentar salvar info de agendamento para o TccId: {TccId}", tccId);
            return ResultPattern<string>.FailureResult("Erro ao salvar info de agendamento do TCC", 500);
        }
        
        return ResultPattern<string>.SuccessResult("Info de agendamento do TCC salva com sucesso.");
    }
}
