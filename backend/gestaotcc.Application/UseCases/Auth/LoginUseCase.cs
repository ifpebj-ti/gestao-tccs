using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class LoginUseCase(
    IUserGateway userGateway,
    IBcryptGateway bcryptGateway,
    ITokenGateway tokenGateway,
    IAppLoggerGateway<LoginUseCase> logger)
{
    public async Task<ResultPattern<TokenDTO>> Execute(string userCpf)
    {
        logger.LogInformation("Iniciando tentativa de login para o e-mail: {UserCpf}", userCpf);

        var user = await userGateway.FindByCpf(userCpf);
        if (user is null || user.Status.ToUpper() == "INACTIVE")
        {
            logger.LogWarning(
                "Falha no login para {UserCpf}: Usuário não encontrado ou inativo.",
                userCpf);
            return ResultPattern<TokenDTO>.FailureResult(
                "Erro ao realizar login. Por favor verifique as informações e tente novamente", 409);
        }

        var userId = user.Id;
        logger.LogInformation(
            "Usuário encontrado para a tentativa de login. UserId: {UserId}, Status: {UserStatus}",
            userId,
            user.Status);

        var accessToken = tokenGateway.CreateAccessToken(user);
        if (accessToken is null)
        {
            logger.LogError(
                "Falha crítica na geração do AccessToken para o UserId: {UserId}",
                userId);
            return ResultPattern<TokenDTO>.FailureResult("Erro ao realizar login, verifique os dados e tente novamente",
                409);
        }

        logger.LogInformation(
            "Login bem-sucedido e token gerado para o UserId: {UserId}",
            userId);

        return ResultPattern<TokenDTO>.SuccessResult(new TokenDTO(accessToken));
    }
}