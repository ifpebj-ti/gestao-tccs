using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gestaotcc.Application.UseCases;

public class StudentDocumentService : IStudentDocumentService
{
    private readonly IHtmlTemplateEngine _templateEngine;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLogger _auditLogger;

    public StudentDocumentService(
        IHtmlTemplateEngine templateEngine,
        IEmailSender emailSender,
        IAuditLogger auditLogger)
    {
        _templateEngine = templateEngine;
        _emailSender = emailSender;
        _auditLogger = auditLogger;
    }

    public async Task SendDocumentationBatchAsync(Guid studentId, List<string> documentTypes)
    {
        await _auditLogger.LogEventAsync("START_DOC_GENERATION", "System", studentId.ToString(), "INFO");

        foreach (var docType in documentTypes)
        {
            try
            {
                var studentData = await GetStudentSafeDataAsync(studentId);

                string htmlDocument = await _templateEngine.GenerateDocumentAsync(docType, studentData);

                await _emailSender.SendEmailAsync(studentData.AcademicEmail, $"Seu Documento: {docType}", htmlDocument);

                await _auditLogger.LogEventAsync("DOC_SENT_SUCCESS", "System", studentId.ToString(), "SUCCESS", $"Type: {docType}");
            }
            catch (Exception ex)
            {
                await _auditLogger.LogEventAsync("DOC_SENT_FAILED", "System", studentId.ToString(), "ERROR", $"Type: {docType}. Reason: {ex.Message}");
            }
        }
    }
    
    private Task<StudentDto> GetStudentSafeDataAsync(Guid studentId)
    {
        return Task.FromResult(new StudentDto { Id = studentId, AcademicEmail = "aluno@instituicao.edu.br" });
    }
}
