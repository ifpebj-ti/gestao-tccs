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

public class AutoRegisterUseCase(
    IUserGateway userGateway, 
    ITccGateway tccGateway, 
    IProfileGateway profileGateway, 
    ICourseGateway courseGateway,
    IEmailGateway emailGateway,
    IDocumentTypeGateway documentTypeGateway,
    CreateAccessCodeUseCase createAccessCodeUseCase,
    IBcryptGateway bcryptGateway,
    IAppLoggerGateway<AutoRegisterUseCase> logger)
{
    public async Task<ResultPattern<UserEntity>> Execute(AutoRegisterDTO data, string combination)
    {
        logger.LogInformation("Iniciando auto cadastro para o e-mail: {UserEmail}", data.Email);
        var user = await userGateway.FindByEmail(data.Email);
        if (user is not null)
        {
            logger.LogError("Estudante com o e-mail: {UserEmail} já cadastrado", data.Email);
            return ResultPattern<UserEntity>.FailureResult("Estudante já cadastrado no sistema", 409);
        }
        
        var userInvite = await tccGateway.FindInviteTccByEmail(data.Email);
        long campiId = data.CampiId ?? userInvite?.CampiId ?? 1;
        long courseId = data.CourseId ?? userInvite?.CourseId ?? 1;

        var campiCourse = await courseGateway.FindByCampiAndCourseId(campiId, courseId);
        if (campiCourse is null)
        {
            var campis = await courseGateway.FindAllCampis();
            campiCourse = campis.SelectMany(c => c.CampiCourses).FirstOrDefault();
            if (campiCourse is null)
            {
                logger.LogError("Nenhum curso associado encontrado para o campus {CampiId} e curso {CourseId}", campiId, courseId);
                return ResultPattern<UserEntity>.FailureResult("Curso não encontrado para o cadastro", 404);
            }
        }
        
        var expandedProfileRoles = ProfileHelper.ExpandProfiles(["STUDENT"]);
        var profile = await profileGateway.FindByRole(expandedProfileRoles);
        var accessCode = createAccessCodeUseCase.Execute(combination).Data;

        string? hashedPassword = !string.IsNullOrWhiteSpace(data.Password)
            ? bcryptGateway.GenerateHashPassword(data.Password)
            : null;

        logger.LogInformation("Criando nova entidade de usuário para {UserEmail}...", data.Email);
        var newStudent = UserFactory.CreateUser(data, profile, campiCourse, accessCode, hashedPassword);
        
        await userGateway.Save(newStudent);
        logger.LogInformation("Usuário {UserEmail} salvo com sucesso no banco de dados. Novo UserId: {UserId}",
            newStudent.Email, newStudent.Id);
        
        // Se houver convite prévio com TCC, vincula ao TCC existente
        if (userInvite is not null && userInvite.TccId.HasValue && userInvite.TccId.Value > 0)
        {
            logger.LogInformation("Usuário {UserId} possui convite pendente. Executando fluxo de vinculação de TCC.", newStudent.Id);
            var tcc = await tccGateway.FindTccById(userInvite.TccId.Value);
            if (tcc is not null)
            {
                var documentTypes = await documentTypeGateway.FindAll();
                var profileEntity = await profileGateway.FindByRole("STUDENT");
                TccFactory.UpdateUsersTccToCreateUser(tcc, newStudent, profileEntity!);
                CreateDocumentForUser(newStudent, documentTypes, tcc.Documents, tcc.Title);

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

            var sendStudent = await emailGateway.Send(EmailFactory.CreateSendEmailDTO(newStudent, "ADD-USER-TCC"));
            if (sendStudent.IsFailure)
            {
                logger.LogWarning("Falha ao enviar e-mail 'ADD-USER-TCC' para {UserEmail}.", newStudent.Email);
            }
        }

        return ResultPattern<UserEntity>.SuccessResult(newStudent);
    }

    private void CreateDocumentForUser(UserEntity user, List<DocumentTypeEntity> documentTypes,
        ICollection<DocumentEntity> documents, string tccTitle)
    {
        logger.LogInformation("Iniciando processo de criação de documentos para o novo usuário UserId: {UserId}",
            user.Id);
        foreach (var docType in documentTypes)
        {
            if (docType.Name != null && docType.Name.Contains("ANEXO II - TERMO DE COMPROMISSO DE ORIENTAÇÃO VOLUNTÁRIA"))
                continue;

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