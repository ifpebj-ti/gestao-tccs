using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class EvaluateTccUseCase(
    ITccGateway tccGateway,
    IAppLoggerGateway<EvaluateTccUseCase> logger)
{
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
        member.EvaluationComments = data.EvaluationComments;
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
