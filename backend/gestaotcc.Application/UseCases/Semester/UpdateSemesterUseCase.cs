using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Semester;
using gestaotcc.Domain.Entities.Semester;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Semester;

public class UpdateSemesterUseCase(ISemesterGateway semesterGateway, IAppLoggerGateway<UpdateSemesterUseCase> logger)
{
    public async Task<ResultPattern<SemesterEntity>> Execute(long id, CreateSemesterDTO dto)
    {
        logger.LogInformation($"Iniciando atualização do semestre {id}");

        var semester = await semesterGateway.FindById(id);
        if (semester == null)
        {
            return ResultPattern<SemesterEntity>.FailureResult("Semestre não encontrado.", 404);
        }

        semester.Update(dto.Name, dto.StartDate, dto.EndDate, dto.IsActive);
        var result = await semesterGateway.Update(semester);

        logger.LogInformation("Semestre atualizado com sucesso");
        return ResultPattern<SemesterEntity>.SuccessResult(result);
    }
}
