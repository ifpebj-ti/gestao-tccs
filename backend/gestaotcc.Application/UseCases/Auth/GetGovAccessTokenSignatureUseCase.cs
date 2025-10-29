using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class GetGovAccessTokenSignatureUseCase(
    IGovGateway govGateway, 
    IAppLoggerGateway<GetGovAccessTokenLoginUseCase> logger)
{
    public async Task<ResultPattern<TokenDTO>> Execute(string code, string redirectUri)
    {
        logger.LogInformation("Iniciando Busca de AccessToken do Gov");
        
        logger.LogInformation("Buscando Token de acesso do para assinatura gov");
        var tokenResponse = await govGateway.GetSignatureAccessToken(code, redirectUri);
        if (tokenResponse is null)
        {
            logger.LogError("Falha ao obter o token do Gov.br.");
            return ResultPattern<TokenDTO>.FailureResult("Erro ao Realizar login, por favor tente novamente.", 500);
        }
        
        return ResultPattern<TokenDTO>.SuccessResult(new TokenDTO(tokenResponse.AccessToken));
    }
}