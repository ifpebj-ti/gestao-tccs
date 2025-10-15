using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class GetGovAccessTokenUseCase(
    IGovGateway govGateway, 
    IMemoryCacheGateway memoryCacheGateway, 
    IAppLoggerGateway<GetGovAccessTokenUseCase> logger,
    ITokenGateway tokenGateway,
    LoginUseCase loginUseCase)
{
    public async Task<ResultPattern<TokenDTO>> Execute(string code, string state)
    {
        logger.LogInformation("Iniciando Busca de AccessToken do Gov");
        if (!memoryCacheGateway.TryGetValue<string>(state, out var codeVerifier) || string.IsNullOrEmpty(codeVerifier))
        {
            logger.LogError("Estado inválido ou expirado. Possível ataque CSRF");
            return ResultPattern<TokenDTO>.FailureResult("Erro ao Realizar login, por favor tente novamente.", 500);
        }

        // 2. Remover do cache para garantir que só seja usado uma vez.
        memoryCacheGateway.Remove(state);
        
        logger.LogInformation("Buscando Token de acesso do gov");
        var tokenResponse = await govGateway.GetAccessToken(code, codeVerifier);
        if (tokenResponse is null)
        {
            logger.LogError("Falha ao obter o token do Gov.br.");
            return ResultPattern<TokenDTO>.FailureResult("Erro ao Realizar login, por favor tente novamente.", 500);
        }

        logger.LogInformation("Decodificando o id token gov.br");
        var decodedToken = tokenGateway.DecodeToken(tokenResponse.IdToken);
        if(decodedToken is null)
        {
            logger.LogWarning("Erro ao buscar token gov.br");
            return ResultPattern<TokenDTO>.FailureResult("Erro ao Realizar login, por favor tente novamente.", 500);
        }

        var gradusFlowAccessToken = await loginUseCase.Execute(decodedToken.Cpf);
        if (gradusFlowAccessToken.IsFailure)
        {
            logger.LogWarning("Erro ao logar na plataforma gradus flow");
            return ResultPattern<TokenDTO>.FailureResult("Erro ao Realizar login, por favor tente novamente.", 500);
        }

        return ResultPattern<TokenDTO>.SuccessResult(gradusFlowAccessToken.Data);
    }
}