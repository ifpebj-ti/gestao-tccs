using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;
public class FindUserByIdUseCase(IUserGateway userGateway, IAppLoggerGateway<FindUserByIdUseCase> logger)
{
    public async Task<ResultPattern<UserEntity>> Execute(long id, long requesterId)
    {
        logger.LogInformation("Iniciando busca de usuário pelo Id: {UserId}", id);

        var requester = await userGateway.FindById(requesterId);
        if (requester is not { Status: "ACTIVE" }) return Unavailable();

        var roles = requester.Profile.Select(profile => profile.Role).ToHashSet();
        var isSelf = requesterId == id;
        var isAdmin = roles.Contains("ADMIN");
        var canViewCourse = requester.CampiCourseId is > 0 &&
            roles.Any(role => role is "COORDINATOR" or "SUPERVISOR");
        if (!isSelf && !isAdmin && !canViewCourse) return Unavailable();

        var user = isSelf ? requester : await userGateway.FindById(id);
        if (user is null || (!isSelf && !isAdmin && user.CampiCourseId != requester.CampiCourseId))
        {
            logger.LogWarning("Nenhum usuário encontrado para o Id: {UserId}", id);
            return Unavailable();
        }

        logger.LogInformation("Usuário encontrado com sucesso. UserId: {UserId}", user.Id);
        return ResultPattern<UserEntity>.SuccessResult(user);
    }

    private static ResultPattern<UserEntity> Unavailable() =>
        ResultPattern<UserEntity>.FailureResult("Erro ao buscar o usuário. Por favor verifique as informações e tente novamente", 404);
}
