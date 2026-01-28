using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service for calculating timeline and duration metrics.
/// </summary>
public class TimelineMetricsService : ITimelineMetricsService
{
    private readonly AppDbContext _context;

    public TimelineMetricsService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<TimelineSummaryDto?> GetTimelineSummaryAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .Include(e => e.Phases.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
            return null;

        // Get clock events with user info
        var clockEvents = await _context.ClockEvents
            .AsNoTracking()
            .Include(ce => ce.User)
            .Where(ce => ce.ExerciseId == exerciseId)
            .OrderBy(ce => ce.OccurredAt)
            .ToListAsync();

        // Get fired injects for pacing analysis
        var msel = await _context.Msels
            .AsNoTracking()
            .Include(m => m.Injects.Where(i => !i.IsDeleted && i.Status == InjectStatus.Fired))
                .ThenInclude(i => i.Phase)
            .FirstOrDefaultAsync(m => m.ExerciseId == exerciseId && m.IsActive && !m.IsDeleted);

        var firedInjects = msel?.Injects
            .Where(i => i.FiredAt.HasValue)
            .OrderBy(i => i.FiredAt)
            .ToList() ?? new List<Inject>();

        // Calculate planned duration from exercise start/end times
        TimeSpan? plannedDuration = null;
        if (exercise.StartTime.HasValue && exercise.EndTime.HasValue)
        {
            plannedDuration = exercise.EndTime.Value.ToTimeSpan() - exercise.StartTime.Value.ToTimeSpan();
            if (plannedDuration < TimeSpan.Zero)
            {
                // Handle overnight exercises
                plannedDuration = plannedDuration.Value.Add(TimeSpan.FromHours(24));
            }
        }

        // Calculate actual duration
        var actualDuration = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            actualDuration += DateTime.UtcNow - exercise.ClockStartedAt.Value;
        }

        // Duration variance
        TimeSpan? durationVariance = plannedDuration.HasValue
            ? actualDuration - plannedDuration.Value
            : null;

        // Find start and end times from clock events
        var firstStart = clockEvents.FirstOrDefault(ce => ce.EventType == ClockEventType.Started);
        var lastStop = clockEvents.LastOrDefault(ce => ce.EventType == ClockEventType.Stopped);

        DateTime? startedAt = firstStart?.OccurredAt;
        DateTime? endedAt = lastStop?.OccurredAt;
        TimeSpan? wallClockDuration = startedAt.HasValue && endedAt.HasValue
            ? endedAt.Value - startedAt.Value
            : null;

        // Calculate pause metrics
        var pauseEvents = CalculatePauseEvents(clockEvents);
        var totalPauseTime = TimeSpan.FromTicks(pauseEvents.Sum(p => p.Duration.Ticks));
        var avgPauseDuration = pauseEvents.Count > 0
            ? TimeSpan.FromTicks((long)pauseEvents.Average(p => p.Duration.Ticks))
            : (TimeSpan?)null;
        var longestPauseDuration = pauseEvents.Count > 0
            ? pauseEvents.Max(p => p.Duration)
            : (TimeSpan?)null;

        // Map clock events to DTOs
        var clockEventDtos = clockEvents.Select(ce => new ClockEventDto
        {
            EventType = ce.EventType.ToString(),
            OccurredAt = ce.OccurredAt,
            ElapsedTime = ce.ElapsedTimeAtEvent,
            UserName = ce.User?.DisplayName,
            Notes = ce.Notes
        }).ToList();

        // Calculate phase timings from inject firing times
        var phaseTimings = CalculatePhaseTimings(firedInjects, exercise.Phases.ToList());

        // Calculate inject pacing
        var injectPacing = CalculateInjectPacing(firedInjects, actualDuration);

        return new TimelineSummaryDto
        {
            PlannedDuration = plannedDuration,
            ActualDuration = actualDuration,
            DurationVariance = durationVariance,
            StartedAt = startedAt,
            EndedAt = endedAt,
            WallClockDuration = wallClockDuration,
            PauseCount = pauseEvents.Count,
            TotalPauseTime = totalPauseTime,
            AveragePauseDuration = avgPauseDuration,
            LongestPauseDuration = longestPauseDuration,
            PauseEvents = pauseEvents,
            ClockEvents = clockEventDtos,
            PhaseTimings = phaseTimings,
            InjectPacing = injectPacing
        };
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

    private static List<PauseEventDto> CalculatePauseEvents(List<ClockEvent> clockEvents)
    {
        var pauseEvents = new List<PauseEventDto>();

        for (int i = 0; i < clockEvents.Count; i++)
        {
            var evt = clockEvents[i];
            if (evt.EventType == ClockEventType.Paused)
            {
                // Find the next Started event (resume)
                var resumeEvent = clockEvents
                    .Skip(i + 1)
                    .FirstOrDefault(e => e.EventType == ClockEventType.Started);

                var pauseDuration = resumeEvent != null
                    ? resumeEvent.OccurredAt - evt.OccurredAt
                    : TimeSpan.Zero; // Still paused

                pauseEvents.Add(new PauseEventDto
                {
                    PausedAt = evt.OccurredAt,
                    ResumedAt = resumeEvent?.OccurredAt,
                    Duration = pauseDuration,
                    ElapsedAtPause = evt.ElapsedTimeAtEvent,
                    PausedByName = evt.User?.DisplayName,
                    ResumedByName = resumeEvent?.User?.DisplayName,
                    Notes = evt.Notes
                });
            }
        }

        return pauseEvents;
    }

    private static List<PhaseTimingDto> CalculatePhaseTimings(List<Inject> firedInjects, List<Phase> phases)
    {
        if (firedInjects.Count == 0)
            return new List<PhaseTimingDto>();

        // Group fired injects by phase
        var phaseGroups = firedInjects
            .GroupBy(i => new
            {
                i.PhaseId,
                PhaseName = i.Phase?.Name ?? "No Phase",
                Sequence = i.Phase?.Sequence ?? 999
            })
            .OrderBy(g => g.Key.Sequence);

        return phaseGroups.Select(g =>
        {
            var phaseInjects = g.OrderBy(i => i.FiredAt).ToList();
            var first = phaseInjects.First();
            var last = phaseInjects.Last();

            return new PhaseTimingDto
            {
                PhaseId = g.Key.PhaseId,
                PhaseName = g.Key.PhaseName,
                Sequence = g.Key.Sequence,
                StartedAt = first.FiredAt,
                EndedAt = last.FiredAt,
                Duration = first.FiredAt.HasValue && last.FiredAt.HasValue
                    ? last.FiredAt.Value - first.FiredAt.Value
                    : null,
                InjectsFired = phaseInjects.Count,
                // Elapsed time would require clock context - set to null for now
                ElapsedAtStart = null,
                ElapsedAtEnd = null
            };
        }).ToList();
    }

    private static InjectPacingDto CalculateInjectPacing(List<Inject> firedInjects, TimeSpan totalDuration)
    {
        if (firedInjects.Count == 0)
        {
            return new InjectPacingDto
            {
                TotalFired = 0
            };
        }

        var totalFired = firedInjects.Count;

        // Calculate gaps between consecutive inject fires
        var gaps = new List<TimeSpan>();
        for (int i = 1; i < firedInjects.Count; i++)
        {
            if (firedInjects[i].FiredAt.HasValue && firedInjects[i - 1].FiredAt.HasValue)
            {
                var gap = firedInjects[i].FiredAt!.Value - firedInjects[i - 1].FiredAt!.Value;
                gaps.Add(gap);
            }
        }

        TimeSpan? avgTimeBetween = gaps.Count > 0
            ? TimeSpan.FromTicks((long)gaps.Average(g => g.Ticks))
            : null;

        TimeSpan? shortestGap = gaps.Count > 0 ? gaps.Min() : null;
        TimeSpan? longestGap = gaps.Count > 0 ? gaps.Max() : null;

        // Calculate injects per hour
        decimal? injectsPerHour = totalDuration.TotalHours > 0
            ? Math.Round((decimal)(totalFired / totalDuration.TotalHours), 1)
            : null;

        // Find busiest 15-minute period
        var busiestPeriod = CalculateBusiestPeriod(firedInjects);

        return new InjectPacingDto
        {
            TotalFired = totalFired,
            AverageTimeBetweenInjects = avgTimeBetween,
            ShortestGap = shortestGap,
            LongestGap = longestGap,
            InjectsPerHour = injectsPerHour,
            BusiestPeriod = busiestPeriod
        };
    }

    private static BusiestPeriodDto? CalculateBusiestPeriod(List<Inject> firedInjects)
    {
        if (firedInjects.Count < 2)
            return null;

        var injectsWithTime = firedInjects.Where(i => i.FiredAt.HasValue).ToList();
        if (injectsWithTime.Count < 2)
            return null;

        var firstFire = injectsWithTime.Min(i => i.FiredAt!.Value);
        var lastFire = injectsWithTime.Max(i => i.FiredAt!.Value);

        // Slide a 15-minute window across the timeline
        var windowSize = TimeSpan.FromMinutes(15);
        var busiestStart = firstFire;
        var busiestCount = 0;

        var currentTime = firstFire;
        while (currentTime + windowSize <= lastFire)
        {
            var windowEnd = currentTime + windowSize;
            var count = injectsWithTime.Count(i =>
                i.FiredAt!.Value >= currentTime && i.FiredAt!.Value < windowEnd);

            if (count > busiestCount)
            {
                busiestCount = count;
                busiestStart = currentTime;
            }

            // Slide by 1 minute
            currentTime = currentTime.AddMinutes(1);
        }

        if (busiestCount <= 1)
            return null;

        return new BusiestPeriodDto
        {
            StartedAt = busiestStart,
            EndedAt = busiestStart + windowSize,
            InjectCount = busiestCount
        };
    }
}
