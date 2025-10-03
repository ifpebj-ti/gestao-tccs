using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Auth;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Auth;

public class NewPasswordUseCase(
    IUserGateway userGateway,
    IBcryptGateway bcryptGateway,
    ITccGateway tccGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<NewPasswordUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(NewPasswordDTO data)
    {
        logger.LogInformation(
            "Iniciando processo de criação de nova senha para o e-mail: {UserEmail}",
            data.Email);

        var user = await userGateway.FindByEmail(data.Email);
        var tccInvite = await tccGateway.FindInviteTccByEmail(data.Email);

        if (user is null || tccInvite is null)
        {
            logger.LogWarning(
                "Falha na criação de nova senha para {UserEmail}: Usuário ou Convite TCC não encontrado. UserIsNull: {UserIsNull}, TccInviteIsNull: {TccInviteIsNull}",
                data.Email,
                user is null,
                tccInvite is null);
            return ResultPattern<string>.FailureResult(
                "Erro ao criar nova senha. Por favor verifique as informações e tente novamente.", 404);
        }

        var userId = user.Id;
        logger.LogInformation(
            "Usuário e Convite TCC encontrados para {UserEmail}. UserId: {UserId}, TccInviteId: {TccInviteId}",
            data.Email,
            userId,
            tccInvite.Id);

        if (tccInvite.Email != data.Email ||
            tccInvite.Code != data.InviteCode ||
            tccInvite.IsValidCode)
        {
            logger.LogWarning(
                "Falha na validação do código de convite para o UserId: {UserId}. O código fornecido é inválido ou já foi utilizado.",
                userId);
            return ResultPattern<string>.FailureResult(
                "Erro ao criar nova senha. Por favor verifique as informações e tente novamente.", 409);
        }

        logger.LogInformation("Validação do convite bem-sucedida para o UserId: {UserId}", userId);

        user.Password = bcryptGateway.GenerateHashPassword(data.Password);
        user.Status = user.Status != "INACTIVE" ? "ACTIVE" : user.Status;

        await userGateway.Update(user);

        logger.LogInformation(
            "Usuário atualizado com nova senha e status no banco de dados. UserId: {UserId}",
            userId);

        var emailDTO = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(user, "AUTO-REGISTER-USER"));
        if (emailDTO.IsFailure)
        {
            logger.LogError(
                "Falha ao enviar e-mail de confirmação para UserId: {UserId}. Motivo do gateway: {ErrorMessage}",
                userId,
                emailDTO.Message);
            return ResultPattern<string>.FailureResult(emailDTO.Message, 500);
        }

        logger.LogInformation(
            "Processo de criação de nova senha concluído com sucesso para UserId: {UserId}",
            userId);

        return ResultPattern<string>.SuccessResult();
    }
}