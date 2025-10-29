using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class GenerateGovLoginAuthUrlUseCase(IAppLoggerGateway<GenerateGovLoginAuthUrlUseCase> logger, IMemoryCacheGateway memoryCacheGateway)
{
    public async Task<ResultPattern<string>> Execute(GetAuthorizationLoginGovDTO data)
    {
        logger.LogInformation("Iniciando processo de construção de url para requisição authorize do gov");
        var state = Guid.NewGuid().ToString("N");
        var nonce = Guid.NewGuid().ToString("N");
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        // ETAPA CRUCIAL: Salvar o code_verifier usando o state como chave.
        // O Gov.br nos devolverá o 'state', e usaremos ele para recuperar o 'code_verifier' correto.
        memoryCacheGateway.Set(state, codeVerifier, TimeSpan.FromMinutes(10));
        
        logger.LogInformation("Gerando query params");
        var queryParams = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "client_id", data.ClientId },
            { "scope", data.Scope },
            { "redirect_uri", data.RedirectUri },
            { "nonce", nonce },
            { "state", state },
            { "code_challenge", codeChallenge },
            { "code_challenge_method", "S256" }
        };
        
        var url = new UriBuilder(data.GovAuthUrl);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
        foreach (var param in queryParams)
        {
            query[param.Key] = param.Value;
        }
        url.Query = query.ToString();

        logger.LogInformation("Finalizando construção url e retonando");
        
        return ResultPattern<string>.SuccessResult(url.ToString());
    }
    
    private string GenerateCodeVerifier()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return System.Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var challengeBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
        return System.Convert.ToBase64String(challengeBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}