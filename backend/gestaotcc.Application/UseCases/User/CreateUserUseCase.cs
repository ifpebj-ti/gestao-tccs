using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Application.Helpers;
using gestaotcc.Application.UseCases.AccessCode;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.Document;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.User;

public class CreateUserUseCase(
    IUserGateway userGateway,
    IProfileGateway profileGateway,
    IEmailGateway emailGateway,
    ICourseGateway courseGateway,
    ITccGateway tccGateway,
    IDocumentTypeGateway documentTypeGateway,
    CreateAccessCodeUseCase createAccessCodeUseCase,
    IAppLoggerGateway<CreateUserUseCase> logger)
{
    public async Task<ResultPattern<UserEntity>> Execute(CreateUserDTO data, string combination)
    {
        logger.LogInformation("Iniciando criação de usuário para o e-mail: {UserEmail} com perfis: [{Profiles}]",
            data.Email, string.Join(", ", data.Profile));

        var user = await userGateway.FindByEmail(data.Email);
        if (user is not null)
        {
            logger.LogWarning("Falha na criação: Usuário com e-mail {UserEmail} já existe.", data.Email);
            return ResultPattern<UserEntity>.FailureResult(
                "Erro ao cadastrar usuário; Por favor verifique as informações e tente novamente", 404);
        }

        var documentTypes = await documentTypeGateway.FindAll();
        var expandedProfileRoles = ProfileHelper.ExpandProfiles(data.Profile);
        logger.LogInformation("Perfis expandidos para {UserEmail}: [{ExpandedProfiles}]", data.Email,
            string.Join(", ", expandedProfileRoles));

        var profile = await profileGateway.FindByRole(expandedProfileRoles);
        var accessCode = createAccessCodeUseCase.Execute(combination);
        var courseId = data.CourseId;
        var campiId = data.CampusId;

        logger.LogInformation("Criando nova entidade de usuário para {UserEmail}...", data.Email);
        var campiCourse = await courseGateway.FindByCampiAndCourseId(campiId, courseId);
        var newUser = UserFactory.CreateUser(data, profile, campiCourse, accessCode.Data);
        await userGateway.Save(newUser);
        logger.LogInformation("Usuário {UserEmail} salvo com sucesso no banco de dados. Novo UserId: {UserId}",
            newUser.Email, newUser.Id);
        
        logger.LogInformation("Usuário {UserId} não é apenas um estudante. Executando fluxo de criação padrão.",
            newUser.Id);
        var sendOthers = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(newUser, "CREATE-USER"));
        if (sendOthers.IsFailure)
        {
            logger.LogError("Falha ao enviar e-mail 'CREATE-USER' para {UserEmail}. Motivo: {ErrorMessage}",
                newUser.Email, sendOthers.Message);
            return ResultPattern<UserEntity>.FailureResult(sendOthers.Message, 500);
        }

        logger.LogInformation("Criação do usuário {UserEmail} (UserId: {UserId}) concluída com sucesso.", newUser.Email,
            newUser.Id);
        return ResultPattern<UserEntity>.SuccessResult(newUser);
    }
}