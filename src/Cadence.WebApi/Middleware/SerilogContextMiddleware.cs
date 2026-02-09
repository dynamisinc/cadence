using System.Security.Claims;
using Serilog.Context;

namespace Cadence.WebApi.Middleware;

/// <summary>
/// Middleware that enriches Serilog LogContext with Cadence-specific properties.
/// Adds UserId, OrganizationId, and ExerciseId (when available) to every log
/// entry within a request scope, enabling powerful filtering in Application Insights.
/// </summary>
public class SerilogContextMiddleware
{
    private readonly RequestDelegate _next;

    public SerilogContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User?.FindFirst("sub")?.Value
                     ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var orgId = context.User?.FindFirst("org_id")?.Value;
        var exerciseId = context.Request.RouteValues["exerciseId"]?.ToString();

        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        using (LogContext.PushProperty("OrganizationId", orgId ?? "none"))
        using (LogContext.PushProperty("ExerciseId", exerciseId ?? "none"))
        {
            await _next(context);
        }
    }
}
