using gestaotcc.Domain.Exceptions;
using gestaotcc.WebApi.Validators;
using Microsoft.AspNetCore.Mvc;

namespace gestaotcc.WebApi.Config;

public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            var status = error switch
            {
                ValidatorException or FluentValidation.ValidationException or DomainException => 400,
                BadHttpRequestException requestError => requestError.StatusCode,
                _ => 500
            };
            // Do not serialize raw exceptions or values from queries, passwords, or infrastructure messages.
            logger.LogWarning("Falha de requisição {TraceId}, status {Status}, tipo {ErrorType}.",
                context.TraceIdentifier, status, error.GetType().Name);
            context.Response.StatusCode = status;
            context.Response.Headers.CacheControl = "no-store";
            var problem = new ProblemDetails
            {
                Title = status >= 500 ? "Erro interno" : "Requisição inválida",
                Detail = status >= 500 ? "Não foi possível concluir a operação. Informe o identificador de atendimento."
                    : error is ValidatorException or FluentValidation.ValidationException or DomainException ? error.Message : "Verifique os dados enviados.",
                Status = status, Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(problem, options: (System.Text.Json.JsonSerializerOptions?)null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
