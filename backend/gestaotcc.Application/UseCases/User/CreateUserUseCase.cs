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
        var isOnlyAluno = expandedProfileRoles.Count == 1 && expandedProfileRoles.Contains("STUDENT");
        var courseId = data.CourseId;
        var campiId = data.CampusId;

        if (isOnlyAluno)
        {
            logger.LogInformation("Usuário {UserEmail} é um estudante que ainda não está no sistema. Pegando informação do Campus e Curso", data.Email);
            var userInvite = await tccGateway.FindInviteTccByEmail(data.Email);
            if (userInvite is not null)
            {
                courseId = userInvite.CourseId;
                campiId = userInvite.CampiId;
                
            }
        }

        logger.LogInformation("Criando nova entidade de usuário para {UserEmail}...", data.Email);
        var campiCourse = await courseGateway.FindByCampiAndCourseId(campiId, courseId);
        var newUser = UserFactory.CreateUser(data, profile, campiCourse, accessCode.Data);
        await userGateway.Save(newUser);
        logger.LogInformation("Usuário {UserEmail} salvo com sucesso no banco de dados. Novo UserId: {UserId}",
            newUser.Email, newUser.Id);


        if (isOnlyAluno)
        {
            logger.LogInformation("Usuário {UserId} é apenas um estudante. Executando fluxo de vinculação de TCC.",
                newUser.Id);
            var tccInvite = await tccGateway.FindInviteTccByEmail(data.Email);
            var profileEntity = await profileGateway.FindByRole("STUDENT");
            if (tccInvite is not null)
            {
                var tcc = await tccGateway.FindTccById(tccInvite.TccId);
                if (tcc is not null)
                {
                    logger.LogInformation(
                        "TCC {TccId} encontrado para o convite. Vinculando usuário e criando documentos...", tcc.Id);
                    TccFactory.UpdateUsersTccToCreateUser(tcc, newUser, profileEntity!);
                    CreateDocumentForUser(newUser, documentTypes, tcc.Documents, tcc.Title);

                    var allIAlreadyAdded = tcc.TccInvites.Any(inv => inv.IsValidCode);
                    if (!allIAlreadyAdded)
                    {
                        logger.LogInformation(
                            "Todos os convites foram aceitos. Avançando TccId {TccId} para a etapa {NewStep}.", tcc.Id,
                            StepTccType.START_AND_ORGANIZATION.ToString());
                        tcc.Step = StepTccType.START_AND_ORGANIZATION.ToString();
                    }

                    await tccGateway.Update(tcc);
                    logger.LogInformation("TCC {TccId} atualizado com sucesso.", tcc.Id);
                }
            }

            var sendStudent = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(newUser, "ADD-USER-TCC"));
            if (sendStudent.IsFailure)
            {
                logger.LogError("Falha ao enviar e-mail 'ADD-USER-TCC' para {UserEmail}. Motivo: {ErrorMessage}",
                    newUser.Email, sendStudent.Message);
                return ResultPattern<UserEntity>.FailureResult(sendStudent.Message, 500);
            }
        }
        else
        {
            logger.LogInformation("Usuário {UserId} não é apenas um estudante. Executando fluxo de criação padrão.",
                newUser.Id);
            var sendOthers = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(newUser, "CREATE-USER"));
            if (sendOthers.IsFailure)
            {
                logger.LogError("Falha ao enviar e-mail 'CREATE-USER' para {UserEmail}. Motivo: {ErrorMessage}",
                    newUser.Email, sendOthers.Message);
                return ResultPattern<UserEntity>.FailureResult(sendOthers.Message, 500);
            }
        }

        logger.LogInformation("Criação do usuário {UserEmail} (UserId: {UserId}) concluída com sucesso.", newUser.Email,
            newUser.Id);
        return ResultPattern<UserEntity>.SuccessResult(newUser);
    }

    private void CreateDocumentForUser(UserEntity user, List<DocumentTypeEntity> documentTypes,
        ICollection<DocumentEntity> documents, string tccTitle)
    {
        logger.LogInformation("Iniciando processo de criação de documentos para o novo usuário UserId: {UserId}",
            user.Id);
        foreach (var docType in documentTypes)
        {
            logger.LogDebug("Avaliando DocumentType '{DocTypeName}' para UserId {UserId}", docType.Name, user.Id);
            var acceptedRoles = docType.Profiles.Select(p => p.Role).ToHashSet();
            var method = Enum.Parse<MethoSignatureType>(docType.MethodSignature);

            var hasAcceptedProfile = user.Profile.Any(p => acceptedRoles.Contains(p.Role));

            if (!hasAcceptedProfile)
            {
                logger.LogDebug("Usuário UserId {UserId} não tem perfil para '{DocTypeName}'. Pulando.", user.Id,
                    docType.Name);
                continue;
            }

            if (method == MethoSignatureType.ONLY_DOCS)
            {
                if (docType.Profiles.Count > 1)
                {
                    if (documents.Any(doc => doc.DocumentTypeId == docType.Id && doc.UserId is null))
                        continue;

                    documents.Add(DocumentFactory.CreateDocument(docType, tccTitle, null));
                    logger.LogDebug("Documento compartilhado do tipo '{DocTypeName}' criado.", docType.Name);
                }
                else if (docType.Profiles.Count == 1)
                {
                    documents.Add(DocumentFactory.CreateDocument(docType, tccTitle, user));
                    logger.LogDebug("Documento individual do tipo '{DocTypeName}' criado para UserId {UserId}.",
                        docType.Name, user.Id);
                }
            }
            else if (method == MethoSignatureType.NOT_ONLY_DOCS)
            {
                documents.Add(DocumentFactory.CreateDocument(docType, tccTitle, user));
                logger.LogDebug("Documento do tipo '{DocTypeName}' criado para UserId {UserId} (NOT_ONLY_DOCS).",
                    docType.Name, user.Id);
            }
        }
    }
}