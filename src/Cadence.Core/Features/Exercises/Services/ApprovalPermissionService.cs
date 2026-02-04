using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for managing approval permissions.
/// Part of S11: Configurable Approval Permissions.
/// </summary>
public class ApprovalPermissionService : IApprovalPermissionService
{
    private readonly AppDbContext _context;

    public ApprovalPermissionService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ApprovalPermissionsDto> GetApprovalPermissionsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var org = await _context.Organizations.FindAsync(new object[] { organizationId }, ct)
            ?? throw new KeyNotFoundException($"Organization {organizationId} not found.");

        return new ApprovalPermissionsDto(
            org.ApprovalAuthorizedRoles,
            org.SelfApprovalPolicy,
            GetRoleNames(org.ApprovalAuthorizedRoles)
        );
    }

    /// <inheritdoc />
    public async Task<ApprovalPermissionsDto> UpdateApprovalPermissionsAsync(
        Guid organizationId,
        UpdateApprovalPermissionsRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var org = await _context.Organizations.FindAsync(new object[] { organizationId }, ct)
            ?? throw new KeyNotFoundException($"Organization {organizationId} not found.");

        // Administrator must always be included
        var authorizedRoles = request.AuthorizedRoles | ApprovalRoles.Administrator;

        org.ApprovalAuthorizedRoles = authorizedRoles;
        org.SelfApprovalPolicy = request.SelfApprovalPolicy;
        org.ModifiedBy = userId;

        await _context.SaveChangesAsync(ct);

        return new ApprovalPermissionsDto(
            org.ApprovalAuthorizedRoles,
            org.SelfApprovalPolicy,
            GetRoleNames(org.ApprovalAuthorizedRoles)
        );
    }

    /// <inheritdoc />
    public async Task<bool> CanApproveAsync(
        string userId,
        Guid exerciseId,
        CancellationToken ct = default)
    {
        // Get exercise with organization
        var exercise = await _context.Exercises
            .Include(e => e.Organization)
            .FirstOrDefaultAsync(e => e.Id == exerciseId, ct)
            ?? throw new KeyNotFoundException($"Exercise {exerciseId} not found.");

        // Get user's role in this exercise
        var exerciseParticipant = await _context.ExerciseParticipants
            .FirstOrDefaultAsync(ep => ep.ExerciseId == exerciseId && ep.UserId == userId, ct);

        if (exerciseParticipant == null)
        {
            // Check if user is system admin
            var user = await _context.ApplicationUsers.FindAsync(new object[] { userId }, ct);
            if (user?.SystemRole == SystemRole.Admin)
            {
                return true; // System admins can always approve
            }
            return false;
        }

        // Check if user's exercise role is authorized
        var authorizedRoles = exercise.Organization.ApprovalAuthorizedRoles;
        var userRoleFlag = GetApprovalRoleFlag(exerciseParticipant.Role);

        return authorizedRoles.HasFlag(userRoleFlag);
    }

    /// <inheritdoc />
    public async Task<InjectApprovalCheckDto> CanApproveInjectAsync(
        string userId,
        Guid injectId,
        CancellationToken ct = default)
    {
        // Get inject with exercise and organization
        var inject = await _context.Injects
            .Include(i => i.Msel)
                .ThenInclude(m => m.Exercise)
                    .ThenInclude(e => e.Organization)
            .FirstOrDefaultAsync(i => i.Id == injectId, ct)
            ?? throw new KeyNotFoundException($"Inject {injectId} not found.");

        var exercise = inject.Msel.Exercise;
        var organization = exercise.Organization;

        // Check base permission (role authorization)
        var canApproveRole = await CanApproveAsync(userId, exercise.Id, ct);

        if (!canApproveRole)
        {
            return new InjectApprovalCheckDto(
                CanApprove: false,
                PermissionResult: ApprovalPermissionResult.NotAuthorized,
                IsSelfApproval: false,
                RequiresConfirmation: false,
                Message: "Your role is not authorized to approve injects."
            );
        }

        // Check self-approval
        var isSelfApproval = inject.SubmittedByUserId == userId;

        if (isSelfApproval)
        {
            return organization.SelfApprovalPolicy switch
            {
                SelfApprovalPolicy.NeverAllowed => new InjectApprovalCheckDto(
                    CanApprove: false,
                    PermissionResult: ApprovalPermissionResult.SelfApprovalDenied,
                    IsSelfApproval: true,
                    RequiresConfirmation: false,
                    Message: "Self-approval is not permitted by your organization."
                ),

                SelfApprovalPolicy.AllowedWithWarning => new InjectApprovalCheckDto(
                    CanApprove: true,
                    PermissionResult: ApprovalPermissionResult.SelfApprovalWithWarning,
                    IsSelfApproval: true,
                    RequiresConfirmation: true,
                    Message: "You can approve your own submission, but must confirm."
                ),

                SelfApprovalPolicy.AlwaysAllowed => new InjectApprovalCheckDto(
                    CanApprove: true,
                    PermissionResult: ApprovalPermissionResult.Allowed,
                    IsSelfApproval: true,
                    RequiresConfirmation: false,
                    Message: null
                ),

                _ => new InjectApprovalCheckDto(
                    CanApprove: false,
                    PermissionResult: ApprovalPermissionResult.SelfApprovalDenied,
                    IsSelfApproval: true,
                    RequiresConfirmation: false,
                    Message: "Self-approval is not permitted."
                )
            };
        }

        // Not self-approval and role is authorized
        return new InjectApprovalCheckDto(
            CanApprove: true,
            PermissionResult: ApprovalPermissionResult.Allowed,
            IsSelfApproval: false,
            RequiresConfirmation: false,
            Message: null
        );
    }

    /// <inheritdoc />
    public ApprovalRoles GetApprovalRoleFlag(ExerciseRole role)
    {
        return role switch
        {
            ExerciseRole.Administrator => ApprovalRoles.Administrator,
            ExerciseRole.ExerciseDirector => ApprovalRoles.ExerciseDirector,
            ExerciseRole.Controller => ApprovalRoles.Controller,
            ExerciseRole.Evaluator => ApprovalRoles.Evaluator,
            _ => ApprovalRoles.None
        };
    }

    /// <inheritdoc />
    public List<string> GetRoleNames(ApprovalRoles roles)
    {
        var names = new List<string>();

        if (roles.HasFlag(ApprovalRoles.Administrator))
            names.Add("Administrator");
        if (roles.HasFlag(ApprovalRoles.ExerciseDirector))
            names.Add("Exercise Director");
        if (roles.HasFlag(ApprovalRoles.Controller))
            names.Add("Controller");
        if (roles.HasFlag(ApprovalRoles.Evaluator))
            names.Add("Evaluator");

        return names;
    }
}
