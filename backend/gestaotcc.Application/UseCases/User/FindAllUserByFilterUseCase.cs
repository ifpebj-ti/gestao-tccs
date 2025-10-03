using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;

public class FindAllUserByFilterUseCase(IUserGateway userGateway, IAppLoggerGateway<FindAllUserByFilterUseCase> logger)
{
    public async Task<ResultPattern<List<FindAllUserByFilterDTO>>> Execute(UserFilterDTO data)
    {
        logger.LogInformation("Iniciando busca de usuários com filtros {@Filtro}", data);

        var users = await userGateway.FindAllByFilter(data);
        var result = users.Select(UserFactory.CreateFindAllUserByFilterDTO).ToList();

        logger.LogInformation("Busca concluída. {Quantidade} usuários encontrados", result.Count);

        return ResultPattern<List<FindAllUserByFilterDTO>>.SuccessResult(result);
    }
}