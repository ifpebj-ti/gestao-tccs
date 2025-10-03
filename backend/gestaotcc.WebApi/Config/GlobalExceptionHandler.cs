using gestaotcc.Domain.Exceptions;
using gestaotcc.WebApi.Validators;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace gestaotcc.WebApi.Config;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // passa para o próximo middleware
        }
        catch (Exception ex)
        {
            var statusCode = 500;
            var message = ex.Message;

            switch (ex)
            {
                case ValidatorException ve: statusCode = 400; break;
                case DomainException de: statusCode = 400; break;
                case DbUpdateException db: statusCode = 500; break;
            }

            // adiciona propriedades ao log
            using (LogContext.PushProperty("StatusCode", statusCode))
            using (LogContext.PushProperty("Method", context.Request.Method))
            using (LogContext.PushProperty("Path", context.Request.Path))
            using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
            {
                _logger.LogError(ex, "Uma exception ocorreu: {Message}", message);
            }

            context.Response.StatusCode = statusCode;
            var problemDetails = new ProblemDetails
            {
                Title = ex.GetType().Name,
                Detail = message,
                Status = statusCode,
                Instance = context.Request.Path
            };
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case ValidatorException ve:
                problemDetails.Title = "Erro de validação";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = ve.Message;
                break;
            case DomainException de:
                problemDetails.Title = "Erro de domínio";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = de.Message;
                break;
            case DbUpdateException db:
                problemDetails.Title = "Erro de banco de dados";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = "Erro ao tentar salvar no banco de dados";
                break;
            default:
                problemDetails.Title = "Erro interno";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = exception.Message;
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = problemDetails.Status.Value;
        return context.Response.WriteAsJsonAsync(problemDetails);
    }
}