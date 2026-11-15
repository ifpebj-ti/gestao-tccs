using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

/// <summary>
/// Applies local-account eligibility rules to an already verified Google identity.
/// Provider response validation and token issuance belong to the Google login flow.
/// </summary>
public class GoogleAccountEligibilityUseCase(IUserGateway userGateway)
{
    private const string RejectedMessage = "Não foi possível autenticar esta conta com o Google.";

    public async Task<ResultPattern<UserEntity>> Execute(
        GoogleIdentityDTO identity,
        IEnumerable<string>? allowedDomains)
    {
        if (identity is null || !identity.EmailVerified || string.IsNullOrWhiteSpace(identity.Email))
            return Reject();

        var normalizedEmail = identity.Email.Trim().ToLowerInvariant();
        var separatorIndex = normalizedEmail.LastIndexOf('@');
        if (separatorIndex <= 0 || separatorIndex == normalizedEmail.Length - 1)
            return Reject();

        var emailDomain = normalizedEmail[(separatorIndex + 1)..];
        var domainIsAllowed = allowedDomains?.Any(domain =>
            !string.IsNullOrWhiteSpace(domain) &&
            string.Equals(domain.Trim(), emailDomain, StringComparison.OrdinalIgnoreCase)) ?? false;
        if (!domainIsAllowed)
            return Reject();

        var user = await userGateway.FindByEmail(normalizedEmail);
        if (user is null ||
            !string.Equals(user.Email?.Trim(), normalizedEmail, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(user.Status?.Trim(), "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return Reject();
        }

        return ResultPattern<UserEntity>.SuccessResult(user);
    }

    private static ResultPattern<UserEntity> Reject() =>
        ResultPattern<UserEntity>.FailureResult(RejectedMessage, 401, "Autenticação não autorizada");
}