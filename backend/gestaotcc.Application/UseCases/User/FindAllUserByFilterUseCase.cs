using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;

public class FindAllUserByFilterUseCase(IUserGateway userGateway)
{
    public async Task<ResultPattern<List<FindAllUserByFilterDTO>>> Execute(UserFilterDTO data)
    {
        var users = await userGateway.FindAllByFilter(data);
        return ResultPattern<List<FindAllUserByFilterDTO>>.SuccessResult(users.Select(UserFactory.CreateFindAllUserByFilterDTO).ToList());
    }
}