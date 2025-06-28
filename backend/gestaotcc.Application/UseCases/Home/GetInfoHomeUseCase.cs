using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Home;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Home;

public class GetInfoHomeUseCase(IUserGateway userGateway, ITccGateway tccGateway, FindAllPendingSignaturesUseCase findAllPendingSignaturesUseCase)
{
    public async Task<ResultPattern<GetInfoHomeDTO>> Execute(long userId)
    {
        var user = await userGateway.FindById(userId);
        if (user is null || !user.Profile.Any())
            return ResultPattern<GetInfoHomeDTO>.FailureResult("Usuário não encontrado ou não possui perfil.", 404);

        var profiles = user.Profile.Select(p => p.Role).ToList();

        int pendingSignaturesCount = 0;
        int inProgressTccsCount = 0;

        if (profiles.Contains(RoleType.COORDINATOR.ToString()) || profiles.Contains(RoleType.SUPERVISOR.ToString()))
        {
            var pendingSignaturesResult = await findAllPendingSignaturesUseCase.Execute(null);
            if (pendingSignaturesResult.IsSuccess)
            {
                pendingSignaturesCount = pendingSignaturesResult.Data.Sum(p => p.PendingDetails.Count);
            }

            var allInProgressTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(null, StatusTccType.IN_PROGRESS.ToString()));
            inProgressTccsCount = allInProgressTccs.Count;
        }
        else if (profiles.Contains(RoleType.ADVISOR.ToString()) || profiles.Contains(RoleType.BANKING.ToString()))
        {
            var pendingSignaturesResult = await findAllPendingSignaturesUseCase.Execute(userId);
            if (pendingSignaturesResult.IsSuccess)
            {
                pendingSignaturesCount = pendingSignaturesResult.Data.Sum(p => p.PendingDetails.Count);
            }

            var userTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(userId, StatusTccType.IN_PROGRESS.ToString()));
            inProgressTccsCount = userTccs.Count;
        }
        else if (profiles.Contains(RoleType.STUDENT.ToString()))
        {
            var pendingSignaturesResult = await findAllPendingSignaturesUseCase.Execute(userId);
            if (pendingSignaturesResult.IsSuccess)
            {
                pendingSignaturesCount = pendingSignaturesResult.Data.Sum(p => p.PendingDetails.Count);
            }
            
            var userTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(userId, StatusTccType.IN_PROGRESS.ToString()));
            inProgressTccsCount = userTccs.Any() ? 1 : 0;
        }

        var resultDto = new GetInfoHomeDTO(pendingSignaturesCount, inProgressTccsCount);

        return ResultPattern<GetInfoHomeDTO>.SuccessResult(resultDto);
    }
}