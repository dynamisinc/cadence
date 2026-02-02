using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Cadence.WebApi.Authorization.Handlers;

/// <summary>
/// Custom authorization middleware result handler that logs authorization failures.
/// This helps diagnose 403 errors in Application Insights.
/// </summary>
public class AuthorizationLoggingHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly ILogger<AuthorizationLoggingHandler> _logger;

    public AuthorizationLoggingHandler(ILogger<AuthorizationLoggingHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // Log authorization failures
        if (authorizeResult.Forbidden || authorizeResult.Challenged)
        {
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var systemRole = context.User?.FindFirst("SystemRole")?.Value;
            var orgId = context.User?.FindFirst("org_id")?.Value;
            var orgRole = context.User?.FindFirst("org_role")?.Value;
            var path = context.Request.Path.Value;
            var method = context.Request.Method;

            // Get the failed requirements for detailed logging
            var failedRequirements = authorizeResult.AuthorizationFailure?.FailedRequirements
                .Select(r => r.GetType().Name)
                .ToList() ?? new List<string>();

            var failureReasons = authorizeResult.AuthorizationFailure?.FailureReasons
                .Select(r => r.Message)
                .ToList() ?? new List<string>();

            if (authorizeResult.Challenged)
            {
                _logger.LogWarning(
                    "Authorization challenged (401) for {Method} {Path}. " +
                    "User: {UserId}, IsAuthenticated: {IsAuthenticated}",
                    method,
                    path,
                    userId ?? "anonymous",
                    context.User?.Identity?.IsAuthenticated ?? false);
            }
            else if (authorizeResult.Forbidden)
            {
                _logger.LogWarning(
                    "Authorization forbidden (403) for {Method} {Path}. " +
                    "User: {UserId}, SystemRole: {SystemRole}, OrgId: {OrgId}, OrgRole: {OrgRole}. " +
                    "Policy requirements: {PolicyRequirements}. " +
                    "Failed requirements: {FailedRequirements}. " +
                    "Failure reasons: {FailureReasons}",
                    method,
                    path,
                    userId ?? "anonymous",
                    systemRole ?? "none",
                    orgId ?? "none",
                    orgRole ?? "none",
                    string.Join(", ", policy.Requirements.Select(r => r.GetType().Name)),
                    string.Join(", ", failedRequirements),
                    string.Join(", ", failureReasons));
            }
        }

        // Call the default handler to continue the pipeline
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
