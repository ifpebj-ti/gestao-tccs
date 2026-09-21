using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
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
using Scriban;

namespace gestaotcc.Application.UseCases.Signature;

public class FindDocumentUseCase(
    ITccGateway tccGateway, 
    IMinioGateway minioGateway, 
    IUserGateway userGateway, 
    IITextGateway iTextGateway,
    IAppLoggerGateway<FindDocumentUseCase> logger)
{
    public virtual async Task<ResultPattern<FindDocumentDTO>> Execute(long tccId, long documentId, long? studentId, long campiCourseId, bool returnSignedPdfIfAvailable = false)
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
        var documentFileName = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.FileName + ".pdf";

        if (isSign && returnSignedPdfIfAvailable)
        {
            logger.LogInformation("Documento já assinado. Baixando arquivo assinado do Minio.");
            
            // Tenta baixar com .pdf primeiro (legado), se falhar tenta com .html
            byte[] signedBytes;
            try 
            {
                signedBytes = await minioGateway.Download(documentFileName, true);
            }
            catch 
            {
                var htmlFileName = tcc.Documents.FirstOrDefault(doc => doc.Id == documentId)!.FileName + ".html";
                signedBytes = await minioGateway.Download(htmlFileName, true);
            }
            
            var documentUrlBase64 = Convert.ToBase64String(signedBytes);
            
            // Detecta se é HTML (começa com '<' ou 'PGh0' em base64)
            bool isActuallyHtml = documentUrlBase64.StartsWith("PGh0") || documentUrlBase64.StartsWith("PCFET0");
            
            return ResultPattern<FindDocumentDTO>.SuccessResult(new FindDocumentDTO(documentUrlBase64, isActuallyHtml));
        }
        else
        {
            logger.LogInformation("Documento não assinado. Retornando HTML gerado pelo Scriban.");
            
            var supervisorsUser = await userGateway.FindAllByFilter(new UserFilterDTO(null, null, null, RoleType.SUPERVISOR.ToString()), campiCourseId);
            var onlySupervisorUser = supervisorsUser
                .FirstOrDefault(u => u.Profile.Any(p => p.Role != RoleType.COORDINATOR.ToString() && p.Role != RoleType.ADMIN.ToString() && p.Role == RoleType.SUPERVISOR.ToString()));
            
            var directory = Directory.GetCurrentDirectory();
            var templatePath = Path.Combine(directory, "Templates", "Documents", $"{templateDocument.Name}.html");

            if (!File.Exists(templatePath))
            {
                logger.LogWarning("Template HTML não encontrado no caminho: {TemplatePath}. Tentando arquivo padrão.", templatePath);
                templatePath = Path.Combine(directory, "Templates", "Documents", "default-template.html");
            }

            var htmlContent = await File.ReadAllTextAsync(templatePath);

            var templateData = BuildTemplateVariables(studentId, tcc, templateDocument, tcc.UserTccs, onlySupervisorUser!);

            var template = Template.Parse(htmlContent);
            var renderedHtml = template.Render(templateData);

            return ResultPattern<FindDocumentDTO>.SuccessResult(new FindDocumentDTO(renderedHtml, true));
        }
    }

    private object BuildTemplateVariables(
        long? studentUserId,
        TccEntity tcc,
        DocumentTypeEntity documentTypeEntity,
        ICollection<UserTccEntity> usersTccEntity,
        UserEntity supervisorUser)
    {
        var advisor = usersTccEntity.FirstOrDefault(ut => ut.Profile.Role == RoleType.ADVISOR.ToString())?.User;
        var student = usersTccEntity.FirstOrDefault(ut => ut.UserId == studentUserId)?.User 
                      ?? usersTccEntity.FirstOrDefault(ut => ut.Profile.Role != RoleType.ADVISOR.ToString())?.User;
        var students = usersTccEntity
            .Where(ut => ut.Profile.Role != RoleType.ADVISOR.ToString())
            .Select(ut => ut.User)
            .ToList();

        var tccTitle = tcc.Title ?? string.Empty;
        var tccSchedule = tcc.TccSchedule;
        var nowDate = DateTime.Now;

        var semester = (tcc.CreationDate.Month <= 6) ? 1 : 2;
        var formattedSemester = $"{tcc.CreationDate.Year}.{semester}";

        string[] mesesPtBr = { 
            "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", 
            "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" 
        };

        var (tccTitle1, tccTitle2) = SplitTitle(tccTitle, 78);

        var encoder = HtmlEncoder.Default;

        string oral1 = "", oral2 = "", oral3 = "";
        string textual1 = "", textual2 = "", textual3 = "";
        string totalOral = "", totalTextual = "", notaFinal = "";

        if (tcc.BankingMembers != null && tcc.BankingMembers.Any(m => m.Grade.HasValue))
        {
            var avaliacoes = tcc.BankingMembers.Where(m => m.Grade.HasValue).ToList();
            decimal sumOral = 0m, sumTextual = 0m;
            int countOral = 0, countTextual = 0;

            for (int i = 0; i < avaliacoes.Count; i++)
            {
                var member = avaliacoes[i];
                decimal memberOral = 0m;
                decimal memberTextual = 0m;

                if (!string.IsNullOrEmpty(member.EvaluationDetails))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(member.EvaluationDetails);
                        if (doc.RootElement.TryGetProperty("oral", out var oralObj))
                        {
                            foreach (var prop in oralObj.EnumerateObject())
                            {
                                if (decimal.TryParse(prop.Value.GetString()?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                                    memberOral += val;
                            }
                        }
                        if (doc.RootElement.TryGetProperty("textual", out var textualObj))
                        {
                            foreach (var prop in textualObj.EnumerateObject())
                            {
                                if (decimal.TryParse(prop.Value.GetString()?.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
                                    memberTextual += val;
                            }
                        }
                    }
                    catch { }
                }
                
                string oralStr = memberOral.ToString("0.00");
                string textualStr = memberTextual.ToString("0.00");

                if (i == 0) { oral1 = oralStr; textual1 = textualStr; }
                if (i == 1) { oral2 = oralStr; textual2 = textualStr; }
                if (i == 2) { oral3 = oralStr; textual3 = textualStr; }

                sumOral += memberOral;
                countOral++;
                sumTextual += memberTextual;
                countTextual++;
            }

            if (countOral > 0) totalOral = Math.Round(sumOral / countOral, 2).ToString("0.00");
            if (countTextual > 0) totalTextual = Math.Round(sumTextual / countTextual, 2).ToString("0.00");
            if (avaliacoes.Any()) notaFinal = Math.Round(avaliacoes.Average(m => m.Grade.Value), 2).ToString("0.00");
        }

        // Retorna um objeto dinâmico / anônimo contendo todas as chaves acessíveis pelo Scriban no HTML
        return new
        {
            nome_orientador = encoder.Encode(advisor?.Name ?? ""),
            curso_orientador = encoder.Encode(advisor?.CampiCourse?.Course.Name ?? ""),
            universidade_curso_orientador = encoder.Encode(advisor?.CampiCourse?.Campi.City ?? ""),
            universidade_orientador = encoder.Encode(advisor?.CampiCourse?.Campi.Name ?? ""),
            email_orientador = encoder.Encode(advisor?.Email ?? ""),
            telefone_orientador = encoder.Encode(advisor?.Phone ?? ""),
            titulo_orientador = encoder.Encode(advisor?.Titration ?? ""),
            
            nome_orientando = encoder.Encode(student?.Name ?? ""),
            curso_orientando = encoder.Encode(student?.CampiCourse?.Course.Name ?? ""),
            turma_orientando = encoder.Encode(student?.UserClass ?? ""),
            ano_orientando = encoder.Encode(formattedSemester),
            turno_orientando = encoder.Encode(student?.Shift ?? ""),
            email_orientando = encoder.Encode(student?.Email ?? ""),
            telefone_orientando = encoder.Encode(student?.Phone ?? ""),
            matricula_orientando = encoder.Encode(student?.Registration ?? ""),
            universidade_cidade_orientando = encoder.Encode(student?.CampiCourse?.Campi.City ?? ""),

            curso_supervisor = encoder.Encode(supervisorUser?.CampiCourse?.Course.Name ?? ""), 
            universidade_supervisor = encoder.Encode(supervisorUser?.CampiCourse?.Campi.City ?? ""),
            
            titulo_tcc = encoder.Encode(tccTitle),
            titulo_tcc_1 = encoder.Encode(tccTitle1),
            titulo_tcc_2 = encoder.Encode(tccTitle2),
            orientandos = encoder.Encode(string.Join(", ", students.Select(s => s.Name ?? ""))),

            dia_apresentacao = encoder.Encode(tccSchedule?.ScheduledDate.Day.ToString() ?? ""),
            mes_apresentacao = encoder.Encode(tccSchedule?.ScheduledDate.Month.ToString() ?? ""),
            ano_apresentacao = encoder.Encode(tccSchedule?.ScheduledDate.Year.ToString() ?? ""),
            hora_apresentacao = encoder.Encode(tccSchedule?.ScheduledDate.Hour.ToString() ?? ""),
            minuto_apresentacao = encoder.Encode(tccSchedule?.ScheduledDate.Minute.ToString() ?? ""),
            local_apresentacao = encoder.Encode(tccSchedule?.Location ?? ""),
            data_apresentacao = encoder.Encode(tccSchedule?.ScheduledDate.ToString("dd/MM/yyyy") ?? ""),

            cidade = encoder.Encode(student?.CampiCourse?.Campi.City ?? advisor?.CampiCourse?.Campi.City ?? ""),
            dia = encoder.Encode(nowDate.Day.ToString()),
            mes = encoder.Encode(mesesPtBr[nowDate.Month - 1]),
            ano = encoder.Encode(nowDate.Year.ToString()),

            oral_1 = encoder.Encode(oral1),
            oral_2 = encoder.Encode(oral2),
            oral_3 = encoder.Encode(oral3),
            textual_1 = encoder.Encode(textual1),
            textual_2 = encoder.Encode(textual2),
            textual_3 = encoder.Encode(textual3),
            total_oral = encoder.Encode(totalOral),
            total_textual = encoder.Encode(totalTextual),
            nota_final = encoder.Encode(notaFinal),

            // Lista estruturada para loops 
            students = students.Select(s => new {
                name = encoder.Encode(s.Name ?? ""),
                course = encoder.Encode(s.CampiCourse?.Course.Name ?? ""),
                user_class = encoder.Encode(s.UserClass ?? ""),
                semester_year = encoder.Encode(formattedSemester),
                shift = encoder.Encode(s.Shift ?? ""),
                email = encoder.Encode(s.Email ?? ""),
                phone = encoder.Encode(s.Phone ?? "")
            }).ToList()
        };
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