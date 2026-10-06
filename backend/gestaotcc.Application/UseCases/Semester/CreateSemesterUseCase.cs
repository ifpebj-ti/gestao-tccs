using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Semester;
using gestaotcc.Domain.Entities.Semester;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Semester;

public class CreateSemesterUseCase(ISemesterGateway semesterGateway, IAppLoggerGateway<CreateSemesterUseCase> logger)
{
    public async Task<ResultPattern<SemesterEntity>> Execute(CreateSemesterDTO dto)
    {
        logger.LogInformation("Iniciando a criação do semestre");

        var semester = new SemesterEntity(0, dto.Name, dto.StartDate, dto.EndDate, dto.IsActive);
        var result = await semesterGateway.Insert(semester);

        logger.LogInformation("Semestre criado com sucesso");
        return ResultPattern<SemesterEntity>.SuccessResult(result);
    }
}
