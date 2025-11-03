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
    public async Task<ResultPattern<TokenDTO>> Execute(string userEmail, string userPassword)
    {
        logger.LogInformation("Iniciando tentativa de login para o e-mail: {UserEmail}", userEmail);

        var user = await userGateway.FindByEmail(userEmail);
        if (user is null || user.Status.ToUpper() == "INACTIVE")
        {
            logger.LogWarning(
                "Falha no login para {UserEmail}: Usuário não encontrado ou inativo.",
                userEmail);
            return ResultPattern<TokenDTO>.FailureResult(
                "Erro ao realizar login. Por favor verifique as informações e tente novamente", 409);
        }

        var userId = user.Id;
        logger.LogInformation(
            "Usuário encontrado para a tentativa de login. UserId: {UserId}, Status: {UserStatus}",
            userId,
            user.Status);

        var result = ValidateTypeLogin(user, userPassword);
        if (result.IsFailure)
            return ResultPattern<TokenDTO>.FailureResult(result.Message, 409);

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
        
        var isDevTest = user.Email == "dev-test@gmail.com";
        var isTempDevTestPassword = user.Password == "Senha@123";

        return ResultPattern<TokenDTO>.SuccessResult(new TokenDTO(accessToken, isDevTest, isTempDevTestPassword));
    }

    private ResultPattern<TokenDTO> ValidateTypeLogin(UserEntity user, string userPassword)
    {
        var userId = user.Id;

        if (user.Status.ToUpper() == "ACTIVE")
        {
            logger.LogInformation("Validando senha com hash para usuário ativo. UserId: {UserId}", userId);
            var result = bcryptGateway.VerifyHashPassword(user, userPassword);
            if (!result)
            {
                logger.LogWarning("Falha na validação de senha (hash) para o UserId: {UserId}", userId);
                return ResultPattern<TokenDTO>.FailureResult(
                    "Erro ao realizar login, verifique os dados e tente novamente", 409);
            }
        }
        else // Status de primeiro acesso
        {
            logger.LogInformation("Validando senha de primeiro acesso para UserId: {UserId}", userId);
            if (user.Password != userPassword)
            {
                logger.LogWarning("Falha na validação de senha (primeiro acesso) para o UserId: {UserId}", userId);
                return ResultPattern<TokenDTO>.FailureResult(
                    "Erro ao realizar login, verifique os dados e tente novamente", 409);
            }
        }

        logger.LogInformation("Validação de senha bem-sucedida para o UserId: {UserId}", userId);
        return ResultPattern<TokenDTO>.SuccessResult();
    }
}