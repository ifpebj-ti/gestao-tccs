using gestaotcc.Application.Gateways;
using gestaotcc.Application.Helpers;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;
public class UpdateUserUseCase(IUserGateway userGateway, IProfileGateway profileGateway, ICourseGateway courseGateway, IAppLoggerGateway<UpdateUserUseCase> logger)
{
    public async Task<ResultPattern<string>> Execute(UpdateUserDTO data)
    {
        logger.LogInformation("Iniciando atualização para usuário com Id: {id}", data.Id);

        var user = await userGateway.FindById(data.Id);
        if(user is null)
        {
            logger.LogWarning("Usuário com id {id} não encontrado", data.Id);
            return ResultPattern<string>.FailureResult("Erro ao atualizar usuário", 500);
        }

        var expandedProfileRoles = ProfileHelper.ExpandProfiles(data.Profile);
        var profile = await profileGateway.FindByRole(expandedProfileRoles);

        var campus = await courseGateway.FindByCampiAndCourseId(data.CampiId, data.CourseId);

        user.Name = data.Name;
        user.Email = data.Email;
        user.Registration = data.Registration;
        user.SIAPE = data.Siape;
        user.CPF = data.Cpf;
        user.CampiCourseId = campus.Id;
        user.Profile = profile;
        user.Status = data.Status;

        await userGateway.Update(user);

        logger.LogInformation("Usuário com id {id} atualizado com sucesso", user.Id);
        return ResultPattern<string>.SuccessResult();
    }
}

