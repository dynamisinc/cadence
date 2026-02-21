using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Authorization.Services;

/// <summary>
/// Resolves effective permissions for a user in a given context.
/// Implements the authorization logic separating System Roles (application-level)
/// from Exercise Roles (per-exercise HSEEP assignments).
/// </summary>
public interface IRoleResolver
{
    /// <summary>
    /// Get user's HSEEP exercise role for a specific exercise.
    /// Returns null if user is not a participant.
    /// Admins can access all exercises but may not have an explicit exercise role.
    /// </summary>
    /// <param name="userId">ApplicationUser.Id (string)</param>
    /// <param name="exerciseId">Exercise ID</param>
    /// <returns>Exercise role or null if not assigned</returns>
    Task<ExerciseRole?> GetExerciseRoleAsync(string userId, Guid exerciseId);

    /// <summary>
    /// Check if user can access an exercise.
    /// Admins can access all exercises regardless of assignment.
    /// OrgAdmin/OrgManager can access all exercises in their current organization.
    /// Other users must be explicitly assigned as participants.
    /// </summary>
    /// <param name="userId">ApplicationUser.Id (string)</param>
    /// <param name="exerciseId">Exercise ID</param>
    /// <returns>True if user can access the exercise</returns>
    Task<bool> CanAccessExerciseAsync(string userId, Guid exerciseId);

    /// <summary>
    /// Check if user has at least the specified role in an exercise.
    /// Role hierarchy (lowest to highest):
    /// Observer &lt; Evaluator &lt; Controller &lt; ExerciseDirector &lt; Administrator
    ///
    /// Escalation rules:
    /// - System Admins always have full access equivalent to Administrator.
    /// - OrgAdmin/OrgManager get ExerciseDirector-equivalent access in their org.
    /// - The effective role is the highest of: exercise role and mapped org role.
    /// </summary>
    /// <param name="userId">ApplicationUser.Id (string)</param>
    /// <param name="exerciseId">Exercise ID</param>
    /// <param name="minimumRole">Minimum required role</param>
    /// <returns>True if user has at least the minimum role</returns>
    Task<bool> HasExerciseRoleAsync(string userId, Guid exerciseId, ExerciseRole minimumRole);

    /// <summary>
    /// Get user's system role (Admin, Manager, User).
    /// </summary>
    /// <param name="userId">ApplicationUser.Id (string)</param>
    /// <returns>System role or null if user not found</returns>
    Task<SystemRole?> GetSystemRoleAsync(string userId);
}
