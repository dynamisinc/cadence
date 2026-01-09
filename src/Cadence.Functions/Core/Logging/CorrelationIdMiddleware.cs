using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace Cadence.Api.Core.Logging;

/// <summary>
/// Middleware that ensures each request has a correlation ID for tracing.
/// </summary>
public class CorrelationIdMiddleware : IFunctionsWorkerMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Store correlation ID in context for logging
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers if HTTP trigger
        var httpContext = context.GetHttpContext();
        if (httpContext != null)
        {
            httpContext.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
        }

        await next(context);
    }

    private static string GetOrCreateCorrelationId(FunctionContext context)
    {
        // Try to get from HTTP request headers
        var httpContext = context.GetHttpContext();
        if (httpContext?.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue) == true
            && !string.IsNullOrEmpty(headerValue))
        {
            return headerValue!;
        }

        // Try to get from invocation ID
        if (!string.IsNullOrEmpty(context.InvocationId))
        {
            return context.InvocationId;
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString("N")[..12];
    }
}

/// <summary>
/// Extension methods for accessing correlation ID from function context.
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Gets the correlation ID from the function context.
    /// </summary>
    public static string GetCorrelationId(this FunctionContext context)
    {
        if (context.Items.TryGetValue("CorrelationId", out var correlationId)
            && correlationId is string id)
        {
            return id;
        }

        return context.InvocationId ?? Guid.NewGuid().ToString("N")[..12];
    }
}
