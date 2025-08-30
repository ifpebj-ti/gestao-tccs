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
    CreateAccessCodeUseCase createAccessCodeUseCase)
{
    public async Task<ResultPattern<UserEntity>> Execute(CreateUserDTO data, string combination)
    {
        var user = await userGateway.FindByEmail(data.Email);
        if (user is not null)
            return ResultPattern<UserEntity>.FailureResult("Erro ao cadastrar usuário; Por favor verifique as informações e tente novamente", 404);

        var documentTypes = await documentTypeGateway.FindAll();
        
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
            var profileEntity = await profileGateway.FindByRole("STUDENT"); // "ALUNO"
            if (tccInvite is not null)
            {
                var tcc = await tccGateway.FindTccById(tccInvite.TccId);
                if (tcc is not null)
                {
                    TccFactory.UpdateUsersTccToCreateUser(tcc, newUser, profileEntity!);
                    CreateDocumentForUser(newUser, documentTypes, tcc.Documents, tcc.Title);
                    
                    var allIAlreadyAdded = tcc.TccInvites.Any(inv => inv.IsValidCode);
                    if(!allIAlreadyAdded)
                        tcc.Step = StepTccType.START_AND_ORGANIZATION.ToString();
                    
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
    
    private void CreateDocumentForUser(UserEntity user, List<DocumentTypeEntity> documentTypes, ICollection<DocumentEntity> documents, string tccTitle)
    {
        foreach (var docType in documentTypes)
        {
            var acceptedRoles = docType.Profiles.Select(p => p.Role).ToHashSet();
            var method = Enum.Parse<MethoSignatureType>(docType.MethodSignature);

            var hasAcceptedProfile = user.Profile.Any(p => acceptedRoles.Contains(p.Role));

            if (!hasAcceptedProfile)
                continue;

            if (method == MethoSignatureType.ONLY_DOCS)
            {
                if (docType.Profiles.Count > 1)
                {
                    if (documents.Any(doc => doc.DocumentTypeId == docType.Id && doc.UserId is null))
                        continue;

                    // Cria 1 documento com User = null
                    documents.Add(DocumentFactory.CreateDocument(docType, tccTitle, null));
                }
                else if (docType.Profiles.Count == 1)
                {
                    // Cria 1 documento com o próprio usuário
                    documents.Add(DocumentFactory.CreateDocument(docType, tccTitle, user));
                }
            }
            else if (method == MethoSignatureType.NOT_ONLY_DOCS)
            {
                // Cria 1 documento com o próprio usuário
                documents.Add(DocumentFactory.CreateDocument(docType, tccTitle, user));
            }
        }
    }
}
