using gestaotcc.Domain.Dtos.Auth;

namespace gestaotcc.Application.Gateways;

public interface IGovGateway
{
    Task<ResponseGetGovTokenDTO?> GetLoginAccessToken(string code, string codeVerifier);
    Task<ResponseGetSignatureGovTokenDTO?> GetSignatureAccessToken(string code, string redirectUri);
    Task<string> SignPkcs7(string hashToSignB64, string accessToken);
}