using System.Security.Claims;

namespace Cadence.WebApi.Extensions;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> to provide consistent
/// claim extraction across all controllers.
/// </summary>
/// <remarks>
/// Centralizes user identity extraction to eliminate copy-paste variations across
/// 12+ controllers. All controllers should call <see cref="GetUserId"/> instead of
/// maintaining local GetCurrentUserId() helpers.
/// </remarks>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the authenticated user's ID from the NameIdentifier claim.
    /// </summary>
    /// <param name="principal">The claims principal (User property of ControllerBase).</param>
    /// <returns>The user ID string.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the NameIdentifier claim is missing or empty.
    /// </exception>
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }
        return userIdClaim;
    }

    /// <summary>
    /// Attempts to get the authenticated user's ID from the NameIdentifier claim.
    /// </summary>
    /// <param name="principal">The claims principal (User property of ControllerBase).</param>
    /// <returns>The user ID string, or <c>null</c> if the claim is missing.</returns>
    public static string? TryGetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Gets the current organization ID from the org_id claim.
    /// </summary>
    /// <param name="principal">The claims principal (User property of ControllerBase).</param>
    /// <returns>The organization ID, or <c>null</c> if no organization context is present.</returns>
    public static Guid? GetOrganizationId(this ClaimsPrincipal principal)
    {
        var orgIdClaim = principal.FindFirstValue("org_id");
        if (string.IsNullOrEmpty(orgIdClaim))
            return null;

        return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
    }
}
