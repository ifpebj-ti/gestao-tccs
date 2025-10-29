using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Dtos.Signature;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace gestaotcc.Infra.Gateways;

public class GovGateway(IConfiguration configuration) : IGovGateway
{
    public async Task<ResponseGetGovTokenDTO?> GetLoginAccessToken(string code, string codeVerifier)
    {
        var govSettings = configuration.GetSection("GOV_SETTINGS");
        var urlToken = govSettings.GetValue<string>("URL_TOKEN");
        var clientId = govSettings.GetValue<string>("CLIENT_ID");
        var redirectUri = govSettings.GetValue<string>("REDIRECT_URL");

        var options = new RestClientOptions(urlToken);
        var client = new RestClient(options);


        var request = new RestRequest("token", Method.Post);
        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("code", code);
        request.AddParameter("code_verifier", codeVerifier);
        request.AddParameter("redirect_uri", redirectUri);
        request.AddParameter("client_id", clientId);

        var response = await client.ExecutePostAsync<ResponseGetGovTokenDTO>(request);

        return response.Data;
    }

    public async Task<ResponseGetSignatureGovTokenDTO?> GetSignatureAccessToken(string code, string redirectUri)
    {
        var govSettings = configuration.GetSection("GOV_SETTINGS");
        var urlToken = govSettings.GetValue<string>("URL_TOKEN_SIGN");
        var clientId = govSettings.GetValue<string>("CLIENT_ID_SIGN");
        var clientSecret = govSettings.GetValue<string>("CLIENT_SECRET_SIGN");

        if (string.IsNullOrEmpty(urlToken) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException(
                "Configurações da API de Assinatura do Gov.br não encontradas (URL_TOKEN_SIGN, CLIENT_ID_SIGN, ou CLIENT_SECRET_SIGN).");
        }

        var options = new RestClientOptions(urlToken);
        var client = new RestClient(options);
        var request = new RestRequest("", Method.Post);

        request.AddParameter("grant_type", "authorization_code");
        request.AddParameter("code", code);
        request.AddParameter("redirect_uri", redirectUri);
        request.AddParameter("client_id", clientId);
        request.AddParameter("client_secret", clientSecret);

        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

        var response = await client.ExecutePostAsync<ResponseGetSignatureGovTokenDTO>(request);

        return response.Data;
    }

    public async Task<string> SignPkcs7(string hashToSignB64, string accessToken)
    {
        var govSettings = configuration.GetSection("GOV_SETTINGS");
        var urlAssinatura = govSettings.GetValue<string>("URL_ASSINATURA_PKCS7");

        if (string.IsNullOrEmpty(urlAssinatura))
        {
            throw new InvalidOperationException(
                "URL da API de Assinatura PKCS7 não configurada (URL_ASSINATURA_PKCS7).");
        }

        var options = new RestClientOptions(urlAssinatura);
        var client = new RestClient(options);
        var request = new RestRequest("", Method.Post);

        request.AddHeader("Authorization", $"Bearer {accessToken}");
        
        var requestBody = new { hashBase64 = hashToSignB64 };
        request.AddJsonBody(requestBody);

        // Executa a requisição. Esperamos um JSON como resposta.
        var response = await client.ExecutePostAsync<SignPkcs7ResponseDTO>(request);

        return response.Data.AssinaturaPKCS7;
    }
}