using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Entities.TccInvite;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class SendStudentVerificationCodeUseCase(
    IUserGateway userGateway,
    ITccGateway tccGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<SendStudentVerificationCodeUseCase> logger)
{
    public async Task<ResultPattern<bool>> Execute(SendStudentVerificationCodeDTO data)
    {
        logger.LogInformation("Iniciando envio de código de verificação para o e-mail: {UserEmail}", data.UserEmail);

        var existingUser = await userGateway.FindByEmail(data.UserEmail);
        if (existingUser is not null)
        {
            logger.LogWarning("Falha no envio de código: o e-mail {UserEmail} já possui cadastro no sistema.", data.UserEmail);
            return ResultPattern<bool>.FailureResult("Este e-mail institucional já possui cadastro no sistema. Faça login ou recupere sua senha.", 409);
        }

        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
        var code = new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        logger.LogDebug("Código de verificação gerado para {UserEmail}: {VerificationCode}", data.UserEmail, code);

        var existingInvite = await tccGateway.FindInviteTccByEmail(data.UserEmail);
        if (existingInvite is not null)
        {
            logger.LogInformation("Convite/código pré-existente encontrado para {UserEmail}. Atualizando...", data.UserEmail);
            existingInvite.Code = code;
            existingInvite.IsValidCode = true;
            existingInvite.ExpirationDate = DateTime.UtcNow.AddHours(48);

            await tccGateway.UpdateTccInvite(existingInvite);
            var emailDto = EmailFactory.CreateSendEmailDTO(existingInvite, "INVITE-USER");
            await emailGateway.Send(emailDto);
        }
        else
        {
            logger.LogInformation("Criando novo registro de verificação para {UserEmail}...", data.UserEmail);
            var newInvite = new TccInviteEntity
            {
                Email = data.UserEmail,
                Code = code,
                IsValidCode = true,
                ExpirationDate = DateTime.UtcNow.AddHours(48)
            };

            await tccGateway.SaveTccInvite(newInvite);
            var emailDto = EmailFactory.CreateSendEmailDTO(newInvite, "INVITE-USER");
            await emailGateway.Send(emailDto);
        }

        logger.LogInformation("Código de verificação enviado com sucesso para {UserEmail}.", data.UserEmail);
        return ResultPattern<bool>.SuccessResult(true);
    }
}
