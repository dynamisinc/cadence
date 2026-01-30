using Cadence.Core.Exceptions;
using Cadence.Core.Features.Organizations.Models.DTOs;
using Cadence.Core.Models.Entities;

namespace Cadence.Core.Features.Organizations.Services;

/// <summary>
/// Service for managing user-organization memberships.
/// Handles assignment, role changes, and validation of memberships.
/// </summary>
public interface IMembershipService
{
    /// <summary>
    /// Get all memberships for a user.
    /// </summary>
    /// <param name="userId">User ID to get memberships for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of memberships.</returns>
    Task<IEnumerable<MembershipDto>> GetUserMembershipsAsync(string userId, CancellationToken ct = default);

    /// <summary>
    /// Assign a user to an organization with a specific role.
    /// If this is the user's first organization, their status changes from Pending to Active.
    /// If user already has a membership in this org (inactive), it reactivates it.
    /// </summary>
    /// <param name="userId">User ID to assign.</param>
    /// <param name="request">Organization and role information.</param>
    /// <param name="assignedByUserId">User ID of the person making the assignment (for audit).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created or reactivated membership.</returns>
    /// <exception cref="ConflictException">If user already has active membership in organization.</exception>
    /// <exception cref="NotFoundException">If user or organization not found.</exception>
    Task<MembershipDto> AssignUserToOrganizationAsync(
        string userId,
        AssignUserRequest request,
        string assignedByUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Update a user's role in an organization membership.
    /// Validates that organization maintains at least one OrgAdmin.
    /// </summary>
    /// <param name="membershipId">Membership ID to update.</param>
    /// <param name="request">New role information.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated membership.</returns>
    /// <exception cref="BusinessRuleException">If changing role would leave org without admin.</exception>
    /// <exception cref="NotFoundException">If membership not found.</exception>
    Task<MembershipDto> UpdateMembershipRoleAsync(
        Guid membershipId,
        UpdateMembershipRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Remove a user's membership from an organization.
    /// If this is the user's last organization, their status changes from Active to Pending.
    /// </summary>
    /// <param name="membershipId">Membership ID to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response indicating if user status changed.</returns>
    /// <exception cref="BusinessRuleException">If removing last OrgAdmin from organization.</exception>
    /// <exception cref="NotFoundException">If membership not found.</exception>
    Task<RemoveMembershipResponse> RemoveMembershipAsync(Guid membershipId, CancellationToken ct = default);

    /// <summary>
    /// Check if a user has membership in a specific organization.
    /// </summary>
    /// <param name="userId">User ID to check.</param>
    /// <param name="organizationId">Organization ID to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if user has active membership, false otherwise.</returns>
    Task<bool> HasMembershipAsync(string userId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Get a user's role in a specific organization.
    /// </summary>
    /// <param name="userId">User ID to check.</param>
    /// <param name="organizationId">Organization ID to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User's role in org, or null if no active membership.</returns>
    Task<OrgRole?> GetUserRoleInOrganizationAsync(
        string userId,
        Guid organizationId,
        CancellationToken ct = default);

    /// <summary>
    /// Validate that a role change is allowed.
    /// Ensures organization maintains at least one OrgAdmin.
    /// </summary>
    /// <param name="membershipId">Membership being changed.</param>
    /// <param name="newRole">New role being assigned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="BusinessRuleException">If change would violate business rules.</exception>
    Task ValidateCanChangeRoleAsync(Guid membershipId, OrgRole newRole, CancellationToken ct = default);

    /// <summary>
    /// Get all members of an organization.
    /// </summary>
    /// <param name="organizationId">Organization ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of organization members.</returns>
    Task<IEnumerable<OrgMemberDto>> GetOrganizationMembersAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Add a user to an organization by email address.
    /// Creates membership directly if user exists, or creates pending user and membership.
    /// </summary>
    /// <param name="organizationId">Organization to add user to.</param>
    /// <param name="request">Email and role for the new member.</param>
    /// <param name="addedByUserId">ID of the user making the addition.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created membership details.</returns>
    /// <exception cref="ConflictException">If user already has active membership in organization.</exception>
    Task<OrgMemberDto> AddMemberByEmailAsync(
        Guid organizationId,
        AddMemberRequest request,
        string addedByUserId,
        CancellationToken ct = default);
}
