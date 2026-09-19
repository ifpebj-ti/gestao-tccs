using System.Threading.Tasks;
using gestaotcc.Application.UseCases;

namespace gestaotcc.Infra.Gateways;

public class FakeAuditLogger : IAuditLogger
{
    public Task LogEventAsync(string action, string userId, string targetId, string status, string? details = null)
    {
        return Task.CompletedTask;
    }
}
