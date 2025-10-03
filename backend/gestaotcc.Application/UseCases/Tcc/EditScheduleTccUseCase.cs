using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class EditScheduleTccUseCase(ITccGateway tccGateway, IAppLoggerGateway<EditScheduleTccUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(ScheduleTccDTO data)
    {
        logger.LogInformation("Iniciando edição de agendamento de defesa para o TccId: {TccId}", data.IdTcc);

        var tcc = await tccGateway.FindTccScheduling(data.IdTcc);
        if (tcc is null)
        {
            logger.LogWarning("Falha na edição de agendamento: TCC não encontrado para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (tcc.TccSchedule is null)
        {
            logger.LogWarning("Falha na edição de agendamento para TccId {TccId}: TCC não possui um agendamento para editar.", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC ainda não possui agendamento de defesa", 409);
        }
        try
        {
            logger.LogInformation("Agendamento atual para TccId {TccId}: Data: {CurrentDate}, Local: {CurrentLocation}", data.IdTcc, tcc.TccSchedule.ScheduledDate, tcc.TccSchedule.Location);

            if (data.ScheduleDate is not null && data.ScheduleTime is not null)
            {
                var newDateTime = DateTime.SpecifyKind(data.ScheduleDate.Value.ToDateTime(data.ScheduleTime.Value), DateTimeKind.Utc);
                logger.LogInformation("Atualizando data do agendamento para: {NewDateTime}", newDateTime);
                tcc.TccSchedule.ScheduledDate = newDateTime;
            }

            if (data.ScheduleLocation is not null)
            {
                logger.LogInformation("Atualizando local do agendamento para: {NewLocation}", data.ScheduleLocation);
                tcc.TccSchedule.Location = data.ScheduleLocation;
            }
            
            logger.LogInformation("Salvando alterações do agendamento para o TccId: {TccId}", data.IdTcc);
            await tccGateway.Update(tcc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro de banco de dados ao tentar editar o agendamento de defesa para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("Erro ao editar agendamento de defesa do TCC", 500);
        }
        
        logger.LogInformation("Agendamento de defesa do TCC {TccId} editado com sucesso.", data.IdTcc);
        return ResultPattern<string>.SuccessResult("Agendamento de defesa do TCC editado com sucesso.");
    }
}