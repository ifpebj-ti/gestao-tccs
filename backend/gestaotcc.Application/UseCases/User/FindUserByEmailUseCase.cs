using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gestaotcc.Application.UseCases.User;
public class FindUserByEmailUseCase(IUserGateway userGateway)
{
    public async Task<ResultPattern<UserEntity>> Execute(string email)
    {
        var user = await userGateway.FindByEmail(email.ToLower());
        if (user is null)
            return ResultPattern<UserEntity>.FailureResult("Erro ao buscar o usuário. Por favor verifique as informações e tente novamente", 404);
        return ResultPattern<UserEntity>.SuccessResult(user);
    }
}
