using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service for calculating inject delivery and controller activity metrics.
/// </summary>
public class InjectMetricsService : IInjectMetricsService
{
    private readonly AppDbContext _context;

    public InjectMetricsService(AppDbContext context)
    {
        _context = context;
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
        var firedCount = injects.Count(i => i.Status == InjectStatus.Released);
        var skippedCount = injects.Count(i => i.Status == InjectStatus.Deferred);
        var notExecutedCount = injects.Count(i => i.Status == InjectStatus.Draft || i.Status == InjectStatus.Synchronized);

        // Calculate percentages
        var firedPercentage = totalCount > 0 ? Math.Round((decimal)firedCount / totalCount * 100, 1) : 0;
        var skippedPercentage = totalCount > 0 ? Math.Round((decimal)skippedCount / totalCount * 100, 1) : 0;
        var notExecutedPercentage = totalCount > 0 ? Math.Round((decimal)notExecutedCount / totalCount * 100, 1) : 0;

        // Calculate timing metrics for fired injects
        var firedInjects = injects.Where(i => i.Status == InjectStatus.Released && i.FiredAt.HasValue).ToList();
        var timingMetrics = CalculateTimingMetrics(firedInjects, exercise.ActivatedAt, onTimeToleranceMinutes);

        // Group by phase
        var byPhase = injects
            .GroupBy(i => new { i.PhaseId, PhaseName = i.Phase?.Name ?? "No Phase", Sequence = i.Phase?.Sequence ?? 999 })
            .OrderBy(g => g.Key.Sequence)
            .Select(g =>
            {
                var phaseInjects = g.ToList();
                var phaseFired = phaseInjects.Where(i => i.Status == InjectStatus.Released && i.FiredAt.HasValue).ToList();
                return new PhaseInjectSummaryDto
                {
                    PhaseId = g.Key.PhaseId,
                    PhaseName = g.Key.PhaseName,
                    Sequence = g.Key.Sequence,
                    TotalCount = phaseInjects.Count,
                    FiredCount = phaseInjects.Count(i => i.Status == InjectStatus.Released),
                    SkippedCount = phaseInjects.Count(i => i.Status == InjectStatus.Deferred),
                    NotExecutedCount = phaseInjects.Count(i => i.Status == InjectStatus.Draft || i.Status == InjectStatus.Synchronized),
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
            .Where(i => i.Status == InjectStatus.Deferred)
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

        var firedInjects = injects.Where(i => i.Status == InjectStatus.Released).ToList();
        var skippedInjects = injects.Where(i => i.Status == InjectStatus.Deferred).ToList();

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

                // Calculate on-time stats using exercise-relative time (DeliveryTime)
                var onTimeCount = 0;
                var variances = new List<TimeSpan>();

                foreach (var inject in c.FiredInjects)
                {
                    if (inject.FiredAt.HasValue && inject.DeliveryTime.HasValue && exercise.ActivatedAt.HasValue)
                    {
                        var expectedTime = exercise.ActivatedAt.Value + inject.DeliveryTime.Value;
                        var variance = inject.FiredAt.Value - expectedTime;
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
                        InjectsFired = g.Count(i => i.Status == InjectStatus.Released),
                        InjectsSkipped = g.Count(i => i.Status == InjectStatus.Deferred)
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

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

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
}
