using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;

public class FindAvailableAdvisorsUseCase(
    IUserGateway userGateway,
    IAppLoggerGateway<FindAvailableAdvisorsUseCase> logger
)
{
    public async Task<ResultPattern<List<FindAllUserByFilterDTO>>> Execute(long campiCourseId)
    {
        logger.LogInformation("Buscando docentes disponíveis para o campus {CampiCourseId}", campiCourseId);

        var filter = new UserFilterDTO(null, null, null, RoleType.ADVISOR.ToString());
        var users = await userGateway.FindAllByFilter(filter, campiCourseId);

        var result = users.Select(UserFactory.CreateFindAllUserByFilterDTO).ToList();

        logger.LogInformation("{Quantidade} docentes disponíveis encontrados para o campus {CampiCourseId}", result.Count, campiCourseId);
        return ResultPattern<List<FindAllUserByFilterDTO>>.SuccessResult(result);
    }
}
