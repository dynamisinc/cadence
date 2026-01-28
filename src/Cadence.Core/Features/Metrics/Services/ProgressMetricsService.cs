using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service for calculating real-time exercise progress metrics.
/// </summary>
public class ProgressMetricsService : IProgressMetricsService
{
    private readonly AppDbContext _context;

    public ProgressMetricsService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ExerciseProgressDto?> GetExerciseProgressAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
            return null;

        // Get active MSEL with injects
        var msel = await _context.Msels
            .AsNoTracking()
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Phase)
            .FirstOrDefaultAsync(m => m.ExerciseId == exerciseId && m.IsActive && !m.IsDeleted);

        var injects = msel?.Injects.ToList() ?? new List<Inject>();

        // Calculate inject counts
        var totalInjects = injects.Count;
        var firedCount = injects.Count(i => i.Status == InjectStatus.Fired);
        var skippedCount = injects.Count(i => i.Status == InjectStatus.Skipped);
        var readyCount = injects.Count(i => i.Status == InjectStatus.Ready);
        var pendingCount = injects.Count(i => i.Status == InjectStatus.Pending || i.Status == InjectStatus.Ready);

        var progressPercentage = totalInjects > 0
            ? Math.Round((decimal)(firedCount + skippedCount) / totalInjects * 100, 1)
            : 0;

        // Get observation counts with ratings
        var observations = await _context.Observations
            .AsNoTracking()
            .Where(o => o.ExerciseId == exerciseId && !o.IsDeleted)
            .Select(o => o.Rating)
            .ToListAsync();

        var ratingCounts = new RatingCountsDto
        {
            Performed = observations.Count(r => r == ObservationRating.Performed),
            Satisfactory = observations.Count(r => r == ObservationRating.Satisfactory),
            Marginal = observations.Count(r => r == ObservationRating.Marginal),
            Unsatisfactory = observations.Count(r => r == ObservationRating.Unsatisfactory),
            Unrated = observations.Count(r => r == null)
        };

        // Get current phase name
        var currentPhaseName = GetCurrentPhaseName(injects);

        // Get next 3 upcoming injects (pending or ready, ordered by sequence)
        var nextInjects = injects
            .Where(i => i.Status == InjectStatus.Pending || i.Status == InjectStatus.Ready)
            .OrderBy(i => i.Sequence)
            .Take(3)
            .Select(i => new UpcomingInjectDto
            {
                Id = i.Id,
                InjectNumber = i.InjectNumber,
                Title = i.Title,
                ScheduledTime = i.ScheduledTime,
                DeliveryTime = i.DeliveryTime,
                PhaseName = i.Phase?.Name,
                Status = i.Status.ToString()
            })
            .ToList();

        // Calculate elapsed time (wall clock)
        var wallClockElapsed = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            wallClockElapsed += DateTime.UtcNow - exercise.ClockStartedAt.Value;
        }

        // Apply clock multiplier to get scenario time
        var elapsedTime = TimeSpan.FromTicks((long)(wallClockElapsed.Ticks * (double)exercise.ClockMultiplier));

        return new ExerciseProgressDto
        {
            TotalInjects = totalInjects,
            FiredCount = firedCount,
            SkippedCount = skippedCount,
            PendingCount = pendingCount,
            ReadyCount = readyCount,
            ProgressPercentage = progressPercentage,
            ObservationCount = observations.Count,
            ElapsedTime = elapsedTime,
            ClockStatus = exercise.ClockState.ToString(),
            CurrentPhaseName = currentPhaseName,
            NextInjects = nextInjects,
            RatingCounts = ratingCounts
        };
    }

    private static string? GetCurrentPhaseName(List<Inject> injects)
    {
        if (injects.Count == 0) return null;

        // Find most recently fired inject
        var lastFired = injects
            .Where(i => i.Status == InjectStatus.Fired && i.FiredAt.HasValue)
            .OrderByDescending(i => i.FiredAt)
            .FirstOrDefault();

        if (lastFired?.Phase != null)
            return lastFired.Phase.Name;

        // Fall back to first pending inject's phase
        var firstPending = injects
            .Where(i => i.Status == InjectStatus.Pending || i.Status == InjectStatus.Ready)
            .OrderBy(i => i.Sequence)
            .FirstOrDefault();

        return firstPending?.Phase?.Name;
    }
}
