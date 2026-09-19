using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gestaotcc.Application.UseCases;

public interface IStudentDocumentService
{
    Task SendDocumentationBatchAsync(Guid studentId, List<string> documentTypes);
}
