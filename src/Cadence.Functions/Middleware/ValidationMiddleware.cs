using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Cadence.Functions.Middleware;

/// <summary>
/// Middleware that catches FluentValidation exceptions and returns 400 Bad Request.
/// </summary>
public class ValidationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ValidationMiddleware> _logger;

    public ValidationMiddleware(ILogger<ValidationMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            await WriteErrorResponse(context, ex);
        }
    }

    private static async Task WriteErrorResponse(FunctionContext context, ValidationException exception)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext == null) return;

        var errors = exception.Errors.Select(e => new
        {
            Field = e.PropertyName,
            Message = e.ErrorMessage
        });

        var response = new
        {
            Message = "Validation Failed",
            Errors = errors
        };

        httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
