using System.Diagnostics;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace Cadence.WebApi.Middleware;

/// <summary>
/// Middleware that logs request and response details for failed requests (4xx/5xx).
/// This helps diagnose validation errors and authorization failures in Application Insights.
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    // Maximum body size to capture (prevent memory issues with large payloads)
    private const int MaxBodySize = 32 * 1024; // 32KB

    // Content types to capture (only JSON and form data)
    private static readonly HashSet<string> LoggableContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/x-www-form-urlencoded",
        "text/plain"
    };

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for certain paths (health checks, static files)
        if (ShouldSkipLogging(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Capture request body for potential logging
        string? requestBody = null;
        if (IsLoggableContentType(context.Request.ContentType))
        {
            requestBody = await CaptureRequestBodyAsync(context.Request);
        }

        // Capture response body
        var originalResponseBody = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Check if we should log this response
            var statusCode = context.Response.StatusCode;
            if (statusCode >= 400)
            {
                var responseBody = await CaptureResponseBodyAsync(responseBodyStream);
                LogFailedRequest(context, statusCode, requestBody, responseBody, stopwatch.ElapsedMilliseconds);
            }

            // Copy the response body back to the original stream
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
        }
    }

    private static bool ShouldSkipLogging(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;
        return pathValue.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/hubs/", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
               pathValue.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLoggableContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return LoggableContentTypes.Any(ct => contentType.StartsWith(ct, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string?> CaptureRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
        {
            request.EnableBuffering();
        }

        request.Body.Position = 0;

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: MaxBodySize,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        // Truncate if too large
        if (body.Length > MaxBodySize)
        {
            body = body[..MaxBodySize] + "... [TRUNCATED]";
        }

        // Sanitize sensitive fields
        body = SanitizeBody(body);

        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    private static async Task<string?> CaptureResponseBodyAsync(MemoryStream responseBodyStream)
    {
        responseBodyStream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        // Truncate if too large
        if (body.Length > MaxBodySize)
        {
            body = body[..MaxBodySize] + "... [TRUNCATED]";
        }

        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    private static string SanitizeBody(string body)
    {
        // Redact common sensitive fields in JSON
        // This is a simple regex-based approach; for production, consider a JSON parser
        var sensitivePatterns = new[]
        {
            ("\"password\"\\s*:\\s*\"[^\"]*\"", "\"password\":\"[REDACTED]\""),
            ("\"currentPassword\"\\s*:\\s*\"[^\"]*\"", "\"currentPassword\":\"[REDACTED]\""),
            ("\"newPassword\"\\s*:\\s*\"[^\"]*\"", "\"newPassword\":\"[REDACTED]\""),
            ("\"confirmPassword\"\\s*:\\s*\"[^\"]*\"", "\"confirmPassword\":\"[REDACTED]\""),
            ("\"token\"\\s*:\\s*\"[^\"]*\"", "\"token\":\"[REDACTED]\""),
            ("\"refreshToken\"\\s*:\\s*\"[^\"]*\"", "\"refreshToken\":\"[REDACTED]\""),
            ("\"accessToken\"\\s*:\\s*\"[^\"]*\"", "\"accessToken\":\"[REDACTED]\""),
            ("\"secret\"\\s*:\\s*\"[^\"]*\"", "\"secret\":\"[REDACTED]\""),
        };

        foreach (var (pattern, replacement) in sensitivePatterns)
        {
            body = System.Text.RegularExpressions.Regex.Replace(
                body, pattern, replacement, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return body;
    }

    private void LogFailedRequest(
        HttpContext context,
        int statusCode,
        string? requestBody,
        string? responseBody,
        long durationMs)
    {
        var request = context.Request;
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var orgId = context.User?.FindFirst("org_id")?.Value;

        // Determine severity based on status code
        var logLevel = statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 and < 500 => LogLevel.Warning,
            _ => LogLevel.Information
        };

        // Create structured log with all relevant details
        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestMethod"] = request.Method,
            ["RequestPath"] = request.Path.Value,
            ["QueryString"] = request.QueryString.Value,
            ["StatusCode"] = statusCode,
            ["DurationMs"] = durationMs,
            ["UserId"] = userId,
            ["OrganizationId"] = orgId,
            ["RequestBody"] = requestBody,
            ["ResponseBody"] = responseBody,
            ["UserAgent"] = request.Headers.UserAgent.ToString(),
            ["ClientIP"] = context.Connection.RemoteIpAddress?.ToString()
        }))
        {
            _logger.Log(
                logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms. " +
                "User: {UserId}, Org: {OrgId}. " +
                "Request: {RequestBody}. Response: {ResponseBody}",
                request.Method,
                request.Path.Value,
                statusCode,
                durationMs,
                userId ?? "anonymous",
                orgId ?? "none",
                requestBody ?? "(empty)",
                responseBody ?? "(empty)");
        }

        // Also track as Application Insights custom event for easier querying
        var telemetryClient = context.RequestServices.GetService<TelemetryClient>();
        if (telemetryClient != null)
        {
            var properties = new Dictionary<string, string?>
            {
                ["Method"] = request.Method,
                ["Path"] = request.Path.Value,
                ["StatusCode"] = statusCode.ToString(),
                ["UserId"] = userId,
                ["OrganizationId"] = orgId,
                ["RequestBody"] = requestBody,
                ["ResponseBody"] = responseBody,
                ["QueryString"] = request.QueryString.Value,
                ["UserAgent"] = request.Headers.UserAgent.ToString()
            };

            var metrics = new Dictionary<string, double>
            {
                ["DurationMs"] = durationMs
            };

            telemetryClient.TrackEvent($"FailedRequest_{statusCode}", properties, metrics);
        }
    }
}

/// <summary>
/// Extension methods for registering the middleware.
/// </summary>
public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
