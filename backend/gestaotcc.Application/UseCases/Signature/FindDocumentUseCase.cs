using System.Globalization;
using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.DocumentTypeFormFieldData;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class FindDocumentUseCase(ITccGateway tccGateway, IMinioGateway minioGateway, IUserGateway userGateway)
{
    public async Task<ResultPattern<FindDocumentDTO>> Execute(long tccId, long documentId, long studentId)
    {
        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
            return ResultPattern<FindDocumentDTO>.FailureResult("Erro ao realizar download do documento", 404);

        var isSign = tcc.Documents.Any(doc => doc.Signatures.Any(sig => sig.DocumentId == documentId));
        var templateDocument = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.DocumentType;
        var document = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.FileName + ".pdf";

        var fields = new Dictionary<string, string>();
        if (!isSign)
        {
            var supervisorsUser =
                await userGateway.FindAllByFilter(new UserFilterDTO(null, null, null, RoleType.ADVISOR.ToString()));
            
            var onlySupervisorUser = supervisorsUser // Usuário que é supervisor, mas que não é Admin ou coordenador, pois admin e coordenador também tem acesso de supervisor
                .FirstOrDefault(u => u.Profile
                    .Any(p => p.Role != RoleType.COORDINATOR.ToString() && p.Role != RoleType.ADMIN.ToString() && p.Role == RoleType.ADVISOR.ToString()));
            fields = FillDataFile(studentId, tcc, templateDocument, tcc.UserTccs, onlySupervisorUser!);
        }

        var isTemplateOrDocument = isSign ? document : templateDocument.Name + ".pdf";

        var documentUrl = await minioGateway.GetPresignedUrl(isTemplateOrDocument, fields, isSign);

        return ResultPattern<FindDocumentDTO>.SuccessResult(new FindDocumentDTO(documentUrl));
    }

    private Dictionary<string, string> FillDataFile(
        long studentUserId,
        TccEntity tcc,
        DocumentTypeEntity documentTypeEntity,
        ICollection<UserTccEntity> usersTccEntity,
        UserEntity supervisorUser)
    {
        var fields = new Dictionary<string, string>();
        var advisor = usersTccEntity.FirstOrDefault(ut => ut.Profile.Role == RoleType.ADVISOR.ToString())?.User;
        var student = usersTccEntity.FirstOrDefault(ut => ut.UserId == studentUserId)?.User;
        var students = usersTccEntity
            .Where(ut => ut.Profile.Role != RoleType.ADVISOR.ToString())
            .Select(ut => ut.User)
            .ToList();

        var tccTitle = tcc.Title ?? string.Empty;
        var tccSchedule = tcc.TccSchedule;
        var nowDate = DateTime.Now;

        switch (documentTypeEntity.Id)
        {
            case 1:
                fields["nome_orientador"] = advisor?.Name ?? "";
                fields["curso_orientador"] = advisor?.Course?.Name ?? "";
                fields["universidade_curso_orientador"] = "";
                fields["email_orientador"] = advisor?.Email ?? "";
                fields["telefone_orientador"] = "";
                fields["titulo_orientador"] = "";

                for (var i = 0; i < students.Count; i++)
                {
                    fields[$"nome_orientando_{i + 1}"] = students[i].Name ?? "";
                    fields[$"curso_orientando_{i + 1}"] = students[i].Course?.Name ?? "";
                    fields[$"turma_orientando_{i + 1}"] = "";
                    fields[$"ano_orientando_{i + 1}"] = "";
                    fields[$"turno_orientando_{i + 1}"] = "";
                    fields[$"email_orientando_{i + 1}"] = students[i].Email ?? "";
                    fields[$"telefone_orientando_{i + 1}"] = "";
                }

                break;

            case 2:
                fields["nome_orientador"] = advisor?.Name ?? "";
                fields["curso_orientador"] = advisor?.Course?.Name ?? "";
                fields["universidade_orientador"] = "Instituto Federal de Pernambuco - Campus Belo Jardim";
                fields["nome_orientando"] = student?.Name ?? "";
                fields["email_orientador"] = advisor?.Email ?? "";
                fields["telefone_orientador"] = "";
                fields["titulo_orientador"] = "";
                fields["curso_orientando"] = student?.Course?.Name ?? "";
                fields["turma_orientando"] = "";
                fields["ano_orientando"] = "";
                fields["email_orientando"] = student?.Email ?? "";
                fields["telefone_orientando"] = "";
                AddCommonDateFields(fields, nowDate);
                break;

            case 3:
                fields["nome_orientando"] = student?.Name ?? "";
                fields["curso_orientando"] = student?.Course?.Name ?? "";
                fields["titulo_tcc"] = tccTitle;
                fields["nome_orientador"] = advisor?.Name ?? "";
                AddCommonDateFields(fields, nowDate);
                break;

            case 4:
                fields["nome_orientando"] = student?.Name ?? "";
                fields["curso_orientando"] = student?.Course?.Name ?? "";
                fields["turma_orientando"] = "";
                fields["ano_orientando"] = "";
                fields["data_apresentacao"] = tccSchedule.ScheduledDate.ToString("dd/MM/yyyy");
                fields["local_apresentacao"] = tccSchedule.Location ?? "";
                fields["nome_orientador"] = advisor?.Name ?? "";
                fields["titulo_tcc"] = tccTitle;
                AddCommonDateFields(fields, nowDate);
                break;

            case 5:
                fields["nome_orientando"] = student?.Name ?? "";
                fields["matricula_orientando"] = "";
                fields["curso_orientando"] = student?.Course?.Name ?? "";
                fields["universidade_cidade_orientando"] = "";
                fields["nome_orientador"] = advisor?.Name ?? "";
                AddCommonDateFields(fields, nowDate);
                break;

            case 6:
                fields["nome_orientando"] = student?.Name ?? "";
                fields["curso_orientando"] = student?.Course?.Name ?? "";
                fields["titulo_tcc"] = tccTitle;
                fields["nome_orientador"] = advisor?.Name ?? "";
                AddCommonDateFields(fields, nowDate);
                break;

            case 7:
                var (tccTitle1, tccTitle2) = SplitTitle(tccTitle, 78);
                var studentNames = string.Join(", ", students.Select(s => s.Name));

                fields["curso_supervisor"] = supervisorUser.Course?.Name ?? "";
                fields["universidade_supervisor"] = "";
                fields["titulo_tcc_1"] = tccTitle1;
                fields["titulo_tcc_2"] = tccTitle2;
                fields["orientandos"] = studentNames;
                fields["curso_orientando"] = student?.Course?.Name ?? "";
                fields["dia_apresentacao"] = tccSchedule.ScheduledDate.Day.ToString();
                fields["mes_apresentacao"] = tccSchedule.ScheduledDate.Month.ToString();
                fields["ano_apresentacao"] = tccSchedule.ScheduledDate.Year.ToString();
                fields["hora_apresentacao"] = tccSchedule.ScheduledDate.Hour.ToString();
                fields["minuto_apresentacao"] = tccSchedule.ScheduledDate.Minute.ToString();
                fields["local_apresentacao"] = tccSchedule.Location ?? "";
                AddCommonDateFields(fields, nowDate);
                break;
        }

        return fields;
    }

    private void AddCommonDateFields(Dictionary<string, string> fields, DateTime date)
    {
        fields["cidade"] = "";
        fields["dia"] = date.Day.ToString();
        fields["mes"] = date.ToString("MMMM", new CultureInfo("pt-BR"));
        fields["ano"] = date.Year.ToString();
    }

    /// <summary>
    /// Divide um título longo em duas partes, respeitando o tamanho máximo
    /// </summary>
    private (string, string) SplitTitle(string title, int maxLength)
    {
        if (string.IsNullOrEmpty(title))
            return ("", "");

        return title.Length <= maxLength
            ? (title, "")
            : (title.Substring(0, maxLength), title.Substring(maxLength));
    }
}