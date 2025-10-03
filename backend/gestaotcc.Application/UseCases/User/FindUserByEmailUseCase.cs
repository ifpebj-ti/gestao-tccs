﻿using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;
public class FindUserByEmailUseCase(IUserGateway userGateway, IAppLoggerGateway<FindUserByEmailUseCase> logger)
{
    public async Task<ResultPattern<UserEntity>> Execute(string email)
    {
        logger.LogInformation("Iniciando busca de usuário pelo e-mail: {UserEmail}", email);

        var user = await userGateway.FindByEmail(email.ToLower());
        if (user is null)
        {
            logger.LogWarning("Nenhum usuário encontrado para o e-mail: {UserEmail}", email);
            return ResultPattern<UserEntity>.FailureResult("Erro ao buscar o usuário. Por favor verifique as informações e tente novamente", 404);
        }

        logger.LogInformation("Usuário encontrado com sucesso. UserId: {UserId}, UserEmail: {UserEmail}", user.Id, user.Email);
        return ResultPattern<UserEntity>.SuccessResult(user);
    }
}