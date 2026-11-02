using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class EvaluateTccUseCase(
    ITccGateway tccGateway,
    IAppLoggerGateway<EvaluateTccUseCase> logger)
{
    public async Task<ResultPattern<long>> ExecuteRegisteredUser(long tccId, long userId, decimal grade, string? evaluationComments, string? evaluationDetails)
    {
        if (grade is < 0 or > 10)
            return ResultPattern<long>.FailureResult("A nota deve estar entre 0 e 10.", 400);

        var member = await tccGateway.FindBankingMemberByTccAndUser(tccId, userId);
        if (member is null || (member.Role != "Examinador Interno" && !member.Role.Contains("Orientador", StringComparison.OrdinalIgnoreCase)))
            return ResultPattern<long>.FailureResult("O usuário não está cadastrado como examinador interno deste TCC.", 403);
        if (!member.TokenExpiryDate.HasValue)
            return ResultPattern<long>.FailureResult("A avaliacao ficara disponivel apos a conclusao da apresentacao.", 409);
        if (member.Grade.HasValue)
            return ResultPattern<long>.FailureResult("A avaliação já foi enviada.", 409);

        member.Grade = grade;
        member.EvaluationComments = !string.IsNullOrWhiteSpace(evaluationComments)
            ? evaluationComments
            : (grade >= 7m ? "Aprovado" : "Reprovado");
        member.EvaluationDetails = evaluationDetails;

        try
        {
            await tccGateway.UpdateBankingMember(member);
            logger.LogInformation("Avaliação interna enviada pelo usuário {UserId} para o TCC {TccId}.", userId, tccId);
            return ResultPattern<long>.SuccessResult(tccId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar avaliação interna do usuário {UserId} para o TCC {TccId}.", userId, tccId);
            return ResultPattern<long>.FailureResult("Erro ao salvar a avaliação.", 500);
        }
    }

    public async Task<ResultPattern<long>> Execute(EvaluateTccDTO data)
    {
        logger.LogInformation("Recebendo avaliação para o token: {Token}", data.Token);

        if (string.IsNullOrWhiteSpace(data.Token))
        {
            return ResultPattern<long>.FailureResult("Token inválido ou não fornecido.", 400);
        }

        var member = await tccGateway.FindBankingMemberByToken(data.Token);

        if (member is null)
        {
            logger.LogWarning("Membro da banca não encontrado para o token fornecido.");
            return ResultPattern<long>.FailureResult("Acesso negado ou token inválido.", 401);
        }

        if (member.UserId.HasValue)
            return ResultPattern<long>.FailureResult("Este examinador deve enviar a avaliação autenticado no sistema.", 403);

        if (member.TokenExpiryDate.HasValue && member.TokenExpiryDate.Value < DateTime.UtcNow)
        {
            logger.LogWarning("Token expirado para o membro {MemberId}.", member.Id);
            return ResultPattern<long>.FailureResult("O link de avaliação expirou.", 400);
        }

        if (member.Grade.HasValue)
        {
            logger.LogWarning("Membro {MemberId} já realizou a avaliação.", member.Id);
            return ResultPattern<long>.FailureResult("A avaliação já foi realizada anteriormente.", 400);
        }

        member.Grade = data.Grade;
        member.EvaluationComments = !string.IsNullOrWhiteSpace(data.EvaluationComments)
            ? data.EvaluationComments
            : (data.Grade >= 7m ? "Aprovado" : "Reprovado");
        member.EvaluationDetails = data.EvaluationDetails;
        
        // Do NOT invalidate token here, as the external examiner will need it later to log in and sign documents.
        // member.AccessToken = string.Empty;

        try
        {
            await tccGateway.UpdateBankingMember(member);
            logger.LogInformation("Avaliação do membro {MemberId} salva com sucesso.", member.Id);
            return ResultPattern<long>.SuccessResult(member.TccId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao salvar a avaliação do membro {MemberId}.", member.Id);
            return ResultPattern<long>.FailureResult("Erro ao salvar a avaliação.", 500);
        }
    }
}
