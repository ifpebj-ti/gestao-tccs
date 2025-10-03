using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.Signature;
using gestaotcc.Domain.Dtos.Home;
using gestaotcc.Domain.Dtos.Tcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Home;

public class GetInfoHomeUseCase(
    IUserGateway userGateway, 
    ITccGateway tccGateway, 
    FindAllPendingSignaturesUseCase findAllPendingSignaturesUseCase, 
    IAppLoggerGateway<GetInfoHomeUseCase> logger)
{
    public async Task<ResultPattern<GetInfoHomeDTO>> Execute(long userId)
    {
        logger.LogInformation("Iniciando busca de informações da home para o UserId: {UserId}", userId);

        var user = await userGateway.FindById(userId);
        if (user is null || !user.Profile.Any())
        {
            logger.LogWarning("Falha ao buscar informações da home para UserId {UserId}: Usuário não encontrado ou não possui perfil.", userId);
            return ResultPattern<GetInfoHomeDTO>.FailureResult("Usuário não encontrado ou não possui perfil.", 404);
        }

        var profiles = user.Profile.Select(p => p.Role).ToList();
        logger.LogInformation("Perfis encontrados para UserId {UserId}: [{UserProfiles}]", userId, string.Join(", ", profiles));

        int pendingSignaturesCount = 0;
        int inProgressTccsCount = 0;

        if (profiles.Contains(RoleType.COORDINATOR.ToString()) || profiles.Contains(RoleType.SUPERVISOR.ToString()))
        {
            logger.LogInformation("Executando lógica de busca para perfil COORDENADOR/SUPERVISOR. UserId: {UserId}", userId);
            var pendingSignaturesResult = await findAllPendingSignaturesUseCase.Execute(null);
            if (pendingSignaturesResult.IsSuccess)
            {
                pendingSignaturesCount = pendingSignaturesResult.Data.Sum(p => p.PendingDetails.Count);
            }
            else
            {
                logger.LogWarning("A busca por assinaturas pendentes (global) falhou. UserId: {UserId}. Motivo: {ErrorMessage}", userId, pendingSignaturesResult.Message);
            }

            var allInProgressTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(null, StatusTccType.IN_PROGRESS.ToString()));
            inProgressTccsCount = allInProgressTccs.Count;
        }
        else if (profiles.Contains(RoleType.ADVISOR.ToString()) || profiles.Contains(RoleType.BANKING.ToString()))
        {
            logger.LogInformation("Executando lógica de busca para perfil ORIENTADOR/BANCA. UserId: {UserId}", userId);
            var pendingSignaturesResult = await findAllPendingSignaturesUseCase.Execute(userId);
            if (pendingSignaturesResult.IsSuccess)
            {
                pendingSignaturesCount = pendingSignaturesResult.Data.Sum(p => p.PendingDetails.Count);
            }
            else
            {
                logger.LogWarning("A busca por assinaturas pendentes falhou para UserId {UserId}. Motivo: {ErrorMessage}", userId, pendingSignaturesResult.Message);
            }
            
            var userTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(userId, StatusTccType.IN_PROGRESS.ToString()));
            inProgressTccsCount = userTccs.Count;
        }
        else if (profiles.Contains(RoleType.STUDENT.ToString()))
        {
            logger.LogInformation("Executando lógica de busca para perfil ESTUDANTE. UserId: {UserId}", userId);
            var pendingSignaturesResult = await findAllPendingSignaturesUseCase.Execute(userId);
            if (pendingSignaturesResult.IsSuccess)
            {
                pendingSignaturesCount = pendingSignaturesResult.Data.Sum(p => p.PendingDetails.Count);
            }
            else
            {
                logger.LogWarning("A busca por assinaturas pendentes falhou para UserId {UserId}. Motivo: {ErrorMessage}", userId, pendingSignaturesResult.Message);
            }
            
            var userTccs = await tccGateway.FindAllTccByFilter(new TccFilterDTO(userId, StatusTccType.IN_PROGRESS.ToString()));
            inProgressTccsCount = userTccs.Any() ? 1 : 0;
        }

        var resultDto = new GetInfoHomeDTO(pendingSignaturesCount, inProgressTccsCount);
        logger.LogInformation("Busca de informações da home concluída para UserId {UserId}. AssinaturasPendentes: {PendingSignatures}, TCCsEmAndamento: {InProgressTccs}", userId, resultDto.PendingSignature, resultDto.TccInprogress);
        
        return ResultPattern<GetInfoHomeDTO>.SuccessResult(resultDto);
    }
}