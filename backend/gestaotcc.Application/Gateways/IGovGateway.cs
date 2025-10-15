using gestaotcc.Domain.Dtos.Auth;

namespace gestaotcc.Application.Gateways;

public interface IGovGateway
{
    Task<ResponseGetGovTokenDTO> GetAccessToken(string code, string codeVerifier);
}