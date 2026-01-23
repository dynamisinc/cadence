using Cadence.Core.Data;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Exercises.Services;

/// <summary>
/// Service for managing exercise participants and their exercise-specific roles.
/// Uses ApplicationUser.SystemRole for system-level permissions and ExerciseParticipant.Role for exercise-specific assignments.
/// </summary>
public class ExerciseParticipantService : IExerciseParticipantService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExerciseParticipantService> _logger;

    public ExerciseParticipantService(
        AppDbContext context,
        ILogger<ExerciseParticipantService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ExerciseParticipantDto>> GetParticipantsAsync(
        Guid exerciseId,
        CancellationToken ct = default)
    {
        var participants = await _context.ExerciseParticipants
            .Include(p => p.User)
            .Where(p => p.ExerciseId == exerciseId && !p.IsDeleted)
            .ToListAsync(ct);

        var result = new List<ExerciseParticipantDto>();

        foreach (var participant in participants)
        {
            // Handle soft-deleted users gracefully
            if (participant.User == null)
            {
                _logger.LogWarning(
                    "ExerciseParticipant {ParticipantId} references soft-deleted user {UserId}",
                    participant.Id, participant.UserId);
                continue;
            }

            result.Add(MapToDto(participant, participant.User));
        }

        return result.OrderBy(p => p.DisplayName).ToList();
    }

    /// <inheritdoc />
    public async Task<ExerciseParticipantDto?> GetParticipantAsync(
        Guid exerciseId,
        string userId,
        CancellationToken ct = default)
    {
        var participant = await _context.ExerciseParticipants
            .Include(p => p.User)
            .Where(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (participant == null || participant.User == null)
        {
            return null;
        }

        return MapToDto(participant, participant.User);
    }

    /// <inheritdoc />
    public async Task<string> GetEffectiveRoleAsync(
        Guid exerciseId,
        string userId,
        CancellationToken ct = default)
    {
        // Check for exercise-specific role
        var participant = await _context.ExerciseParticipants
            .Where(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (participant != null)
        {
            return participant.Role.ToString();
        }

        // TODO: Fall back to global role once User.GlobalRole is available
        // For now, default to Observer
        return nameof(ExerciseRole.Observer);
    }

    /// <inheritdoc />
    public async Task<ExerciseParticipantDto> AddParticipantAsync(
        Guid exerciseId,
        AddParticipantRequest request,
        CancellationToken ct = default)
    {
        // Verify user exists
        var user = await _context.ApplicationUsers.FindAsync(request.UserId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {request.UserId} not found");
        }

        // Parse the requested role
        var role = ParseRole(request.Role) ?? ExerciseRole.Observer;

        // Validate Exercise Director assignment
        if (role == ExerciseRole.ExerciseDirector)
        {
            if (user.SystemRole != SystemRole.Admin && user.SystemRole != SystemRole.Manager)
            {
                throw new InvalidOperationException("Only Admins and Managers can be assigned as Exercise Director");
            }
        }

        // Check if already a participant (including soft-deleted)
        var existing = await _context.ExerciseParticipants
            .Where(p => p.ExerciseId == exerciseId && p.UserId == request.UserId)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ct);

        if (existing != null)
        {
            if (!existing.IsDeleted)
            {
                throw new InvalidOperationException("User is already a participant in this exercise");
            }

            // Reactivate soft-deleted participant
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.Role = role;

            _logger.LogInformation(
                "Reactivated participant {UserId} for exercise {ExerciseId} with role {Role}",
                request.UserId, exerciseId, existing.Role);
        }
        else
        {
            // Create new participant
            var participant = new ExerciseParticipant
            {
                ExerciseId = exerciseId,
                UserId = request.UserId,
                Role = role,
                AssignedAt = DateTime.UtcNow
            };

            _context.ExerciseParticipants.Add(participant);

            _logger.LogInformation(
                "Added participant {UserId} to exercise {ExerciseId} with role {Role}",
                request.UserId, exerciseId, participant.Role);
        }

        await _context.SaveChangesAsync(ct);

        var result = await GetParticipantAsync(exerciseId, request.UserId, ct);
        return result ?? throw new Exception("Failed to add participant");
    }

    /// <inheritdoc />
    public async Task<ExerciseParticipantDto> UpdateParticipantRoleAsync(
        Guid exerciseId,
        string userId,
        UpdateParticipantRoleRequest request,
        CancellationToken ct = default)
    {
        var participant = await _context.ExerciseParticipants
            .Include(p => p.User)
            .Where(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (participant == null)
        {
            throw new KeyNotFoundException($"Participant not found for user {userId} in exercise {exerciseId}");
        }

        if (participant.User == null)
        {
            throw new KeyNotFoundException($"User {userId} has been deleted");
        }

        // Parse the new role, default to Observer if null
        var newRole = ParseRole(request.Role) ?? ExerciseRole.Observer;

        // Validate Exercise Director assignment
        if (newRole == ExerciseRole.ExerciseDirector)
        {
            if (participant.User.SystemRole != SystemRole.Admin && participant.User.SystemRole != SystemRole.Manager)
            {
                throw new InvalidOperationException("Only Admins and Managers can be assigned as Exercise Director");
            }
        }

        participant.Role = newRole;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated participant {UserId} role in exercise {ExerciseId} to {Role}",
            userId, exerciseId, newRole);

        return MapToDto(participant, participant.User);
    }

    /// <inheritdoc />
    public async Task RemoveParticipantAsync(
        Guid exerciseId,
        string userId,
        CancellationToken ct = default)
    {
        var participant = await _context.ExerciseParticipants
            .Where(p => p.ExerciseId == exerciseId && p.UserId == userId && !p.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (participant == null)
        {
            throw new KeyNotFoundException($"Participant not found for user {userId} in exercise {exerciseId}");
        }

        // Soft delete
        participant.IsDeleted = true;
        participant.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Removed participant {UserId} from exercise {ExerciseId}",
            userId, exerciseId);
    }

    /// <inheritdoc />
    public async Task BulkUpdateParticipantsAsync(
        Guid exerciseId,
        BulkUpdateParticipantsRequest request,
        CancellationToken ct = default)
    {
        foreach (var participantRequest in request.Participants)
        {
            try
            {
                // Check if participant already exists
                var existing = await GetParticipantAsync(exerciseId, participantRequest.UserId, ct);

                if (existing != null)
                {
                    // Update existing participant
                    await UpdateParticipantRoleAsync(
                        exerciseId,
                        participantRequest.UserId,
                        new UpdateParticipantRoleRequest { Role = participantRequest.Role },
                        ct);
                }
                else
                {
                    // Add new participant
                    await AddParticipantAsync(exerciseId, participantRequest, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process participant {UserId} in bulk update for exercise {ExerciseId}",
                    participantRequest.UserId, exerciseId);
                throw;
            }
        }

        _logger.LogInformation(
            "Bulk updated {Count} participants for exercise {ExerciseId}",
            request.Participants.Count, exerciseId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExerciseAssignmentDto>> GetUserExerciseAssignmentsAsync(
        string userId,
        CancellationToken ct = default)
    {
        var assignments = await _context.ExerciseParticipants
            .Include(p => p.Exercise)
            .Where(p => p.UserId == userId && !p.IsDeleted && !p.Exercise.IsDeleted)
            .OrderByDescending(p => p.AssignedAt)
            .Select(p => new ExerciseAssignmentDto(
                p.ExerciseId,
                p.Exercise.Name,
                p.Role.ToString(),
                p.AssignedAt
            ))
            .ToListAsync(ct);

        _logger.LogInformation(
            "Retrieved {Count} exercise assignments for user {UserId}",
            assignments.Count, userId);

        return assignments;
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    /// <summary>
    /// Map ExerciseParticipant entity to DTO.
    /// </summary>
    private static ExerciseParticipantDto MapToDto(ExerciseParticipant participant, ApplicationUser user)
    {
        var roleString = participant.Role.ToString();

        return new ExerciseParticipantDto
        {
            UserId = participant.UserId,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            SystemRole = user.SystemRole.ToString(),
            ExerciseRole = roleString
        };
    }

    /// <summary>
    /// Parse role string to ExerciseRole enum.
    /// Returns null if the string is null or whitespace.
    /// </summary>
    private static ExerciseRole? ParseRole(string? roleString)
    {
        if (string.IsNullOrWhiteSpace(roleString))
        {
            return null;
        }

        if (Enum.TryParse<ExerciseRole>(roleString, ignoreCase: true, out var role))
        {
            return role;
        }

        throw new ArgumentException($"Invalid role: {roleString}");
    }
}
