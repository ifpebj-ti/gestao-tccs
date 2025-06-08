using gestaotcc.Application.Factories;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class SendPendingSignatureUseCase(ITccGateway tccGateway, IEmailGateway emailGateway, IDocumentTypeGateway documentTypeGateway)
{
    public async Task<ResultPattern<string>> Execute()
    {
        var filter = "IN_PROGRESS";
        var tccs = await tccGateway.FindAllTccByFilter(filter);
        var documentTypes = await documentTypeGateway.FindAll();

        var stepSignatureOrderMap = new Dictionary<StepTccType, int>
        {
            { StepTccType.PROPOSAL_REGISTRATION, 1 },
            { StepTccType.START_AND_ORGANIZATION, 2 },
            { StepTccType.DEVELOPMENT_AND_MONITORING, 3 },
            { StepTccType.PREPARATION_FOR_PRESENTATION, 4 },
            { StepTccType.PRESENTATION_AND_EVALUATION, 5 },
            { StepTccType.FINALIZATION_AND_PUBLICATION, 6 }
        };

        var usersToNotify = new Dictionary<string, SendPendingSignatureDTO>();

        foreach (var tcc in tccs)
        {
            if (!Enum.TryParse<StepTccType>(tcc.Step, out var tccStep)) continue;
            if (tccStep == StepTccType.PROPOSAL_REGISTRATION) continue;

            var currentStepOrder = stepSignatureOrderMap[tccStep];

            var documents = tcc.Documents
                .Where(d => d.DocumentType.SignatureOrder == currentStepOrder)
                .ToList();

            var expectedDocumentTypes = documentTypes
                .Where(dt => dt.SignatureOrder == currentStepOrder)
                .ToList();

            var userTccsOrdered = tcc.UserTccs
                .OrderBy(ut => ut.User.Profile.MinBy(p => p.Id).Id)
                .ThenBy(ut => ut.Id)
                .ToList();

            if (!documents.Any())
            {
                var firstUser = userTccsOrdered.First().User;
                AddOrUpdateUser(usersToNotify, firstUser.Email, firstUser.Name, tcc.Title!, expectedDocumentTypes.Select(dt => dt.Name));

                continue;
            }

            foreach (var docType in expectedDocumentTypes)
            {
                var document = documents.FirstOrDefault(d => d.DocumentType.Id == docType.Id);

                if (document == null)
                {
                    var firstUser = userTccsOrdered.First().User;
                    AddOrUpdateUser(usersToNotify, firstUser.Email, firstUser.Name, tcc.Title!, new List<string> { docType.Name });

                    continue;
                }

                foreach (var userTcc in userTccsOrdered)
                {
                    var user = userTcc.User;

                    if (document.Signatures.Any(s => s.UserId == user.Id))
                        continue;

                    var index = userTccsOrdered.IndexOf(userTcc);
                    var previousUsers = userTccsOrdered.Take(index);

                    var allPreviousSigned = previousUsers.All(prev =>
                        document.Signatures.Any(sig => sig.UserId == prev.User.Id));

                    if (allPreviousSigned)
                    {
                        AddOrUpdateUser(usersToNotify, user.Email, user.Name, tcc.Title!, new List<string> { docType.Name });
                    }

                    break;
                }
            }
        }

        foreach (var dto in usersToNotify.Values)
        {
            var emailDto = EmailFactory.CreateSendEmailDTO(dto);
            await emailGateway.Send(emailDto);
        }

        return ResultPattern<string>.SuccessResult();
    }

    private void AddOrUpdateUser(Dictionary<string, SendPendingSignatureDTO> map, string email, string name, string tccTitle, IEnumerable<string> docNames)
    {
        if (map.ContainsKey(email))
        {
            var existing = map[email];
            var updatedList = existing.DocumentNames.Union(docNames).Distinct().ToList();
            map[email] = new SendPendingSignatureDTO(email, name, updatedList, tccTitle);
        }
        else
        {
            map[email] = new SendPendingSignatureDTO(email, name, docNames.ToList(), tccTitle);
        }
    }



}