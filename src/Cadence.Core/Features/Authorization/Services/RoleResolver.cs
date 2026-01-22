using Cadence.Core.Data;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Authorization.Services;

/// <summary>
/// Resolves effective permissions for users by checking both system-level roles
/// (ApplicationUser.SystemRole) and exercise-specific roles (ExerciseParticipant.Role).
/// </summary>
public class RoleResolver : IRoleResolver
{
    private readonly AppDbContext _context;

    public RoleResolver(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ExerciseRole?> GetExerciseRoleAsync(string userId, Guid exerciseId)
    {
        var participant = await _context.ExerciseParticipants
            .Where(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted)
            .FirstOrDefaultAsync();

        return participant?.Role;
    }

    /// <inheritdoc />
    public async Task<bool> CanAccessExerciseAsync(string userId, Guid exerciseId)
    {
        // Check if user is a system admin
        var user = await _context.ApplicationUsers.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // System Admins can access all exercises
        if (user.SystemRole == SystemRole.Admin)
        {
            return true;
        }

        // Other users must be assigned as participants
        var isParticipant = await _context.ExerciseParticipants
            .AnyAsync(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted);

        return isParticipant;
    }

    /// <inheritdoc />
    public async Task<bool> HasExerciseRoleAsync(string userId, Guid exerciseId, ExerciseRole minimumRole)
    {
        // Check if user is a system admin
        var user = await _context.ApplicationUsers.FindAsync(userId);
        if (user == null)
        {
            return false;
        }

        // System Admins have Administrator-equivalent access to all exercises
        if (user.SystemRole == SystemRole.Admin)
        {
            return true;
        }

        // Check exercise-specific role
        var participant = await _context.ExerciseParticipants
            .Where(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted)
            .FirstOrDefaultAsync();

        if (participant == null)
        {
            return false;
        }

        // Compare role hierarchy
        return GetRoleHierarchyValue(participant.Role) >= GetRoleHierarchyValue(minimumRole);
    }

    /// <inheritdoc />
    public async Task<SystemRole?> GetSystemRoleAsync(string userId)
    {
        var user = await _context.ApplicationUsers.FindAsync(userId);
        return user?.SystemRole;
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
