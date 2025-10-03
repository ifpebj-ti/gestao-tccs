using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.AccessCode;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.AccessCode;
public class ResendAccessCodeUseCase(IUserGateway userGateway, IEmailGateway emailGateway, IAppLoggerGateway<ResendAccessCodeUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(ResendAccessCodeDTO data, string combination)
    {
        logger.LogInformation(
            "Iniciando o reenvio do código de acesso para o email: {UserEmail}", 
            data.UserEmail);
        
        var user = await userGateway.FindByEmail(data.UserEmail);
        if (user is null)
        {
            logger.LogWarning(
                "Nenhum usuário encontrado para o e-mail fornecido: {UserEmail}", 
                data.UserEmail);
            return ResultPattern<string>.FailureResult("Erro ao reenviar código de acesso. Por favor verifique as informações e tente novamente.", 404);
        }

        logger.LogInformation(
            "Usuário encontrado com sucesso. UserId: {UserId}", 
            user.Id);

        var newAccessCode = GenerateNewAccessCode(combination);
        user.AccessCode!.Code = newAccessCode;
        user.AccessCode!.ExpirationDate = DateTime.UtcNow.AddMinutes(5);
        user.AccessCode.IsActive = true;
        user.AccessCode.IsUserUpdatePassword = false;

        await userGateway.Update(user);

        logger.LogInformation(
            "Código de acesso atualizado no banco de dados para o UserId: {UserId}", 
            user.Id);

        var sendResult = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(user, "UPDATE-PASSWORD"));
        if (sendResult.IsFailure)
        {
            logger.LogError(
                "Falha ao enviar e-mail para o UserId: {UserId}. Motivo do gateway: {ErrorMessage}",
                user.Id,
                sendResult.Message);
            return ResultPattern<string>.FailureResult(sendResult.Message, 500);
        }

        logger.LogInformation(
            "Processo de reenvio de código concluído com sucesso para o email: {UserEmail} (UserId: {UserId})", 
            data.UserEmail, 
            user.Id);

        return ResultPattern<string>.SuccessResult();
    }

    private string GenerateNewAccessCode(string combination)
    {
        var chars = combination;
        var random = new Random();

        var code = new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return code;
    }
}
