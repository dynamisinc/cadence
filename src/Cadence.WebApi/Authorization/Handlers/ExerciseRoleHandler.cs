using System.Security.Claims;
using Cadence.WebApi.Authorization.Requirements;
using Cadence.Core.Features.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Cadence.WebApi.Authorization.Handlers;

/// <summary>
/// Authorization handler for ExerciseRoleRequirement.
/// Checks if the user has at least the minimum required role in an exercise.
/// System Admins automatically satisfy all role requirements.
/// </summary>
public class ExerciseRoleHandler : AuthorizationHandler<ExerciseRoleRequirement>
{
    private readonly IRoleResolver _roleResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExerciseRoleHandler(IRoleResolver roleResolver, IHttpContextAccessor httpContextAccessor)
    {
        _roleResolver = roleResolver;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ExerciseRoleRequirement requirement)
    {
        // Get user ID from claims
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return; // Not authenticated
        }

        // Extract exercise ID from route data
        var exerciseId = GetExerciseIdFromRoute();
        if (exerciseId == null)
        {
            return; // No exercise ID in route
        }

        // Check if user has the required role
        var hasRole = await _roleResolver.HasExerciseRoleAsync(userId, exerciseId.Value, requirement.MinimumRole);
        if (hasRole)
        {
            context.Succeed(requirement);
        }
    }

    /// <summary>
    /// Extracts exercise ID from route values.
    /// Note: Route constraints ({id:guid}) validate GUID format before this handler runs,
    /// so invalid GUIDs are caught earlier with 404. This null return handles edge cases
    /// where routes don't have the constraint or the parameter is missing.
    /// </summary>
    private Guid? GetExerciseIdFromRoute()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        // Try to get exerciseId from route values
        if (httpContext.Request.RouteValues.TryGetValue("exerciseId", out var routeValue))
        {
            if (Guid.TryParse(routeValue?.ToString(), out var exerciseId))
            {
                return exerciseId;
            }
        }

        // Try to get id from route values (for routes like /exercises/{id})
        if (httpContext.Request.RouteValues.TryGetValue("id", out var idValue))
        {
            if (Guid.TryParse(idValue?.ToString(), out var exerciseId))
            {
                return exerciseId;
            }
        }

        return null;
    }
}
