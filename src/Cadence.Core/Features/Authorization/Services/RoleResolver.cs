using Cadence.Core.Data;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Authorization.Services;

/// <summary>
/// Resolves effective permissions for users by checking system-level roles
/// (ApplicationUser.SystemRole), organization roles (OrgRole), and
/// exercise-specific roles (ExerciseParticipant.Role).
///
/// Escalation rules:
/// - System Admins always have Administrator-equivalent access to all exercises.
/// - OrgAdmin/OrgManager get ExerciseDirector-equivalent access to exercises in their org.
/// - The effective role is the highest of: exercise role, mapped system role, mapped org role.
/// </summary>
public class RoleResolver : IRoleResolver
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;

    public RoleResolver(AppDbContext context, ICurrentOrganizationContext orgContext)
    {
        _context = context;
        _orgContext = orgContext;
    }

    /// <inheritdoc />
    public async Task<ExerciseRole?> GetExerciseRoleAsync(string userId, Guid exerciseId)
    {
        var participant = await _context.ExerciseParticipants
            .AsNoTracking()
            .Where(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted)
            .FirstOrDefaultAsync();

        return participant?.Role;
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessExerciseAsync(string userId, Guid exerciseId)
    {
        // Check if user is a system admin
        var user = await _context.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return false;
        }

        // System Admins can access all exercises
        if (user.SystemRole == SystemRole.Admin)
        {
            return true;
        }

        // OrgAdmin/OrgManager can access all exercises in their organization
        if (_orgContext.CurrentOrgRole is OrgRole.OrgAdmin or OrgRole.OrgManager)
        {
            var exerciseBelongsToOrg = await _context.Exercises
                .AsNoTracking()
                .AnyAsync(e => e.Id == exerciseId && e.OrganizationId == _orgContext.CurrentOrganizationId && !e.IsDeleted);
            if (exerciseBelongsToOrg)
            {
                return true;
            }
        }

        // Other users must be assigned as participants
        var isParticipant = await _context.ExerciseParticipants
            .AsNoTracking()
            .AnyAsync(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted);

        return isParticipant;
    }

    /// <inheritdoc />
    public async Task<bool> HasExerciseRoleAsync(string userId, Guid exerciseId, ExerciseRole minimumRole)
    {
        // Check if user is a system admin
        var user = await _context.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return false;
        }

        // System Admins have Administrator-equivalent access to all exercises
        if (user.SystemRole == SystemRole.Admin)
        {
            return true;
        }

        // Compute org-level escalation (OrgAdmin/OrgManager -> ExerciseDirector)
        var orgEquivalentRole = MapOrgRoleToExerciseRole(_orgContext.CurrentOrgRole);

        // Check exercise-specific role
        var participant = await _context.ExerciseParticipants
            .AsNoTracking()
            .Where(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted)
            .FirstOrDefaultAsync();

        // Take the highest role from exercise role and org-mapped role
        var effectiveHierarchy = Math.Max(
            participant != null ? GetRoleHierarchyValue(participant.Role) : 0,
            orgEquivalentRole.HasValue ? GetRoleHierarchyValue(orgEquivalentRole.Value) : 0
        );

        return effectiveHierarchy >= GetRoleHierarchyValue(minimumRole);
    }

    /// <inheritdoc />
    public async Task<SystemRole?> GetSystemRoleAsync(string userId)
    {
        var user = await _context.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        return user?.SystemRole;
    }

    /// <summary>
    /// Map organization roles to exercise role equivalents for escalation.
    /// OrgAdmin/OrgManager get ExerciseDirector-equivalent access.
    /// </summary>
    private static ExerciseRole? MapOrgRoleToExerciseRole(OrgRole? orgRole)
    {
        return orgRole switch
        {
            OrgRole.OrgAdmin => ExerciseRole.ExerciseDirector,
            OrgRole.OrgManager => ExerciseRole.ExerciseDirector,
            _ => null
        };
    }

    /// <summary>
    /// Get numeric value representing role hierarchy for comparison.
    /// Higher values = more permissions.
    /// </summary>
    private static int GetRoleHierarchyValue(ExerciseRole role)
    {
        return role switch
        {
            ExerciseRole.Observer => 1,
            ExerciseRole.Evaluator => 2,
            ExerciseRole.Controller => 3,
            ExerciseRole.ExerciseDirector => 4,
            ExerciseRole.Administrator => 5,
            _ => 0
        };
    }
}
