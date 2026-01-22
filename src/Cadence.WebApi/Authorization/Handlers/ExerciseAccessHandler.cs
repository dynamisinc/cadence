using System.Security.Claims;
using Cadence.WebApi.Authorization.Requirements;
using Cadence.Core.Features.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Cadence.WebApi.Authorization.Handlers;

/// <summary>
/// Authorization handler for ExerciseAccessRequirement.
/// Checks if the user can access a specific exercise by verifying:
/// 1. User is a System Admin (can access all exercises), OR
/// 2. User is assigned as a participant in the exercise
/// </summary>
public class ExerciseAccessHandler : AuthorizationHandler<ExerciseAccessRequirement>
{
    private readonly IRoleResolver _roleResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ExerciseAccessHandler(IRoleResolver roleResolver, IHttpContextAccessor httpContextAccessor)
    {
        _roleResolver = roleResolver;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ExerciseAccessRequirement requirement)
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

        // Check if user can access the exercise
        var canAccess = await _roleResolver.CanAccessExerciseAsync(userId, exerciseId.Value);
        if (canAccess)
        {
            context.Succeed(requirement);
        }
    }

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
