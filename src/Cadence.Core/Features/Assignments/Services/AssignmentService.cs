using Cadence.Core.Data;
using Cadence.Core.Features.Assignments.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Assignments.Services;

/// <summary>
/// Service for managing user exercise assignments.
/// </summary>
public class AssignmentService : IAssignmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AssignmentService> _logger;

    public AssignmentService(AppDbContext context, ILogger<AssignmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MyAssignmentsResponse> GetMyAssignmentsAsync(string userId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting assignments for user {UserId}", userId);

        // Get all active assignments for the user with exercise data
        var assignments = await _context.ExerciseParticipants
            .Include(p => p.Exercise)
                .ThenInclude(e => e.ActiveMsel)
                    .ThenInclude(m => m!.Injects)
            .Where(p => p.UserId == userId && !p.IsDeleted && !p.Exercise.IsDeleted)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var active = new List<AssignmentDto>();
        var upcoming = new List<AssignmentDto>();
        var completed = new List<AssignmentDto>();

        foreach (var assignment in assignments)
        {
            var dto = MapToDto(assignment);

            // Determine which section the assignment belongs to
            var status = assignment.Exercise.Status;

            if (status == ExerciseStatus.Completed || status == ExerciseStatus.Archived)
            {
                completed.Add(dto);
            }
            else if (status == ExerciseStatus.Active || status == ExerciseStatus.Paused)
            {
                // Active/Paused exercises are "in conduct"
                active.Add(dto);
            }
            else
            {
                // Draft exercises or future scheduled dates go to Upcoming
                upcoming.Add(dto);
            }
        }

        // Sort each section appropriately
        var result = new MyAssignmentsResponse
        {
            // Active: by clock start time (most recent first), then by name
            Active = active
                .OrderByDescending(a => a.ClockState == "Running")
                .ThenByDescending(a => a.ElapsedSeconds ?? 0)
                .ThenBy(a => a.ExerciseName)
                .ToList(),

            // Upcoming: by scheduled date (soonest first)
            Upcoming = upcoming
                .OrderBy(a => a.ScheduledDate)
                .ThenBy(a => a.StartTime)
                .ThenBy(a => a.ExerciseName)
                .ToList(),

            // Completed: by completion date (most recent first)
            Completed = completed
                .OrderByDescending(a => a.CompletedAt)
                .ThenBy(a => a.ExerciseName)
                .ToList()
        };

        _logger.LogInformation(
            "Retrieved {ActiveCount} active, {UpcomingCount} upcoming, {CompletedCount} completed assignments for user {UserId}",
            result.Active.Count, result.Upcoming.Count, result.Completed.Count, userId);

        return result;
    }

    /// <inheritdoc />
    public async Task<AssignmentDto?> GetAssignmentAsync(string userId, Guid exerciseId, CancellationToken ct = default)
    {
        var assignment = await _context.ExerciseParticipants
            .Include(p => p.Exercise)
                .ThenInclude(e => e.ActiveMsel)
                    .ThenInclude(m => m!.Injects)
            .Where(p => p.UserId == userId && p.ExerciseId == exerciseId && !p.IsDeleted && !p.Exercise.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (assignment == null)
        {
            return null;
        }

        return MapToDto(assignment);
    }

    /// <summary>
    /// Map ExerciseParticipant to AssignmentDto.
    /// </summary>
    private static AssignmentDto MapToDto(ExerciseParticipant participant)
    {
        var exercise = participant.Exercise;
        var injects = exercise.ActiveMsel?.Injects ?? new List<Inject>();

        // Calculate elapsed time for running clock
        double? elapsedSeconds = null;
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            var elapsedSinceStart = DateTime.UtcNow - exercise.ClockStartedAt.Value;
            var previousElapsed = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;
            elapsedSeconds = (elapsedSinceStart + previousElapsed).TotalSeconds;
        }
        else if (exercise.ClockElapsedBeforePause.HasValue)
        {
            elapsedSeconds = exercise.ClockElapsedBeforePause.Value.TotalSeconds;
        }

        return new AssignmentDto
        {
            ExerciseId = exercise.Id,
            ExerciseName = exercise.Name,
            Role = participant.Role.ToString(),
            ExerciseStatus = exercise.Status.ToString(),
            ExerciseType = exercise.ExerciseType.ToString(),
            ScheduledDate = exercise.ScheduledDate,
            StartTime = exercise.StartTime,
            ClockState = exercise.ClockState.ToString(),
            ElapsedSeconds = elapsedSeconds,
            CompletedAt = exercise.CompletedAt,
            AssignedAt = participant.AssignedAt,
            TotalInjects = injects.Count(i => !i.IsDeleted),
            FiredInjects = injects.Count(i => !i.IsDeleted && i.Status == InjectStatus.Released),
            ReadyInjects = injects.Count(i => !i.IsDeleted && i.Status == InjectStatus.Synchronized),
            Location = exercise.Location,
            TimeZoneId = exercise.TimeZoneId
        };
    }
}
