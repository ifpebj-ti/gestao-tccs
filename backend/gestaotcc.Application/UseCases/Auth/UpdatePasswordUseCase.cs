using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;
public class UpdatePasswordUseCase(IUserGateway userGateway, IBcryptGateway bcryptGateway, IAppLoggerGateway<UpdatePasswordUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(UpdatePasswordDTO data)
    {
        logger.LogInformation("Iniciando processo de alteração de senha para o e-mail: {UserEmail}", data.UserEmail);

        var user = await userGateway.FindByEmail(data.UserEmail);
        if (user is null)
        {
            logger.LogWarning("Falha na alteração de senha para {UserEmail}: Usuário não encontrado.", data.UserEmail);
            return ResultPattern<string>.FailureResult("Erro ao alterar senha. Por favor verifique as informações e tente novamente.", 404);
        }

        var userId = user.Id;
        logger.LogInformation("Usuário encontrado para alteração de senha. UserId: {UserId}", userId);

        if (user.Email != "dev-test@gmail.com")
        {
            if (user.AccessCode!.IsActive)
            {
                logger.LogWarning("Falha na alteração de senha para UserId {UserId}: O código de acesso ainda não foi validado.", userId);
                return ResultPattern<string>.FailureResult("Código de acesso ainda não validado. Por favor tente novamente.", 409);
            }
                
            var duration = user.AccessCode!.ExpirationDate - DateTime.UtcNow;
            if (duration <= TimeSpan.Zero)
            {
                logger.LogWarning("Falha na alteração de senha para UserId {UserId}: O código de acesso expirou em {ExpirationDate}.", userId, user.AccessCode.ExpirationDate);
                return ResultPattern<string>.FailureResult("Código de acesso expirado. Por favor gere outro e tente novamente.", 409);
            }
                
            if (user.AccessCode.IsUserUpdatePassword)
            {
                logger.LogWarning("Falha na alteração de senha para UserId {UserId}: O código de acesso já foi utilizado para uma alteração anterior.", userId);
                return ResultPattern<string>.FailureResult("Código de acesso já utilizado. Por favor gere outro e tente novamente.", 409);
            }

            logger.LogInformation("Validações concluídas para UserId {UserId}. Atualizando a senha.", userId);
            
            user.AccessCode.IsUserUpdatePassword = true;
        }

        user.Password = bcryptGateway.GenerateHashPassword(data.UserPassword);
        user.Status = user.Status != "INACTIVE" ? "ACTIVE" : user.Status;

        await userGateway.Update(user);

        logger.LogInformation("Senha alterada e usuário atualizado com sucesso no banco de dados para o UserId: {UserId}", userId);

        return ResultPattern<string>.SuccessResult();
    }
}