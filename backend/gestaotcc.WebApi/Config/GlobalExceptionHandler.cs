using FluentValidation;
using gestaotcc.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace gestaotcc.WebApi.Config;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception, "Uma exception ocorreu: {Message}", exception.Message);

        var problemDetails = new ProblemDetails();

        if(exception is DomainException)
        {
            problemDetails.Title = "Erro de domínio";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = exception.Message;
            problemDetails.Instance = httpContext.Request.Path;
        }else if(exception is DbUpdateException)
        {
            problemDetails.Title = "Erro de banco de dados";
            problemDetails.Status = StatusCodes.Status500InternalServerError;
            problemDetails.Detail = "Erro ao tentar salvar no banco de dados";
            problemDetails.Instance = httpContext.Request.Path;
        }
        else if(exception is ValidationException)
        {
            problemDetails.Title = "Erro de validação";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Detail = exception.Message;
            problemDetails.Instance = httpContext.Request.Path;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}