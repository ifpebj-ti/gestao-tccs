using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;
public class CreateUserUseCase(IUserGateway userGateway, IProfileGateway profileGateway, IEmailGateway emailGateway, ICourseGateway courseGateway, CreateAccessCodeUseCase createAccessCodeUseCase)
{
    public async Task<ResultPattern<UserEntity>> Execute(CreateUserDTO data, string combination)
    {
        var user = await userGateway.FindByEmail(data.Email);
        if (user is not null)
            return ResultPattern<UserEntity>.FailureResult("Erro ao cadastrar usuário; Por favor verifique as informações e tente novamente", 404);

        var profile = await profileGateway.FindByRole(data.Profile);
        var course = await courseGateway.FindByName(data.Course);
        var accessCode = createAccessCodeUseCase.Execute(combination);

        var newUser = UserFactory.CreateUser(data, profile, course, accessCode.Data /*Colocar parametro de senha aleatoria aqui caso seja usuario docente*/);
        await userGateway.Save(newUser);


        var sendResult = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(newUser, "CREATE-USER"));
        if (sendResult.IsFailure)
            return ResultPattern<UserEntity>.FailureResult(sendResult.Message, 500);

        return ResultPattern<UserEntity>.SuccessResult(newUser);
    }
}
