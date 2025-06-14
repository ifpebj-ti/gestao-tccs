using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog.Context;

namespace gestaotcc.WebApi.Middlewares;

public class LogMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        var requestId = httpContext.TraceIdentifier;
        string? userId = httpContext.User.FindFirst("userId")?.Value ?? "anonymous";
        var environment = httpContext.RequestServices.GetService<IWebHostEnvironment>()!.EnvironmentName;

        var endpoint = httpContext.GetEndpoint();
        var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

        var actionName = actionDescriptor != null
            ? $"{actionDescriptor.ControllerTypeInfo.FullName}.{actionDescriptor.ActionName} ({actionDescriptor.ControllerTypeInfo.Assembly.GetName().Name})"
            : "unknown";

        using (LogContext.PushProperty("env", environment))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("ActionName", actionName))
        {
            await next(httpContext);
        }
    }
}