using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class GoogleLoginUseCase(
    GoogleAccountEligibilityUseCase eligibilityUseCase,
    ITokenGateway tokenGateway)
{
    private const string RejectedMessage = "Não foi possível autenticar esta conta com o Google.";

    public async Task<ResultPattern<TokenDTO>> Execute(
        GoogleIdentityDTO identity,
        IEnumerable<string>? allowedDomains)
    {
        var eligibility = await eligibilityUseCase.Execute(identity, allowedDomains);
        if (eligibility.IsFailure)
            return ResultPattern<TokenDTO>.FailureResult(RejectedMessage, 401, "Autenticação não autorizada");

        var accessToken = tokenGateway.CreateAccessToken(eligibility.Data);
        if (string.IsNullOrWhiteSpace(accessToken))
            return ResultPattern<TokenDTO>.FailureResult(RejectedMessage, 401, "Autenticação não autorizada");

        return ResultPattern<TokenDTO>.SuccessResult(new TokenDTO(accessToken, false, false));
    }
}