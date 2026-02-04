using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service interface for managing approval permissions.
/// Part of S11: Configurable Approval Permissions.
/// </summary>
public interface IApprovalPermissionService
{
    /// <summary>
    /// Gets the approval permission settings for the current organization.
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Approval permissions DTO</returns>
    /// <exception cref="KeyNotFoundException">Organization not found</exception>
    Task<ApprovalPermissionsDto> GetApprovalPermissionsAsync(
        Guid organizationId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the approval permission settings for an organization.
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="request">Update request with new settings</param>
    /// <param name="userId">ID of user making the change (for audit)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated approval permissions</returns>
    /// <exception cref="KeyNotFoundException">Organization not found</exception>
    Task<ApprovalPermissionsDto> UpdateApprovalPermissionsAsync(
        Guid organizationId,
        UpdateApprovalPermissionsRequest request,
        string userId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can approve injects for an exercise based on their role.
    /// Does not check self-approval - use CanApproveInjectAsync for specific inject checks.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if user's role is authorized to approve</returns>
    Task<bool> CanApproveAsync(
        string userId,
        Guid exerciseId,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if a user can approve a specific inject, including self-approval checks.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="injectId">The inject ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Approval check result with permission details</returns>
    Task<InjectApprovalCheckDto> CanApproveInjectAsync(
        string userId,
        Guid injectId,
        CancellationToken ct = default);

    /// <summary>
    /// Maps an ExerciseRole to the corresponding ApprovalRoles flag.
    /// </summary>
    /// <param name="role">The exercise role</param>
    /// <returns>The corresponding ApprovalRoles flag value</returns>
    ApprovalRoles GetApprovalRoleFlag(ExerciseRole role);

    /// <summary>
    /// Gets the human-readable names of roles included in an ApprovalRoles flags value.
    /// </summary>
    /// <param name="roles">The approval roles flags</param>
    /// <returns>List of role names</returns>
    List<string> GetRoleNames(ApprovalRoles roles);
}
