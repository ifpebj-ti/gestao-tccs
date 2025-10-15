using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class GetGovAccessTokenUseCase(
    IGovGateway govGateway, 
    IMemoryCacheGateway memoryCacheGateway, 
    IAppLoggerGateway<GetGovAccessTokenUseCase> logger,
    ITokenGateway tokenGateway)
{
    public async Task<ResultPattern<string>> Execute(string code, string state)
    {
        logger.LogInformation("Iniciando Busca de AccessToken do Gov");
        if (!memoryCacheGateway.TryGetValue<string>(state, out var codeVerifier) || string.IsNullOrEmpty(codeVerifier))
        {
            logger.LogError("Estado inválido ou expirado. Possível ataque CSRF");
            return ResultPattern<string>.FailureResult("Erro ao Realizar login, por favor tente novamente.", 500);
        }

        // 2. Remover do cache para garantir que só seja usado uma vez.
        memoryCacheGateway.Remove(state);
        
        logger.LogInformation("Buscando Token de acesso do gov");
        var tokenResponse = await govGateway.GetAccessToken(code, codeVerifier);
        if (tokenResponse is null)
        {
            logger.LogError("Falha ao obter o token do Gov.br.");
            return ResultPattern<string>.FailureResult("Erro ao Realizar login, por favor tente novamente.", 500);
        }

        var decodedToken = tokenGateway.DecodeToken(tokenResponse.IdToken);
        
        // 5. LÓGICA DA SUA APLICAÇÃO:
        // - Buscar usuário no seu banco de dados pelo CPF.
        // - Se não existir, criar um novo usuário.
        // - Gerar um token JWT da SUA aplicação para este usuário.
        
    }
}