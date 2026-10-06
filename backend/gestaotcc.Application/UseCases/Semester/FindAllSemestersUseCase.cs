using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Semester;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Semester;

public class FindAllSemestersUseCase(ISemesterGateway semesterGateway)
{
    public async Task<ResultPattern<List<SemesterDTO>>> Execute()
    {
        var semesters = await semesterGateway.FindAll();
        var dtoList = semesters.Select(s => new SemesterDTO(s.Id, s.Name, s.StartDate, s.EndDate, s.IsActive)).ToList();
        return ResultPattern<List<SemesterDTO>>.SuccessResult(dtoList);
    }
}
