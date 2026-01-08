using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace DynamisReferenceApp.Api.Core.Middleware;

/// <summary>
/// Middleware that handles unhandled exceptions and returns appropriate HTTP responses.
/// </summary>
public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(FunctionContext context, Exception exception)
    {
        var correlationId = context.GetCorrelationId();

        _logger.LogError(
            exception,
            "Unhandled exception in function {FunctionName}. CorrelationId: {CorrelationId}. Error: {ErrorMessage}",
            context.FunctionDefinition.Name,
            correlationId,
            exception.Message);

        var httpContext = context.GetHttpContext();
        if (httpContext == null) return;

        var (statusCode, message) = GetStatusCodeAndMessage(exception);

        var errorResponse = new ErrorResponse
        {
            CorrelationId = correlationId,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, JsonOptions));
    }

    private static (HttpStatusCode StatusCode, string Message) GetStatusCodeAndMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "The requested resource was not found."),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "You are not authorized to access this resource."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

/// <summary>
/// Standard error response format.
/// </summary>
public class ErrorResponse
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
