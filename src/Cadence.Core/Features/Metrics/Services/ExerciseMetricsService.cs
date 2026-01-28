using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service for calculating exercise metrics.
/// All calculations are performed server-side for accuracy.
/// </summary>
public class ExerciseMetricsService : IExerciseMetricsService
{
    private readonly AppDbContext _context;

    public ExerciseMetricsService(AppDbContext context)
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

        // Calculate elapsed time
        var elapsedTime = exercise.ClockElapsedBeforePause ?? TimeSpan.Zero;
        if (exercise.ClockState == ExerciseClockState.Running && exercise.ClockStartedAt.HasValue)
        {
            elapsedTime += DateTime.UtcNow - exercise.ClockStartedAt.Value;
        }

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

    /// <inheritdoc />
    public async Task<InjectSummaryDto?> GetInjectSummaryAsync(Guid exerciseId, int onTimeToleranceMinutes = 5)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
            return null;

        // Get active MSEL with injects, phases, and user info
        var msel = await _context.Msels
            .AsNoTracking()
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Phase)
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.FiredByUser)
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.SkippedByUser)
            .FirstOrDefaultAsync(m => m.ExerciseId == exerciseId && m.IsActive && !m.IsDeleted);

        var injects = msel?.Injects.ToList() ?? new List<Inject>();

        // Basic counts
        var totalCount = injects.Count;
        var firedCount = injects.Count(i => i.Status == InjectStatus.Fired);
        var skippedCount = injects.Count(i => i.Status == InjectStatus.Skipped);
        var notExecutedCount = injects.Count(i => i.Status == InjectStatus.Pending || i.Status == InjectStatus.Ready);

        // Calculate percentages
        var firedPercentage = totalCount > 0 ? Math.Round((decimal)firedCount / totalCount * 100, 1) : 0;
        var skippedPercentage = totalCount > 0 ? Math.Round((decimal)skippedCount / totalCount * 100, 1) : 0;
        var notExecutedPercentage = totalCount > 0 ? Math.Round((decimal)notExecutedCount / totalCount * 100, 1) : 0;

        // Calculate timing metrics for fired injects
        var firedInjects = injects.Where(i => i.Status == InjectStatus.Fired && i.FiredAt.HasValue).ToList();
        var timingMetrics = CalculateTimingMetrics(firedInjects, exercise.ActivatedAt, onTimeToleranceMinutes);

        // Group by phase
        var byPhase = injects
            .GroupBy(i => new { i.PhaseId, PhaseName = i.Phase?.Name ?? "No Phase", Sequence = i.Phase?.Sequence ?? 999 })
            .OrderBy(g => g.Key.Sequence)
            .Select(g =>
            {
                var phaseInjects = g.ToList();
                var phaseFired = phaseInjects.Where(i => i.Status == InjectStatus.Fired && i.FiredAt.HasValue).ToList();
                return new PhaseInjectSummaryDto
                {
                    PhaseId = g.Key.PhaseId,
                    PhaseName = g.Key.PhaseName,
                    Sequence = g.Key.Sequence,
                    TotalCount = phaseInjects.Count,
                    FiredCount = phaseInjects.Count(i => i.Status == InjectStatus.Fired),
                    SkippedCount = phaseInjects.Count(i => i.Status == InjectStatus.Skipped),
                    NotExecutedCount = phaseInjects.Count(i => i.Status == InjectStatus.Pending || i.Status == InjectStatus.Ready),
                    OnTimeRate = CalculateOnTimeRate(phaseFired, exercise.ActivatedAt, onTimeToleranceMinutes)
                };
            })
            .ToList();

        // Group by controller (who fired the inject)
        var byController = firedInjects
            .GroupBy(i => new { i.FiredByUserId, FiredByName = i.FiredByUser?.DisplayName ?? "Unknown" })
            .Select(g =>
            {
                var controllerInjects = g.ToList();
                var variances = CalculateVariances(controllerInjects, exercise.ActivatedAt);
                return new ControllerInjectSummaryDto
                {
                    // Parse string ApplicationUser.Id to Guid for DTO backward compatibility
                    ControllerId = string.IsNullOrEmpty(g.Key.FiredByUserId) ? null : Guid.Parse(g.Key.FiredByUserId),
                    ControllerName = g.Key.FiredByName,
                    FiredCount = controllerInjects.Count,
                    AverageVariance = variances.Count > 0 ? TimeSpan.FromTicks((long)variances.Average(v => v.Ticks)) : null,
                    OnTimeRate = CalculateOnTimeRate(controllerInjects, exercise.ActivatedAt, onTimeToleranceMinutes)
                };
            })
            .OrderByDescending(c => c.FiredCount)
            .ToList();

        // Skipped injects with reasons
        var skippedInjects = injects
            .Where(i => i.Status == InjectStatus.Skipped)
            .OrderBy(i => i.Sequence)
            .Select(i => new SkippedInjectDto
            {
                Id = i.Id,
                InjectNumber = i.InjectNumber,
                Title = i.Title,
                PhaseName = i.Phase?.Name,
                SkipReason = i.SkipReason,
                SkippedAt = i.SkippedAt,
                SkippedByName = i.SkippedByUser?.DisplayName
            })
            .ToList();

        return new InjectSummaryDto
        {
            TotalCount = totalCount,
            FiredCount = firedCount,
            SkippedCount = skippedCount,
            NotExecutedCount = notExecutedCount,
            FiredPercentage = firedPercentage,
            SkippedPercentage = skippedPercentage,
            NotExecutedPercentage = notExecutedPercentage,
            OnTimeRate = timingMetrics.OnTimeRate,
            OnTimeCount = timingMetrics.OnTimeCount,
            AverageVariance = timingMetrics.AverageVariance,
            EarliestVariance = timingMetrics.EarliestVariance,
            LatestVariance = timingMetrics.LatestVariance,
            ByPhase = byPhase,
            ByController = byController,
            SkippedInjects = skippedInjects
        };
    }

    /// <inheritdoc />
    public async Task<ObservationSummaryDto?> GetObservationSummaryAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .Include(e => e.Objectives.Where(o => !o.IsDeleted))
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
            return null;

        // Get all observations with related data
        var observations = await _context.Observations
            .AsNoTracking()
            .Include(o => o.CreatedByUser)
            .Include(o => o.Inject)
                .ThenInclude(i => i!.Phase)
            .Where(o => o.ExerciseId == exerciseId && !o.IsDeleted)
            .ToListAsync();

        var totalCount = observations.Count;

        // Rating distribution
        var ratingDistribution = CalculateRatingDistribution(observations);

        // Coverage calculation
        var objectives = exercise.Objectives.ToList();
        var totalObjectives = objectives.Count;
        var observedObjectiveIds = observations
            .Where(o => o.ObjectiveId.HasValue)
            .Select(o => o.ObjectiveId!.Value)
            .Distinct()
            .ToHashSet();
        var objectivesCovered = objectives.Count(o => observedObjectiveIds.Contains(o.Id));
        var coverageRate = totalObjectives > 0
            ? Math.Round((decimal)objectivesCovered / totalObjectives * 100, 1)
            : (decimal?)null;

        // Uncovered objectives
        var uncoveredObjectives = objectives
            .Where(o => !observedObjectiveIds.Contains(o.Id))
            .Select(o => new UncoveredObjectiveDto
            {
                Id = o.Id,
                ObjectiveNumber = o.ObjectiveNumber,
                Name = o.Name
            })
            .ToList();

        // By evaluator
        var byEvaluator = observations
            .GroupBy(o => new { o.CreatedByUserId, EvaluatorName = o.CreatedByUser?.DisplayName ?? "Unknown" })
            .Select(g =>
            {
                var evalObs = g.ToList();
                var rated = evalObs.Where(o => o.Rating.HasValue).ToList();
                return new EvaluatorSummaryDto
                {
                    EvaluatorId = g.Key.CreatedByUserId,
                    EvaluatorName = g.Key.EvaluatorName,
                    ObservationCount = evalObs.Count,
                    AverageRating = rated.Count > 0 ? Math.Round((decimal)rated.Average(o => RatingToNumeric(o.Rating!.Value)), 2) : null,
                    RatingCounts = new RatingCountsDto
                    {
                        Performed = evalObs.Count(o => o.Rating == ObservationRating.Performed),
                        Satisfactory = evalObs.Count(o => o.Rating == ObservationRating.Satisfactory),
                        Marginal = evalObs.Count(o => o.Rating == ObservationRating.Marginal),
                        Unsatisfactory = evalObs.Count(o => o.Rating == ObservationRating.Unsatisfactory),
                        Unrated = evalObs.Count(o => o.Rating == null)
                    }
                };
            })
            .OrderByDescending(e => e.ObservationCount)
            .ToList();

        // By phase (through linked inject)
        var byPhase = observations
            .GroupBy(o => new
            {
                PhaseId = o.Inject?.PhaseId,
                PhaseName = o.Inject?.Phase?.Name ?? "No Phase",
                Sequence = o.Inject?.Phase?.Sequence ?? 999
            })
            .OrderBy(g => g.Key.Sequence)
            .Select(g =>
            {
                var phaseObs = g.ToList();
                return new PhaseObservationSummaryDto
                {
                    PhaseId = g.Key.PhaseId,
                    PhaseName = g.Key.PhaseName,
                    Sequence = g.Key.Sequence,
                    ObservationCount = phaseObs.Count,
                    RatingCounts = new RatingCountsDto
                    {
                        Performed = phaseObs.Count(o => o.Rating == ObservationRating.Performed),
                        Satisfactory = phaseObs.Count(o => o.Rating == ObservationRating.Satisfactory),
                        Marginal = phaseObs.Count(o => o.Rating == ObservationRating.Marginal),
                        Unsatisfactory = phaseObs.Count(o => o.Rating == ObservationRating.Unsatisfactory),
                        Unrated = phaseObs.Count(o => o.Rating == null)
                    }
                };
            })
            .ToList();

        // Linking statistics
        var linkedToInjectCount = observations.Count(o => o.InjectId.HasValue);
        var linkedToObjectiveCount = observations.Count(o => o.ObjectiveId.HasValue);
        var unlinkedCount = observations.Count(o => !o.InjectId.HasValue && !o.ObjectiveId.HasValue);

        return new ObservationSummaryDto
        {
            TotalCount = totalCount,
            RatingDistribution = ratingDistribution,
            CoverageRate = coverageRate,
            ObjectivesCovered = objectivesCovered,
            TotalObjectives = totalObjectives,
            UncoveredObjectives = uncoveredObjectives,
            ByEvaluator = byEvaluator,
            ByPhase = byPhase,
            LinkedToInjectCount = linkedToInjectCount,
            LinkedToObjectiveCount = linkedToObjectiveCount,
            UnlinkedCount = unlinkedCount
        };
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

    private record TimingMetricsResult(
        decimal? OnTimeRate,
        int OnTimeCount,
        TimeSpan? AverageVariance,
        TimingVarianceDto? EarliestVariance,
        TimingVarianceDto? LatestVariance);

    private static TimingMetricsResult CalculateTimingMetrics(
        List<Inject> firedInjects,
        DateTime? exerciseActivatedAt,
        int toleranceMinutes)
    {
        if (firedInjects.Count == 0 || !exerciseActivatedAt.HasValue)
        {
            return new TimingMetricsResult(null, 0, null, null, null);
        }

        var variances = firedInjects
            .Where(i => i.DeliveryTime.HasValue && i.FiredAt.HasValue)
            .Select(i =>
            {
                var expectedTime = exerciseActivatedAt.Value + i.DeliveryTime!.Value;
                var variance = i.FiredAt!.Value - expectedTime;
                return new { Inject = i, Variance = variance };
            })
            .ToList();

        if (variances.Count == 0)
        {
            return new TimingMetricsResult(null, 0, null, null, null);
        }

        var tolerance = TimeSpan.FromMinutes(toleranceMinutes);
        var onTimeCount = variances.Count(v => Math.Abs(v.Variance.TotalMinutes) <= toleranceMinutes);
        var onTimeRate = Math.Round((decimal)onTimeCount / variances.Count * 100, 1);

        var avgVariance = TimeSpan.FromTicks((long)variances.Average(v => v.Variance.Ticks));

        var earliest = variances.MinBy(v => v.Variance);
        var latest = variances.MaxBy(v => v.Variance);

        return new TimingMetricsResult(
            onTimeRate,
            onTimeCount,
            avgVariance,
            earliest != null ? new TimingVarianceDto
            {
                InjectId = earliest.Inject.Id,
                InjectNumber = earliest.Inject.InjectNumber,
                Title = earliest.Inject.Title,
                Variance = earliest.Variance
            } : null,
            latest != null ? new TimingVarianceDto
            {
                InjectId = latest.Inject.Id,
                InjectNumber = latest.Inject.InjectNumber,
                Title = latest.Inject.Title,
                Variance = latest.Variance
            } : null
        );
    }

    private static decimal? CalculateOnTimeRate(
        List<Inject> firedInjects,
        DateTime? exerciseActivatedAt,
        int toleranceMinutes)
    {
        if (firedInjects.Count == 0 || !exerciseActivatedAt.HasValue)
            return null;

        var withTiming = firedInjects
            .Where(i => i.DeliveryTime.HasValue && i.FiredAt.HasValue)
            .ToList();

        if (withTiming.Count == 0)
            return null;

        var onTimeCount = withTiming.Count(i =>
        {
            var expectedTime = exerciseActivatedAt.Value + i.DeliveryTime!.Value;
            var variance = Math.Abs((i.FiredAt!.Value - expectedTime).TotalMinutes);
            return variance <= toleranceMinutes;
        });

        return Math.Round((decimal)onTimeCount / withTiming.Count * 100, 1);
    }

    private static List<TimeSpan> CalculateVariances(
        List<Inject> firedInjects,
        DateTime? exerciseActivatedAt)
    {
        if (!exerciseActivatedAt.HasValue)
            return new List<TimeSpan>();

        return firedInjects
            .Where(i => i.DeliveryTime.HasValue && i.FiredAt.HasValue)
            .Select(i =>
            {
                var expectedTime = exerciseActivatedAt.Value + i.DeliveryTime!.Value;
                return i.FiredAt!.Value - expectedTime;
            })
            .ToList();
    }

    private static RatingDistributionDto CalculateRatingDistribution(List<Observation> observations)
    {
        var total = observations.Count;
        if (total == 0)
        {
            return new RatingDistributionDto();
        }

        var performedCount = observations.Count(o => o.Rating == ObservationRating.Performed);
        var satisfactoryCount = observations.Count(o => o.Rating == ObservationRating.Satisfactory);
        var marginalCount = observations.Count(o => o.Rating == ObservationRating.Marginal);
        var unsatisfactoryCount = observations.Count(o => o.Rating == ObservationRating.Unsatisfactory);
        var unratedCount = observations.Count(o => o.Rating == null);

        var rated = observations.Where(o => o.Rating.HasValue).ToList();
        var avgRating = rated.Count > 0
            ? Math.Round((decimal)rated.Average(o => RatingToNumeric(o.Rating!.Value)), 2)
            : (decimal?)null;

        return new RatingDistributionDto
        {
            PerformedCount = performedCount,
            PerformedPercentage = Math.Round((decimal)performedCount / total * 100, 1),
            SatisfactoryCount = satisfactoryCount,
            SatisfactoryPercentage = Math.Round((decimal)satisfactoryCount / total * 100, 1),
            MarginalCount = marginalCount,
            MarginalPercentage = Math.Round((decimal)marginalCount / total * 100, 1),
            UnsatisfactoryCount = unsatisfactoryCount,
            UnsatisfactoryPercentage = Math.Round((decimal)unsatisfactoryCount / total * 100, 1),
            UnratedCount = unratedCount,
            UnratedPercentage = Math.Round((decimal)unratedCount / total * 100, 1),
            AverageRating = avgRating
        };
    }

    private static int RatingToNumeric(ObservationRating rating)
    {
        return rating switch
        {
            ObservationRating.Performed => 1,
            ObservationRating.Satisfactory => 2,
            ObservationRating.Marginal => 3,
            ObservationRating.Unsatisfactory => 4,
            _ => 0
        };
    }

    // =========================================================================
    // S04 Timeline Helper Methods
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

    // =========================================================================
    // S07: Controller Activity Methods
    // =========================================================================

    /// <inheritdoc />
    public async Task<ControllerActivitySummaryDto?> GetControllerActivityAsync(Guid exerciseId, int onTimeToleranceMinutes = 5)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
            return null;

        // Get active MSEL with injects and user info
        var msel = await _context.Msels
            .AsNoTracking()
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.Phase)
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.FiredByUser)
            .Include(m => m.Injects.Where(i => !i.IsDeleted))
                .ThenInclude(i => i.SkippedByUser)
            .FirstOrDefaultAsync(m => m.ExerciseId == exerciseId && m.IsActive && !m.IsDeleted);

        var injects = msel?.Injects.ToList() ?? new List<Inject>();

        var firedInjects = injects.Where(i => i.Status == InjectStatus.Fired).ToList();
        var skippedInjects = injects.Where(i => i.Status == InjectStatus.Skipped).ToList();

        var totalFired = firedInjects.Count;
        var totalSkipped = skippedInjects.Count;

        // Group by controller (FiredByUserId for fired, SkippedByUserId for skipped)
        var controllerActivity = new Dictionary<string, ControllerActivityBuilder>();

        // Process fired injects
        foreach (var inject in firedInjects)
        {
            if (string.IsNullOrEmpty(inject.FiredByUserId)) continue;

            var controllerId = inject.FiredByUserId;
            if (!controllerActivity.TryGetValue(controllerId, out var builder))
            {
                builder = new ControllerActivityBuilder
                {
                    ControllerId = Guid.Parse(controllerId),
                    ControllerName = inject.FiredByUser?.DisplayName ?? "Unknown"
                };
                controllerActivity[controllerId] = builder;
            }

            builder.FiredInjects.Add(inject);
        }

        // Process skipped injects
        foreach (var inject in skippedInjects)
        {
            if (string.IsNullOrEmpty(inject.SkippedByUserId)) continue;

            var controllerId = inject.SkippedByUserId;
            if (!controllerActivity.TryGetValue(controllerId, out var builder))
            {
                builder = new ControllerActivityBuilder
                {
                    ControllerId = Guid.Parse(controllerId),
                    ControllerName = inject.SkippedByUser?.DisplayName ?? "Unknown"
                };
                controllerActivity[controllerId] = builder;
            }

            builder.SkippedInjects.Add(inject);
        }

        // Build controller DTOs
        var controllers = controllerActivity.Values
            .OrderByDescending(c => c.FiredInjects.Count)
            .Select(c =>
            {
                var firedCount = c.FiredInjects.Count;
                var skippedCount = c.SkippedInjects.Count;
                var workloadPct = totalFired > 0
                    ? Math.Round((decimal)firedCount / totalFired * 100, 1)
                    : 0;

                // Calculate on-time stats
                var onTimeCount = 0;
                var variances = new List<TimeSpan>();

                foreach (var inject in c.FiredInjects)
                {
                    if (inject.FiredAt.HasValue)
                    {
                        var scheduled = inject.ScheduledTime.ToTimeSpan();
                        var actual = inject.FiredAt.Value.TimeOfDay;
                        var variance = actual - scheduled;
                        variances.Add(variance);

                        if (Math.Abs(variance.TotalMinutes) <= onTimeToleranceMinutes)
                            onTimeCount++;
                    }
                }

                var avgVariance = variances.Count > 0
                    ? TimeSpan.FromTicks((long)variances.Average(v => v.Ticks))
                    : (TimeSpan?)null;

                var onTimeRate = firedCount > 0
                    ? Math.Round((decimal)onTimeCount / firedCount * 100, 1)
                    : (decimal?)null;

                // Phase activity
                var phaseActivity = c.FiredInjects
                    .Concat(c.SkippedInjects)
                    .GroupBy(i => new
                    {
                        i.PhaseId,
                        PhaseName = i.Phase?.Name ?? "No Phase",
                        Sequence = i.Phase?.Sequence ?? 999
                    })
                    .OrderBy(g => g.Key.Sequence)
                    .Select(g => new ControllerPhaseActivityDto
                    {
                        PhaseId = g.Key.PhaseId,
                        PhaseName = g.Key.PhaseName,
                        Sequence = g.Key.Sequence,
                        InjectsFired = g.Count(i => i.Status == InjectStatus.Fired),
                        InjectsSkipped = g.Count(i => i.Status == InjectStatus.Skipped)
                    })
                    .ToList();

                return new ControllerActivityDto
                {
                    ControllerId = c.ControllerId.ToString(),
                    ControllerName = c.ControllerName,
                    InjectsFired = firedCount,
                    InjectsSkipped = skippedCount,
                    WorkloadPercentage = workloadPct,
                    OnTimeRate = onTimeRate,
                    OnTimeCount = onTimeCount,
                    AverageVariance = avgVariance,
                    PhaseActivity = phaseActivity,
                    FirstFireAt = c.FiredInjects.Where(i => i.FiredAt.HasValue).Select(i => i.FiredAt).Min(),
                    LastFireAt = c.FiredInjects.Where(i => i.FiredAt.HasValue).Select(i => i.FiredAt).Max()
                };
            })
            .ToList();

        // Calculate overall on-time rate
        var allOnTime = controllers.Sum(c => c.OnTimeCount);
        var overallOnTimeRate = totalFired > 0
            ? Math.Round((decimal)allOnTime / totalFired * 100, 1)
            : (decimal?)null;

        return new ControllerActivitySummaryDto
        {
            TotalControllers = controllers.Count,
            TotalInjectsFired = totalFired,
            TotalInjectsSkipped = totalSkipped,
            OverallOnTimeRate = overallOnTimeRate,
            Controllers = controllers
        };
    }

    /// <summary>
    /// Helper class for building controller activity data.
    /// </summary>
    private class ControllerActivityBuilder
    {
        public Guid ControllerId { get; set; }
        public string ControllerName { get; set; } = string.Empty;
        public List<Inject> FiredInjects { get; } = new();
        public List<Inject> SkippedInjects { get; } = new();
    }

    // =========================================================================
    // S08: Evaluator Coverage Methods
    // =========================================================================

    /// <inheritdoc />
    public async Task<EvaluatorCoverageSummaryDto?> GetEvaluatorCoverageAsync(Guid exerciseId)
    {
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId && !e.IsDeleted);

        if (exercise == null)
            return null;

        // Get objectives
        var objectives = await _context.Set<Objective>()
            .AsNoTracking()
            .Where(o => o.ExerciseId == exerciseId && !o.IsDeleted)
            .ToListAsync();

        // Get observations with user, inject info (for phase), and capability tags
        var observations = await _context.Observations
            .AsNoTracking()
            .Include(o => o.CreatedByUser)
            .Include(o => o.Inject)
                .ThenInclude(i => i!.Phase)
            .Include(o => o.ObservationCapabilities)
            .Where(o => o.ExerciseId == exerciseId && !o.IsDeleted)
            .ToListAsync();

        var totalObjectives = objectives.Count;
        var coveredObjectiveIds = observations
            .Where(o => o.ObjectiveId.HasValue)
            .Select(o => o.ObjectiveId!.Value)
            .Distinct()
            .ToHashSet();

        var objectivesCovered = coveredObjectiveIds.Count;
        var coverageRate = totalObjectives > 0
            ? Math.Round((decimal)objectivesCovered / totalObjectives * 100, 1)
            : (decimal?)null;

        // Get all covered capability IDs
        var allCoveredCapabilityIds = observations
            .SelectMany(o => o.ObservationCapabilities.Select(oc => oc.CoreCapabilityId))
            .Distinct()
            .ToHashSet();

        // Group by evaluator
        var evaluatorGroups = observations
            .GroupBy(o => new
            {
                EvaluatorId = o.CreatedByUserId,
                EvaluatorName = o.CreatedByUser?.DisplayName ?? "Unknown"
            })
            .ToList();

        var evaluators = evaluatorGroups
            .OrderByDescending(g => g.Count())
            .Select(g =>
            {
                var evalObservations = g.ToList();
                var evalObjectivesCovered = evalObservations
                    .Where(o => o.ObjectiveId.HasValue)
                    .Select(o => o.ObjectiveId!.Value)
                    .Distinct()
                    .Count();

                // Calculate capabilities covered by this evaluator
                var evalCapabilitiesCovered = evalObservations
                    .SelectMany(o => o.ObservationCapabilities.Select(oc => oc.CoreCapabilityId))
                    .Distinct()
                    .Count();

                var rated = evalObservations.Where(o => o.Rating.HasValue).ToList();
                var avgRating = rated.Count > 0
                    ? Math.Round((decimal)rated.Average(o => RatingToNumeric(o.Rating!.Value)), 2)
                    : (decimal?)null;

                // Phase activity
                var phaseActivity = evalObservations
                    .GroupBy(o => new
                    {
                        PhaseId = o.Inject?.PhaseId,
                        PhaseName = o.Inject?.Phase?.Name ?? "Unlinked",
                        Sequence = o.Inject?.Phase?.Sequence ?? 999
                    })
                    .OrderBy(pg => pg.Key.Sequence)
                    .Select(pg => new EvaluatorPhaseActivityDto
                    {
                        PhaseId = pg.Key.PhaseId,
                        PhaseName = pg.Key.PhaseName,
                        Sequence = pg.Key.Sequence,
                        ObservationCount = pg.Count()
                    })
                    .ToList();

                return new EvaluatorCoverageDto
                {
                    EvaluatorId = g.Key.EvaluatorId,
                    EvaluatorName = g.Key.EvaluatorName,
                    ObservationCount = evalObservations.Count,
                    ObjectivesCovered = evalObjectivesCovered,
                    CapabilitiesCovered = evalCapabilitiesCovered,
                    AverageRating = avgRating,
                    RatingCounts = new RatingCountsDto
                    {
                        Performed = evalObservations.Count(o => o.Rating == ObservationRating.Performed),
                        Satisfactory = evalObservations.Count(o => o.Rating == ObservationRating.Satisfactory),
                        Marginal = evalObservations.Count(o => o.Rating == ObservationRating.Marginal),
                        Unsatisfactory = evalObservations.Count(o => o.Rating == ObservationRating.Unsatisfactory),
                        Unrated = evalObservations.Count(o => o.Rating == null)
                    },
                    PhaseActivity = phaseActivity,
                    FirstObservationAt = evalObservations.Min(o => o.ObservedAt),
                    LastObservationAt = evalObservations.Max(o => o.ObservedAt)
                };
            })
            .ToList();

        // Build coverage matrix
        var evaluatorIds = evaluators.Select(e => e.EvaluatorId).Where(id => id != null).ToList();
        var coverageMatrix = objectives
            .OrderBy(o => o.ObjectiveNumber)
            .Select(obj =>
            {
                var objObservations = observations.Where(o => o.ObjectiveId == obj.Id).ToList();
                var byEvaluator = evaluatorIds.ToDictionary(
                    id => id!,
                    id => objObservations.Count(o => o.CreatedByUserId == id)
                );

                var total = objObservations.Count;
                var status = total == 0 ? "None" : total <= 2 ? "Low" : "Good";

                return new ObjectiveCoverageRowDto
                {
                    ObjectiveId = obj.Id,
                    ObjectiveNumber = obj.ObjectiveNumber,
                    ObjectiveName = obj.Name,
                    TotalObservations = total,
                    ByEvaluator = byEvaluator,
                    CoverageStatus = status
                };
            })
            .ToList();

        // Uncovered objectives
        var uncoveredObjectives = objectives
            .Where(o => !coveredObjectiveIds.Contains(o.Id))
            .Select(o => new UncoveredObjectiveDto
            {
                Id = o.Id,
                ObjectiveNumber = o.ObjectiveNumber,
                Name = o.Name
            })
            .ToList();

        // Low coverage objectives (1-2 observations)
        var lowCoverageObjectives = coverageMatrix
            .Where(row => row.TotalObservations > 0 && row.TotalObservations <= 2)
            .Select(row => new LowCoverageObjectiveDto
            {
                Id = row.ObjectiveId,
                ObjectiveNumber = row.ObjectiveNumber,
                Name = row.ObjectiveName,
                ObservationCount = row.TotalObservations
            })
            .ToList();

        // Calculate evaluator consistency indicator
        var consistency = CalculateEvaluatorConsistency(evaluators);

        return new EvaluatorCoverageSummaryDto
        {
            TotalEvaluators = evaluators.Count,
            TotalObservations = observations.Count,
            ObjectivesCovered = objectivesCovered,
            TotalObjectives = totalObjectives,
            ObjectiveCoverageRate = coverageRate,
            CapabilitiesCovered = allCoveredCapabilityIds.Count,
            TotalCapabilities = allCoveredCapabilityIds.Count, // Only count capabilities actually used
            Consistency = consistency,
            Evaluators = evaluators,
            CoverageMatrix = coverageMatrix,
            UncoveredObjectives = uncoveredObjectives,
            LowCoverageObjectives = lowCoverageObjectives
        };
    }

    /// <summary>
    /// Calculate evaluator consistency based on rating variance.
    /// </summary>
    private static EvaluatorConsistencyDto? CalculateEvaluatorConsistency(List<EvaluatorCoverageDto> evaluators)
    {
        // Need at least 2 evaluators with ratings to calculate consistency
        var evaluatorsWithRatings = evaluators
            .Where(e => e.AverageRating.HasValue)
            .ToList();

        if (evaluatorsWithRatings.Count < 2)
        {
            return new EvaluatorConsistencyDto
            {
                Level = "Insufficient Data",
                OverallAverageRating = evaluatorsWithRatings.FirstOrDefault()?.AverageRating ?? 0,
                RatingStandardDeviation = 0,
                Description = "Need at least 2 evaluators with rated observations to calculate consistency."
            };
        }

        var ratings = evaluatorsWithRatings.Select(e => e.AverageRating!.Value).ToList();
        var overallAvg = Math.Round(ratings.Average(), 2);

        // Calculate standard deviation
        var variance = ratings.Average(r => Math.Pow((double)(r - overallAvg), 2));
        var stdDev = Math.Round((decimal)Math.Sqrt(variance), 2);

        // Determine consistency level based on standard deviation
        // Lower std dev = more consistent
        string level;
        string description;
        if (stdDev <= 0.3m)
        {
            level = "High";
            description = "Evaluators are rating consistently with minimal variance.";
        }
        else if (stdDev <= 0.6m)
        {
            level = "Moderate";
            description = "Evaluators show some variation in rating patterns.";
        }
        else
        {
            level = "Low";
            description = "Significant rating variance between evaluators - consider calibration.";
        }

        // Find harsh and lenient raters (deviation > 0.5 from overall average)
        var harshRaters = evaluatorsWithRatings
            .Where(e => e.AverageRating!.Value > overallAvg + 0.5m)
            .Select(e => new EvaluatorRatingBiasDto
            {
                EvaluatorName = e.EvaluatorName,
                AverageRating = e.AverageRating!.Value,
                Deviation = Math.Round(e.AverageRating!.Value - overallAvg, 2)
            })
            .OrderByDescending(e => e.Deviation)
            .ToList();

        var lenientRaters = evaluatorsWithRatings
            .Where(e => e.AverageRating!.Value < overallAvg - 0.5m)
            .Select(e => new EvaluatorRatingBiasDto
            {
                EvaluatorName = e.EvaluatorName,
                AverageRating = e.AverageRating!.Value,
                Deviation = Math.Round(e.AverageRating!.Value - overallAvg, 2)
            })
            .OrderBy(e => e.Deviation)
            .ToList();

        return new EvaluatorConsistencyDto
        {
            Level = level,
            OverallAverageRating = overallAvg,
            RatingStandardDeviation = stdDev,
            HarshRaters = harshRaters,
            LenientRaters = lenientRaters,
            Description = description
        };
    }

    // =========================================================================
    // S06: Capability Performance
    // =========================================================================

    /// <inheritdoc />
    public async Task<CapabilityPerformanceSummaryDto?> GetCapabilityPerformanceAsync(Guid exerciseId)
    {
        // Verify exercise exists
        var exercise = await _context.Exercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise == null)
            return null;

        // Get all observations for this exercise with their capability tags
        var observations = await _context.Observations
            .AsNoTracking()
            .Where(o => o.ExerciseId == exerciseId)
            .Include(o => o.ObservationCapabilities)
                .ThenInclude(oc => oc.CoreCapability)
            .ToListAsync();

        // Get target capabilities for this exercise
        var targetCapabilityIds = await _context.ExerciseTargetCapabilities
            .AsNoTracking()
            .Where(etc => etc.ExerciseId == exerciseId)
            .Select(etc => etc.CoreCapabilityId)
            .ToListAsync();

        // Get all core capabilities for reference
        var allCapabilities = await _context.CoreCapabilities
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync();

        // Count total observations and tagged observations
        var totalObservations = observations.Count;
        var taggedObservations = observations.Where(o => o.ObservationCapabilities.Any()).ToList();
        var totalTaggedObservations = taggedObservations.Count;
        var taggingRate = totalObservations > 0
            ? Math.Round((decimal)totalTaggedObservations / totalObservations * 100, 1)
            : 0m;

        // Group observations by capability
        var observationsByCapability = taggedObservations
            .SelectMany(o => o.ObservationCapabilities.Select(oc => new { Observation = o, Capability = oc.CoreCapability }))
            .GroupBy(x => x.Capability.Id)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Build capability performance list
        var capabilities = allCapabilities
            .Where(c => observationsByCapability.ContainsKey(c.Id))
            .Select(capability =>
            {
                var capObservations = observationsByCapability[capability.Id];
                var ratedObservations = capObservations.Where(x => x.Observation.Rating != null).ToList();

                var performed = ratedObservations.Count(x => x.Observation.Rating == ObservationRating.Performed);
                var satisfactory = ratedObservations.Count(x => x.Observation.Rating == ObservationRating.Satisfactory);
                var marginal = ratedObservations.Count(x => x.Observation.Rating == ObservationRating.Marginal);
                var unsatisfactory = ratedObservations.Count(x => x.Observation.Rating == ObservationRating.Unsatisfactory);
                var unrated = capObservations.Count - ratedObservations.Count;

                decimal? avgRating = null;
                if (ratedObservations.Any())
                {
                    var sum = performed * 1 + satisfactory * 2 + marginal * 3 + unsatisfactory * 4;
                    avgRating = Math.Round((decimal)sum / ratedObservations.Count, 2);
                }

                var ratingCategory = avgRating switch
                {
                    null => "No Rated Observations",
                    <= 1.5m => "Performed",
                    <= 2.5m => "Satisfactory",
                    <= 3.5m => "Marginal",
                    _ => "Unsatisfactory"
                };

                var performanceLevel = avgRating switch
                {
                    null => "Unknown",
                    <= 1.5m => "Good",
                    <= 2.5m => "Satisfactory",
                    <= 3.5m => "Needs Improvement",
                    _ => "Critical"
                };

                return new CapabilityPerformanceDto
                {
                    CapabilityId = capability.Id,
                    Name = capability.Name,
                    MissionArea = capability.MissionArea.ToString(),
                    ObservationCount = capObservations.Count,
                    AverageRating = avgRating,
                    RatingCategory = ratingCategory,
                    RatingCounts = new RatingCountsDto
                    {
                        Performed = performed,
                        Satisfactory = satisfactory,
                        Marginal = marginal,
                        Unsatisfactory = unsatisfactory,
                        Unrated = unrated
                    },
                    IsTargetCapability = targetCapabilityIds.Contains(capability.Id),
                    PerformanceLevel = performanceLevel
                };
            })
            // Sort by average rating (worst first), then by name
            .OrderByDescending(c => c.AverageRating ?? 0)
            .ThenBy(c => c.Name)
            .ToList();

        // Calculate target capability coverage
        var evaluatedCapabilityIds = capabilities.Select(c => c.CapabilityId).ToHashSet();
        var targetCapabilitiesEvaluated = targetCapabilityIds.Count(id => evaluatedCapabilityIds.Contains(id));
        var targetCoverageRate = targetCapabilityIds.Count > 0
            ? Math.Round((decimal)targetCapabilitiesEvaluated / targetCapabilityIds.Count * 100, 1)
            : (decimal?)null;

        // Unevaluated target capabilities
        var unevaluatedTargets = allCapabilities
            .Where(c => targetCapabilityIds.Contains(c.Id) && !evaluatedCapabilityIds.Contains(c.Id))
            .Select(c => new UnevaluatedCapabilityDto
            {
                Id = c.Id,
                Name = c.Name,
                MissionArea = c.MissionArea.ToString()
            })
            .ToList();

        // Group by mission area
        var byMissionArea = capabilities
            .GroupBy(c => c.MissionArea)
            .Select(g =>
            {
                var areaObservations = g.Sum(c => c.ObservationCount);
                var areaRated = g.Where(c => c.AverageRating != null).ToList();
                decimal? areaAvgRating = null;
                if (areaRated.Any())
                {
                    // Weighted average by observation count
                    var weightedSum = areaRated.Sum(c => (c.AverageRating ?? 0) * c.ObservationCount);
                    var totalRated = areaRated.Sum(c => c.ObservationCount);
                    if (totalRated > 0)
                        areaAvgRating = Math.Round(weightedSum / totalRated, 2);
                }

                return new MissionAreaSummaryDto
                {
                    MissionArea = g.Key,
                    CapabilitiesEvaluated = g.Count(),
                    ObservationCount = areaObservations,
                    AverageRating = areaAvgRating,
                    RatingCounts = new RatingCountsDto
                    {
                        Performed = g.Sum(c => c.RatingCounts.Performed),
                        Satisfactory = g.Sum(c => c.RatingCounts.Satisfactory),
                        Marginal = g.Sum(c => c.RatingCounts.Marginal),
                        Unsatisfactory = g.Sum(c => c.RatingCounts.Unsatisfactory),
                        Unrated = g.Sum(c => c.RatingCounts.Unrated)
                    }
                };
            })
            .OrderBy(m => m.MissionArea)
            .ToList();

        return new CapabilityPerformanceSummaryDto
        {
            CapabilitiesEvaluated = capabilities.Count,
            TargetCapabilitiesCount = targetCapabilityIds.Count,
            TargetCapabilitiesEvaluated = targetCapabilitiesEvaluated,
            TargetCoverageRate = targetCoverageRate,
            TotalTaggedObservations = totalTaggedObservations,
            TotalObservations = totalObservations,
            TaggingRate = taggingRate,
            Capabilities = capabilities,
            UnevaluatedTargets = unevaluatedTargets,
            ByMissionArea = byMissionArea
        };
    }
}
