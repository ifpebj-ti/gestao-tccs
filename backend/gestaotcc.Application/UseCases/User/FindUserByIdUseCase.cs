using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;
public class FindUserByIdUseCase(IUserGateway userGateway, IAppLoggerGateway<FindUserByIdUseCase> logger)
{
    public async Task<ResultPattern<UserEntity>> Execute(long id)
    {
        logger.LogInformation("Iniciando busca de usuário pelo Id: {UserId}", id);

        var user = await userGateway.FindById(id);
        if (user is null)
        {
            logger.LogWarning("Nenhum usuário encontrado para o Id: {UserId}", id);
            return ResultPattern<UserEntity>.FailureResult("Erro ao buscar o usuário. Por favor verifique as informações e tente novamente", 404);
        }

        logger.LogInformation("Usuário encontrado com sucesso. UserId: {UserId}, UserEmail: {UserEmail}", user.Id, user.Email);
        return ResultPattern<UserEntity>.SuccessResult(user);
    }
}