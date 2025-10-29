using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class GenerateGovSignatureAuthUrlUseCase(IAppLoggerGateway<GenerateGovSignatureAuthUrlUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(GetAuthorizationSignatureGovDTO data)
    {
        logger.LogInformation("Iniciando processo de construção de url para requisição authorize para assinatura do gov");
        var state = Guid.NewGuid().ToString("N");
        var nonce = Guid.NewGuid().ToString("N");
        
        logger.LogInformation("Gerando query params");
        var queryParams = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "client_id", data.ClientId },
            { "scope", data.Scope },
            { "redirect_uri", data.RedirectUri },
            { "nonce", nonce },
            { "state", state }
        };
        
        var url = new UriBuilder(data.GovAuthSignUrl);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        foreach (var param in queryParams)
        {
            query[param.Key] = param.Value;
        }
        url.Query = query.ToString();

        logger.LogInformation("Finalizando construção url e retonando");
        
        return ResultPattern<string>.SuccessResult(url.ToString());
    }
}