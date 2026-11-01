using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Tcc;

public class ConfirmHardCoverUseCase
{
    private readonly ITccGateway _tccGateway;
    private readonly IUserGateway _userGateway;

    public ConfirmHardCoverUseCase(ITccGateway tccGateway, IUserGateway userGateway)
    {
        _tccGateway = tccGateway;
        _userGateway = userGateway;
    }

    public async Task<ResultPattern<bool>> Execute(long tccId, long coordinatorUserId)
    {
        if (tccId <= 0 || coordinatorUserId <= 0)
            return ResultPattern<bool>.FailureResult("Parâmetros inválidos.", 400);

        var user = await _userGateway.FindById(coordinatorUserId);
        if (user == null || user.Status != "ACTIVE")
            return ResultPattern<bool>.FailureResult("Usuário não autorizado ou inativo.", 401);

        var roles = user.Profile.Select(p => p.Role).ToArray();
        var canConfirm = roles.Any(r => r is "ADMIN" or "COORDINATOR" or "SUPERVISOR");
        if (!canConfirm)
            return ResultPattern<bool>.FailureResult("Apenas a Coordenação ou Administração do curso pode confirmar a entrega da capa dura.", 403);

        var tcc = await _tccGateway.FindTccById(tccId);
        if (tcc == null)
            return ResultPattern<bool>.FailureResult("TCC não encontrado.", 404);

        if (!roles.Contains("ADMIN") && user.CampiCourseId.HasValue && user.CampiCourseId.Value > 0)
        {
            var studentCourses = tcc.UserTccs
                .Where(link => link.Profile.Role == "STUDENT")
                .Select(link => link.User.CampiCourseId)
                .ToList();

            if (studentCourses.Count > 0 && !studentCourses.Contains(user.CampiCourseId))
                return ResultPattern<bool>.FailureResult("Acesso não autorizado para o curso deste TCC.", 403);
        }

        var isFinalStage = tcc.Step == StepTccType.FINALIZATION_AND_PUBLICATION.ToString() ||
                           tcc.Step == "6" ||
                           tcc.FinalSubmissionDeadline.HasValue;

        if (!isFinalStage)
            return ResultPattern<bool>.FailureResult("O TCC não está na etapa final de entrega da versão em capa dura.", 400);

        if (tcc.HardCoverReceivedAt.HasValue)
            return ResultPattern<bool>.FailureResult("A entrega da capa dura já foi confirmada anteriormente.", 409);

        tcc.HardCoverReceivedAt = DateTime.UtcNow;
        tcc.HardCoverReceivedByUserId = coordinatorUserId;
        await _tccGateway.Update(tcc);

        return ResultPattern<bool>.SuccessResult(true);
    }
}
