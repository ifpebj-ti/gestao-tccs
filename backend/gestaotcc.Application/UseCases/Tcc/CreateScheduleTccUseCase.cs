using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;
public class CreateScheduleTccUseCase(ITccGateway tccGateway, IAppLoggerGateway<CreateScheduleTccUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(ScheduleTccDTO data)
    {
        logger.LogInformation("Iniciando criação de agendamento de defesa para o TccId: {TccId}. Data: {ScheduledDate}", data.IdTcc, data.ScheduleDate);

        var tcc = await tccGateway.FindTccScheduling(data.IdTcc);
        if (tcc is null)
        {
            logger.LogWarning("Falha na criação de agendamento: TCC não encontrado para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC não encontrado", 404);
        }
        if (tcc.TccSchedule is not null)
        {
            logger.LogWarning("Falha na criação de agendamento para TccId {TccId}: TCC já possui um agendamento.", data.IdTcc);
            return ResultPattern<string>.FailureResult("TCC já possui agendamento de defesa", 409);
        }

        try
        {
            logger.LogInformation("Criando e atribuindo agendamento para o TccId: {TccId}", data.IdTcc);
            var tccSchedule = TccScheduleFactory.CreateTccSchedule(data);
            tcc.TccSchedule = tccSchedule;
            await tccGateway.Update(tcc);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro de banco de dados ao tentar criar o agendamento de defesa para o TccId: {TccId}", data.IdTcc);
            return ResultPattern<string>.FailureResult("Erro ao criar agendamento de defesa do TCC", 500);
        }
        
        logger.LogInformation("Agendamento de defesa do TCC {TccId} criado com sucesso.", data.IdTcc);
        return ResultPattern<string>.SuccessResult("Agendamento de defesa do TCC criado com sucesso.");
    }
}