using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gestaotcc.Infra.Gateways;

public class GovGateway(IConfiguration configuration) : IGovGateway
{
    public async Task<ResponseGetGovTokenDTO?> GetAccessToken(string code, string codeVerifier)
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
}