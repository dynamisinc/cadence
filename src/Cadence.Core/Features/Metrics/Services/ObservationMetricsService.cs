using Cadence.Core.Data;
using Cadence.Core.Features.Metrics.Models.DTOs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Service for calculating observation, evaluator, and capability metrics.
/// </summary>
public class ObservationMetricsService : IObservationMetricsService
{
    private readonly AppDbContext _context;

    public ObservationMetricsService(AppDbContext context)
    {
        _context = context;
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
            .SelectMany(o => o.ObservationCapabilities.Select(oc => oc.CapabilityId))
            .Distinct()
            .ToHashSet();

        // Get total active capabilities for coverage calculation
        var totalCapabilities = await _context.Capabilities
            .AsNoTracking()
            .CountAsync(c => c.IsActive);

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
                    .SelectMany(o => o.ObservationCapabilities.Select(oc => oc.CapabilityId))
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
            TotalCapabilities = totalCapabilities,
            Consistency = consistency,
            Evaluators = evaluators,
            CoverageMatrix = coverageMatrix,
            UncoveredObjectives = uncoveredObjectives,
            LowCoverageObjectives = lowCoverageObjectives
        };
    }

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
                .ThenInclude(oc => oc.Capability)
            .ToListAsync();

        // Get target capabilities for this exercise
        var targetCapabilityIds = await _context.ExerciseTargetCapabilities
            .AsNoTracking()
            .Where(etc => etc.ExerciseId == exerciseId)
            .Select(etc => etc.CapabilityId)
            .ToListAsync();

        // Get all core capabilities for reference
        var allCapabilities = await _context.Capabilities
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
            .SelectMany(o => o.ObservationCapabilities.Select(oc => new { Observation = o, Capability = oc.Capability }))
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
                    Category = capability.Category ?? "Uncategorized",
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
                Category = c.Category ?? "Uncategorized"
            })
            .ToList();

        // Group by category
        var byCategory = capabilities
            .GroupBy(c => c.Category)
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

                return new CategorySummaryDto
                {
                    Category = g.Key,
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
            .OrderBy(m => m.Category)
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
            ByCategory = byCategory
        };
    }

    // =========================================================================
    // Private Helper Methods
    // =========================================================================

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
}
