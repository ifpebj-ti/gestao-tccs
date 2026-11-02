using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Email;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ConcludePresentationUseCase(
    ITccGateway tccGateway,
    IProfileGateway profileGateway,
    IEmailGateway emailGateway,
    IAppLoggerGateway<ConcludePresentationUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(long tccId)
    {
        logger.LogInformation("Iniciando conclusão de apresentação para o TccId: {TccId}", tccId);

        var tcc = await tccGateway.FindTccScheduling(tccId);
        
        if (tcc is null)
        {
            logger.LogWarning("TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<string>.FailureResult("TCC não encontrado.", 404);
        }

        if (tcc.Status == StatusTccType.COMPLETED.ToString())
        {
            logger.LogWarning("TCC {TccId} já está concluído.", tccId);
            return ResultPattern<string>.FailureResult("A apresentação deste TCC já foi concluída.", 400);
        }

        // A apresentação está concluída, mas o status do TCC só passará para COMPLETED
        // após todos os membros (orientador, banca, alunos) assinarem a Ficha Avaliativa (Anexo IV).

        // Garante que o orientador está na banca para poder avaliar
        var advisorUser = tcc.UserTccs.FirstOrDefault(ut => ut.Profile.Role == gestaotcc.Domain.Enums.RoleType.ADVISOR.ToString())?.User;
        var bankingProfile = await profileGateway.FindByRole("BANKING");
        if (advisorUser != null && !tcc.BankingMembers.Any(m => m.Email.Equals(advisorUser.Email, StringComparison.OrdinalIgnoreCase)))
        {
            var token = Guid.NewGuid().ToString("N");
            var advisorMember = new gestaotcc.Domain.Entities.TccBankingMember.TccBankingMemberEntity(advisorUser.Name, advisorUser.Email, "Orientador(a)", token, tcc.Id)
            {
                UserId = advisorUser.Id
            };
            tcc.BankingMembers.Add(advisorMember);
            if (bankingProfile is not null)
                TccFactory.UpdateUsersTccToCreateBanking(tcc, advisorUser, bankingProfile);
        }
        else if (advisorUser != null)
        {
            var advisorMember = tcc.BankingMembers.First(m => m.Email.Equals(advisorUser.Email, StringComparison.OrdinalIgnoreCase));
            advisorMember.UserId ??= advisorUser.Id;
            if (bankingProfile is not null)
                TccFactory.UpdateUsersTccToCreateBanking(tcc, advisorUser, bankingProfile);
        }

        // Revalidate/Generate token for each banking member and send email
        foreach (var member in tcc.BankingMembers)
        {
            member.AccessToken = Guid.NewGuid().ToString("N");
            member.TokenExpiryDate = DateTime.UtcNow.AddHours(1); // Token válido por 1 hora

            if (member.UserId.HasValue) continue;

            // Não enviar e-mail se for o Orientador, pois ele avalia direto na tela
            if (!member.Role.Contains("Orientador", StringComparison.OrdinalIgnoreCase))
            {
                var variables = new Dictionary<string, object>
                {
                    { "username", member.Name },
                    { "titulo_tcc", tcc.Title ?? "TCC" },
                    { "token", member.AccessToken }
                };

                var emailDto = new SendEmailDTO(
                    emailBody: string.Empty,
                    subjet: "Avaliação do TCC - Lançamento de Notas",
                    recipient: member.Email,
                    typeTemplate: "BANKING-EVALUATION",
                    variables: variables
                );

                await emailGateway.Send(emailDto);
            }
        }

        try
        {
            await tccGateway.Update(tcc);
            logger.LogInformation("Apresentação do TCC {TccId} concluída com sucesso.", tccId);
            
            var advisorToken = tcc.BankingMembers
                .FirstOrDefault(m => m.Role.Contains("Orientador", StringComparison.OrdinalIgnoreCase))?.AccessToken;

            var advisorIsRegistered = tcc.BankingMembers.Any(m =>
                m.Role.Contains("Orientador", StringComparison.OrdinalIgnoreCase) && m.UserId.HasValue);
            return ResultPattern<string>.SuccessResult(advisorIsRegistered ? string.Empty : advisorToken ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar TCC {TccId} para concluído.", tccId);
            return ResultPattern<string>.FailureResult("Erro ao concluir a apresentação do TCC.", 500);
        }
    }
}
