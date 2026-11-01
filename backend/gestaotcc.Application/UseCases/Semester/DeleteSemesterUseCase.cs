using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.Semester;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Semester;

public class DeleteSemesterUseCase(ISemesterGateway semesterGateway, IAppLoggerGateway<DeleteSemesterUseCase> logger)
{
    public async Task<ResultPattern<bool>> Execute(long id)
    {
        logger.LogInformation($"Iniciando exclusão do semestre {id}");

        var semester = await semesterGateway.FindById(id);
        if (semester == null)
        {
            return ResultPattern<bool>.FailureResult("Semestre não encontrado.", 404);
        }

        // TODO: Verificar se há TCCs vinculados antes de deletar, ou deixar o SetNull do EF agir.
        await semesterGateway.Delete(id);

        logger.LogInformation("Semestre excluído com sucesso");
        return ResultPattern<bool>.SuccessResult(true);
    }
}
