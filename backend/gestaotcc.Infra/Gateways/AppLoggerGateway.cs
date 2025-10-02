using gestaotcc.Application.Gateways;
using Microsoft.Extensions.Logging;

namespace gestaotcc.Infra.Gateways;

public class AppLoggerGateway<T>(ILogger<T> logger) : IAppLoggerGateway<T>
{
    public void LogInformation(string message, params object[] args)
        => logger.LogInformation(message, args);

    public void LogWarning(string message, params object[] args)
        => logger.LogWarning(message, args);

    public void LogError(string message, params object[] args)
        => logger.LogError(message, args);

    public void LogError(Exception exception, string message, params object[] args)
        => logger.LogError(exception, message, args);

    public void LogDebug(string message, params object[] args)
        => logger.LogDebug(message, args);
}