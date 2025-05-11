namespace gestaotcc.Domain.Errors;

public class ResultPattern<T>
{
    public bool Success { get; private set; }
    public string Message { get; private set; }
    public T Data { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public FormatDetails? ErrorDetails { get; private set; }

    private ResultPattern(bool success, string message, T data = default, FormatDetails? errorDetails = null)
    {
        if (success) IsSuccess = true;

        Success = success;
        Message = message;
        Data = data;
        ErrorDetails = errorDetails;
    }

    // Método estático para sucesso (sem dados de retorno)
    public static ResultPattern<T> SuccessResult()
    {
        var message = "Operação realizada com sucesso.";
        return new ResultPattern<T>(true, message);
    }

    // Método estático para sucesso (com dados de retorno)
    public static ResultPattern<T> SuccessResult(T data)
    {
        var message = "Operação realizada com sucesso.";
        return new ResultPattern<T>(true, message, data);
    }

    // Método estático para falha (usando ProblemDetails)
    public static ResultPattern<T> FailureResult(string detail, int statusCode, string title = "Ocorreu um erro")
    {
        var problemDetails = new FormatDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = $"urn:problem:{Guid.NewGuid()}"  // Gera um identificador único para o problema
        };

        return new ResultPattern<T>(false, detail, default, problemDetails);
    }
}