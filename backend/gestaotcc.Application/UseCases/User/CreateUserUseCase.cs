using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.Helpers;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;
public class CreateUserUseCase(IUserGateway userGateway, IProfileGateway profileGateway, IEmailGateway emailGateway, ICourseGateway courseGateway, ITccGateway tccGateway, CreateAccessCodeUseCase createAccessCodeUseCase)
{
    public async Task<ResultPattern<UserEntity>> Execute(CreateUserDTO data, string combination)
    {
        var user = await userGateway.FindByEmail(data.Email);
        if (user is not null)
            return ResultPattern<UserEntity>.FailureResult("Erro ao cadastrar usuário; Por favor verifique as informações e tente novamente", 404);

        var expandedProfileRoles = ProfileHelper.ExpandProfiles(data.Profile);

        var profile = await profileGateway.FindByRole(expandedProfileRoles);
        var course = await courseGateway.FindByName(data.Course);
        var accessCode = createAccessCodeUseCase.Execute(combination);

        var newUser = UserFactory.CreateUser(data, profile, course, accessCode.Data);
        await userGateway.Save(newUser);

        var isOnlyAluno = expandedProfileRoles.Count == 1 && expandedProfileRoles.Contains("STUDENT");

        if (isOnlyAluno)
        {
            var tccInvite = await tccGateway.FindInviteTccByEmail(data.Email);
            if (tccInvite is not null)
            {
                var tcc = await tccGateway.FindTccById(tccInvite.TccId);
                if (tcc is not null)
                {
                    TccFactory.UpdateUsersTcc(tcc, newUser);
                    await tccGateway.Update(tcc);
                }
            }

            var sendStudent = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(newUser, "ADD-USER-TCC"));
            if (sendStudent.IsFailure)
                return ResultPattern<UserEntity>.FailureResult(sendStudent.Message, 500);
        }
        else
        {
            var sendOthers = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(newUser, "CREATE-USER"));
            if (sendOthers.IsFailure)
                return ResultPattern<UserEntity>.FailureResult(sendOthers.Message, 500);

        }
        return ResultPattern<UserEntity>.SuccessResult(newUser);
    }
}
