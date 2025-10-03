using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.AccessCode;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.AccessCode;
public class VerifyAccessCodeUseCase(IUserGateway userGateway, IAppLoggerGateway<VerifyAccessCodeUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(VerifyAccessCodeDTO data)
    {
        logger.LogInformation(
            "Iniciando verificação de código de acesso para o e-mail: {UserEmail}", 
            data.UserEmail);

        var user = await userGateway.FindByEmail(data.UserEmail);
        if (user is null)
        {
            logger.LogWarning(
                "Falha na verificação: Usuário não encontrado para o e-mail {UserEmail}", 
                data.UserEmail);
            return ResultPattern<string>.FailureResult("Erro ao verificar código de acesso. Por favor verifique as informações e tente novamente.", 404);
        }
        
        var userId = user.Id;
        logger.LogInformation("Usuário encontrado para verificação. UserId: {UserId}", userId);

        if (!user.AccessCode!.IsActive)
        {
            logger.LogWarning(
                "Falha na verificação para UserId {UserId}: O código de acesso já foi utilizado/inativado.", 
                userId);
            return ResultPattern<string>.FailureResult("Código de acesso já validado. Por favor gere outro e tente novamente.", 409);
        }

        if (!user.AccessCode!.Code.Equals(data.AccessCode))
        {
            logger.LogWarning(
                "Falha na verificação para UserId {UserId}: O código de acesso fornecido é inválido.", 
                userId);
            return ResultPattern<string>.FailureResult("Erro ao verificar código de acesso. Por favor verifique as informações e tente novamente.", 409);
        }

        var duration = user.AccessCode!.ExpirationDate - DateTime.UtcNow;
        if (duration <= TimeSpan.Zero)
        {
            logger.LogWarning(
                "Falha na verificação para UserId {UserId}: O código de acesso expirou em {ExpirationDate}", 
                userId, 
                user.AccessCode.ExpirationDate);
            return ResultPattern<string>.FailureResult("Código de acesso expirado. Por favor gere outro e tente novamente.", 409);
        }

        user.AccessCode.IsActive = false;
        await userGateway.Update(user);
        logger.LogInformation(
            "Código de acesso desativado com sucesso no banco de dados para o UserId: {UserId}", 
            userId);

        logger.LogInformation(
            "Código de acesso verificado e validado com sucesso para o UserId: {UserId}", 
            userId);

        return ResultPattern<string>.SuccessResult();
    }
}
