using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;
public class FindUserByIdUseCase(IUserGateway userGateway)
{
    public async Task<ResultPattern<UserEntity>> Execute(long id)
    {
        var user = await userGateway.FindById(id);
        if (user is null)
            return ResultPattern<UserEntity>.FailureResult("Erro ao buscar o usuário. Por favor verifique as informações e tente novamente", 404);
        return ResultPattern<UserEntity>.SuccessResult(user);
    }
}
