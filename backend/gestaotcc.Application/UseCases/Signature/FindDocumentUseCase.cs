using System.Globalization;
using System.Text;
using gestaotcc.Application.Gateways;
using gestaotcc.Domain.Dtos.Signature;
using gestaotcc.Domain.Dtos.User;
using gestaotcc.Domain.Entities.DocumentType;
using gestaotcc.Domain.Entities.Tcc;
using gestaotcc.Domain.Entities.User;
using gestaotcc.Domain.Entities.UserTcc;
using gestaotcc.Domain.Enums;
using gestaotcc.Domain.Errors;

namespace gestaotcc.Application.UseCases.Signature;

public class FindDocumentUseCase(ITccGateway tccGateway, IMinioGateway minioGateway, IUserGateway userGateway, IAppLoggerGateway<FindDocumentUseCase> logger)
{
    //verificar
    public async Task<ResultPattern<FindDocumentDTO>> Execute(long tccId, long documentId, long studentId)
    {
        logger.LogInformation("Iniciando busca de documento para TccId: {TccId}, DocumentId: {DocumentId}, StudentId: {StudentId}", tccId, documentId, studentId);
        
        var tcc = await tccGateway.FindTccById(tccId);
        if (tcc is null)
        {
            logger.LogWarning("Falha na busca: TCC não encontrado para o TccId: {TccId}", tccId);
            return ResultPattern<FindDocumentDTO>.FailureResult("Erro ao realizar download do documento", 404);
        }
        
        logger.LogInformation("TCC encontrado. TccId: {TccId}", tccId);

        var isSign = tcc.Documents.Any(doc => doc.Signatures.Any(sig => sig.DocumentId == documentId));
        logger.LogInformation("Verificação de assinatura para DocumentId {DocumentId}: IsSigned = {IsSigned}", documentId, isSign);
        
        var templateDocument = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.DocumentType;
        var document = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.FileName + ".pdf";

        var fields = new Dictionary<string, string>();
        if (!isSign)
        {
            logger.LogInformation("Documento não assinado. Iniciando processo de preenchimento de dados do template.");
            var supervisorsUser = await userGateway.FindAllByFilter(new UserFilterDTO(null, null, null, RoleType.ADVISOR.ToString()));
            logger.LogInformation("Encontrados {SupervisorCount} usuários com perfil de supervisor.", supervisorsUser.Count);
            
            var onlySupervisorUser = supervisorsUser
                .FirstOrDefault(u => u.Profile
                    .Any(p => p.Role != RoleType.COORDINATOR.ToString() && p.Role != RoleType.ADMIN.ToString() && p.Role == RoleType.ADVISOR.ToString()));
            
            logger.LogInformation("Usuário supervisor selecionado: {SupervisorName} (Id: {SupervisorId})", onlySupervisorUser!.Name, onlySupervisorUser.Id);
            fields = FillDataFile(studentId, tcc, templateDocument, tcc.UserTccs, onlySupervisorUser!);
            logger.LogInformation("Processo de preenchimento de dados concluído. {FieldCount} campos foram preenchidos.", fields.Count);
        }

        var isTemplateOrDocument = isSign ? document : templateDocument.Name + ".pdf";
        logger.LogInformation("Solicitando URL pré-assinada do Minio para o arquivo: {FileName} com {FieldCount} campos de substituição.", isTemplateOrDocument, fields.Count);

        var documentUrl = await minioGateway.GetPresignedUrl(isTemplateOrDocument, fields, isSign);
        logger.LogInformation("URL pré-assinada para TccId {TccId} recebida com sucesso.", tccId);

        return ResultPattern<FindDocumentDTO>.SuccessResult(new FindDocumentDTO(documentUrl));
    }

    private Dictionary<string, string> FillDataFile(
        long studentUserId,
        TccEntity tcc,
        DocumentTypeEntity documentTypeEntity,
        ICollection<UserTccEntity> usersTccEntity,
        UserEntity supervisorUser)
    {
        logger.LogDebug("Iniciando FillDataFile para o DocumentTypeId: {DocumentTypeId}", documentTypeEntity.Id);
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
                fields["curso_orientador"] = advisor?.CampiCourse?.Course.Name ?? "";
                fields["universidade_curso_orientador"] = "";
                fields["email_orientador"] = advisor?.Email ?? "";
                fields["telefone_orientador"] = "";
                fields["titulo_orientador"] = "";
                AddCommonDateFields(fields, nowDate);

                for (var i = 0; i < students.Count; i++)
                {
                    fields[$"nome_orientando_{i + 1}"] = students[i].Name ?? "";
                    fields[$"curso_orientando_{i + 1}"] = students[i].CampiCourse?.Course.Name ?? "";
                    fields[$"turma_orientando_{i + 1}"] = "";
                    fields[$"ano_orientando_{i + 1}"] = "";
                    fields[$"turno_orientando_{i + 1}"] = "";
                    fields[$"email_orientando_{i + 1}"] = students[i].Email ?? "";
                    fields[$"telefone_orientando_{i + 1}"] = "";
                }

                break;

            case 2:
                fields["nome_orientador"] = advisor?.Name ?? "";
                fields["curso_orientador"] = advisor?.CampiCourse?.Course.Name ?? "";
                fields["universidade_orientador"] = "Instituto Federal de Pernambuco - Campus Belo Jardim";
                fields["nome_orientando"] = student?.Name ?? "";
                fields["email_orientador"] = advisor?.Email ?? "";
                fields["telefone_orientador"] = "";
                fields["titulo_orientador"] = "";
                fields["curso_orientando"] = student?.CampiCourse?.Course.Name ?? "";
                fields["turma_orientando"] = "";
                fields["ano_orientando"] = "";
                fields["email_orientando"] = student?.Email ?? "";
                fields["telefone_orientando"] = "";
                AddCommonDateFields(fields, nowDate);
                break;

            case 3:
                fields["nome_orientando"] = student?.Name ?? "";
                fields["curso_orientando"] = student?.CampiCourse?.Course.Name ?? "";
                fields["titulo_tcc"] = tccTitle;
                fields["nome_orientador"] = advisor?.Name ?? "";
                AddCommonDateFields(fields, nowDate);
                break;

            case 4:
                fields["nome_orientando"] = student?.Name ?? "";
                fields["curso_orientando"] = student?.CampiCourse?.Course.Name ?? "";
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
                fields["curso_orientando"] = student?.CampiCourse?.Course.Name ?? "";
                fields["universidade_cidade_orientando"] = "";
                fields["nome_orientador"] = advisor?.Name ?? "";
                AddCommonDateFields(fields, nowDate);
                break;

            case 6:
                fields["nome_orientando"] = student?.Name ?? "";
                fields["curso_orientando"] = student?.CampiCourse?.Course.Name ?? "";
                fields["titulo_tcc"] = tccTitle;
                fields["nome_orientador"] = advisor?.Name ?? "";
                AddCommonDateFields(fields, nowDate);
                break;

            case 7:
                var (tccTitle1, tccTitle2) = SplitTitle(tccTitle, 78);
                var studentNames = string.Join(", ", students.Select(s => s.Name));

                fields["curso_supervisor"] = supervisorUser.CampiCourse?.Course.Name ?? "";
                fields["universidade_supervisor"] = "";
                fields["titulo_tcc_1"] = tccTitle1;
                fields["titulo_tcc_2"] = tccTitle2;
                fields["orientandos"] = studentNames;
                fields["curso_orientando"] = student?.CampiCourse?.Course.Name ?? "";
                fields["dia_apresentacao"] = tccSchedule.ScheduledDate.Day.ToString();
                fields["mes_apresentacao"] = tccSchedule.ScheduledDate.Month.ToString();
                fields["ano_apresentacao"] = tccSchedule.ScheduledDate.Year.ToString();
                fields["hora_apresentacao"] = tccSchedule.ScheduledDate.Hour.ToString();
                fields["minuto_apresentacao"] = tccSchedule.ScheduledDate.Minute.ToString();
                fields["local_apresentacao"] = tccSchedule.Location ?? "";
                AddCommonDateFields(fields, nowDate);
                break;
        }

        logger.LogDebug("FillDataFile concluído para DocumentTypeId: {DocumentTypeId}. {FieldCount} campos preenchidos.", documentTypeEntity.Id, fields.Count);
        return fields;
    }

    private void AddCommonDateFields(Dictionary<string, string> fields, DateTime date)
    {
        fields["cidade"] = "";
        fields["dia"] = date.Day.ToString();
        fields["mes"] = date.ToString("MMMM", new CultureInfo("pt-BR"));
        fields["ano"] = date.Year.ToString();
    }
    
    private (string, string) SplitTitle(string title, int maxLength)
    {
        if (string.IsNullOrEmpty(title))
            return ("", "");

        return title.Length <= maxLength
            ? (title, "")
            : (title.Substring(0, maxLength), title.Substring(maxLength));
    }
}