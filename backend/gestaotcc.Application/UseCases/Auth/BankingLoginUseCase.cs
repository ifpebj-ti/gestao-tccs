using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class BankingLoginUseCase(
    ITccGateway tccGateway,
    IUserGateway userGateway,
    ITokenGateway tokenGateway,
    IAppLoggerGateway<BankingLoginUseCase> logger)
{
    public async Task<ResultPattern<TokenDTO>> Execute(string token)
    {
        logger.LogInformation("Iniciando tentativa de login para avaliador externo com token: {Token}", token);

        var member = await tccGateway.FindBankingMemberByToken(token);
        
        if (member == null)
        {
            logger.LogWarning("Token inválido ou não encontrado: {Token}", token);
            return ResultPattern<TokenDTO>.FailureResult("Link inválido ou expirado.", 401);
        }

        if (member.TokenExpiryDate.HasValue && member.TokenExpiryDate.Value < DateTime.UtcNow)
        {
            logger.LogWarning("Token expirado: {Token}", token);
            return ResultPattern<TokenDTO>.FailureResult("Este link expirou. Solicite um novo link de avaliação.", 401);
        }

        var user = await userGateway.FindByEmail(member.Email);
        
        if (user == null || user.Status.ToUpper() == "INACTIVE")
        {
            logger.LogWarning("Avaliador externo não tem perfil de usuário criado ou está inativo. Email: {Email}", member.Email);
            return ResultPattern<TokenDTO>.FailureResult("Usuário não encontrado no sistema.", 404);
        }

        var accessToken = tokenGateway.CreateAccessToken(user);
        if (accessToken == null)
        {
            logger.LogError("Falha na geração do AccessToken para avaliador externo. UserId: {UserId}", user.Id);
            return ResultPattern<TokenDTO>.FailureResult("Erro ao gerar autorização. Tente novamente.", 500);
        }

        logger.LogInformation("Login de avaliador externo bem-sucedido para o UserId: {UserId}", user.Id);
        
        return ResultPattern<TokenDTO>.SuccessResult(new TokenDTO(accessToken, false, false));
    }
}
