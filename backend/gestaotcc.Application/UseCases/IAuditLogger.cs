using System;
using System.Threading.Tasks;

namespace gestaotcc.Application.UseCases;

public interface IAuditLogger
{
    Task LogEventAsync(string action, string userId, string targetId, string status, string? details = null);
}
