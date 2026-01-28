namespace Cadence.Core.Features.Metrics.Models.DTOs;

// =============================================================================
// S01: Exercise Progress Dashboard DTOs (Real-time conduct metrics)
// =============================================================================

/// <summary>
/// Real-time exercise progress data for conduct view.
/// Used by Controllers/Directors to monitor exercise status at a glance.
/// </summary>
public record ExerciseProgressDto
{
    /// <summary>Total number of injects in the active MSEL.</summary>
    public int TotalInjects { get; init; }

    /// <summary>Number of injects that have been fired.</summary>
    public int FiredCount { get; init; }

    /// <summary>Number of injects that have been skipped.</summary>
    public int SkippedCount { get; init; }

    /// <summary>Number of injects still pending (including Ready).</summary>
    public int PendingCount { get; init; }

    /// <summary>Number of injects in Ready status (ready to fire).</summary>
    public int ReadyCount { get; init; }

    /// <summary>Progress percentage: (fired + skipped) / total * 100.</summary>
    public decimal ProgressPercentage { get; init; }

    /// <summary>Total number of observations recorded.</summary>
    public int ObservationCount { get; init; }

    /// <summary>Current exercise clock elapsed time.</summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>Current clock status (Stopped, Running, Paused).</summary>
    public string ClockStatus { get; init; } = string.Empty;

    /// <summary>Current phase name (based on most recently fired inject or first pending).</summary>
    public string? CurrentPhaseName { get; init; }

    /// <summary>Next 3 upcoming injects (pending, ordered by sequence).</summary>
    public List<UpcomingInjectDto> NextInjects { get; init; } = new();

    /// <summary>P/S/M/U rating counts for quick observation summary.</summary>
    public RatingCountsDto RatingCounts { get; init; } = new();
}

/// <summary>
/// Upcoming inject information for progress display.
/// </summary>
public record UpcomingInjectDto
{
    public Guid Id { get; init; }
    public int InjectNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public TimeOnly ScheduledTime { get; init; }
    public TimeSpan? DeliveryTime { get; init; }
    public string? PhaseName { get; init; }
    public string Status { get; init; } = string.Empty;
}

/// <summary>
/// Quick P/S/M/U counts for progress dashboard.
/// </summary>
public record RatingCountsDto
{
    public int Performed { get; init; }
    public int Satisfactory { get; init; }
    public int Marginal { get; init; }
    public int Unsatisfactory { get; init; }
    public int Unrated { get; init; }
}

// =============================================================================
// S02: Exercise Inject Summary DTOs (AAR metrics)
// =============================================================================

/// <summary>
/// Comprehensive inject delivery statistics for after-action review.
/// </summary>
public record InjectSummaryDto
{
    /// <summary>Total inject count in the MSEL.</summary>
    public int TotalCount { get; init; }

    /// <summary>Number of injects that were fired.</summary>
    public int FiredCount { get; init; }

    /// <summary>Number of injects that were skipped.</summary>
    public int SkippedCount { get; init; }

    /// <summary>Number of injects not executed (still pending when exercise ended).</summary>
    public int NotExecutedCount { get; init; }

    /// <summary>Percentage of injects that were fired.</summary>
    public decimal FiredPercentage { get; init; }

    /// <summary>Percentage of injects that were skipped.</summary>
    public decimal SkippedPercentage { get; init; }

    /// <summary>Percentage of injects not executed.</summary>
    public decimal NotExecutedPercentage { get; init; }

    /// <summary>
    /// On-time rate: percentage of fired injects delivered within tolerance (±5 min default).
    /// Null if no timing data available.
    /// </summary>
    public decimal? OnTimeRate { get; init; }

    /// <summary>Number of injects delivered on time.</summary>
    public int OnTimeCount { get; init; }

    /// <summary>Average timing variance from scheduled time (positive = late).</summary>
    public TimeSpan? AverageVariance { get; init; }

    /// <summary>Earliest variance (most early delivery).</summary>
    public TimingVarianceDto? EarliestVariance { get; init; }

    /// <summary>Latest variance (most late delivery).</summary>
    public TimingVarianceDto? LatestVariance { get; init; }

    /// <summary>Inject statistics broken down by phase.</summary>
    public List<PhaseInjectSummaryDto> ByPhase { get; init; } = new();

    /// <summary>Inject statistics broken down by controller who fired them.</summary>
    public List<ControllerInjectSummaryDto> ByController { get; init; } = new();

    /// <summary>List of skipped injects with reasons.</summary>
    public List<SkippedInjectDto> SkippedInjects { get; init; } = new();
}

/// <summary>
/// Timing variance information for a specific inject.
/// </summary>
public record TimingVarianceDto
{
    public Guid InjectId { get; init; }
    public int InjectNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public TimeSpan Variance { get; init; }
}

/// <summary>
/// Inject statistics for a specific phase.
/// </summary>
public record PhaseInjectSummaryDto
{
    public Guid? PhaseId { get; init; }
    public string PhaseName { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public int TotalCount { get; init; }
    public int FiredCount { get; init; }
    public int SkippedCount { get; init; }
    public int NotExecutedCount { get; init; }
    public decimal? OnTimeRate { get; init; }
}

/// <summary>
/// Inject statistics for a specific controller.
/// </summary>
public record ControllerInjectSummaryDto
{
    public Guid? ControllerId { get; init; }
    public string ControllerName { get; init; } = string.Empty;
    public int FiredCount { get; init; }
    public TimeSpan? AverageVariance { get; init; }
    public decimal? OnTimeRate { get; init; }
}

/// <summary>
/// Information about a skipped inject.
/// </summary>
public record SkippedInjectDto
{
    public Guid Id { get; init; }
    public int InjectNumber { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? PhaseName { get; init; }
    public string? SkipReason { get; init; }
    public DateTime? SkippedAt { get; init; }
    public string? SkippedByName { get; init; }
}

// =============================================================================
// S03: Exercise Observation Summary DTOs (AAR metrics)
// =============================================================================

/// <summary>
/// Comprehensive observation statistics for after-action review.
/// </summary>
public record ObservationSummaryDto
{
    /// <summary>Total number of observations recorded.</summary>
    public int TotalCount { get; init; }

    /// <summary>P/S/M/U rating distribution.</summary>
    public RatingDistributionDto RatingDistribution { get; init; } = new();

    /// <summary>
    /// Coverage rate: percentage of objectives with at least one observation.
    /// Null if no objectives defined.
    /// </summary>
    public decimal? CoverageRate { get; init; }

    /// <summary>Number of objectives that have at least one observation.</summary>
    public int ObjectivesCovered { get; init; }

    /// <summary>Total number of objectives defined for the exercise.</summary>
    public int TotalObjectives { get; init; }

    /// <summary>List of objectives without any observations (gaps).</summary>
    public List<UncoveredObjectiveDto> UncoveredObjectives { get; init; } = new();

    /// <summary>Observation statistics broken down by evaluator.</summary>
    public List<EvaluatorSummaryDto> ByEvaluator { get; init; } = new();

    /// <summary>Observation statistics broken down by phase.</summary>
    public List<PhaseObservationSummaryDto> ByPhase { get; init; } = new();

    /// <summary>Number of observations linked to an inject.</summary>
    public int LinkedToInjectCount { get; init; }

    /// <summary>Number of observations linked to an objective.</summary>
    public int LinkedToObjectiveCount { get; init; }

    /// <summary>Number of observations not linked to any inject or objective.</summary>
    public int UnlinkedCount { get; init; }
}

/// <summary>
/// P/S/M/U rating distribution with counts and percentages.
/// </summary>
public record RatingDistributionDto
{
    public int PerformedCount { get; init; }
    public decimal PerformedPercentage { get; init; }

    public int SatisfactoryCount { get; init; }
    public decimal SatisfactoryPercentage { get; init; }

    public int MarginalCount { get; init; }
    public decimal MarginalPercentage { get; init; }

    public int UnsatisfactoryCount { get; init; }
    public decimal UnsatisfactoryPercentage { get; init; }

    public int UnratedCount { get; init; }
    public decimal UnratedPercentage { get; init; }

    /// <summary>
    /// Average rating as numeric value: P=1, S=2, M=3, U=4.
    /// Lower is better. Null if no rated observations.
    /// </summary>
    public decimal? AverageRating { get; init; }
}

/// <summary>
/// Objective without any observations (coverage gap).
/// </summary>
public record UncoveredObjectiveDto
{
    public Guid Id { get; init; }
    public string ObjectiveNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Observation statistics for a specific evaluator.
/// </summary>
public record EvaluatorSummaryDto
{
    public string? EvaluatorId { get; init; }
    public string EvaluatorName { get; init; } = string.Empty;
    public int ObservationCount { get; init; }

    /// <summary>Average rating: P=1, S=2, M=3, U=4. Null if no rated observations.</summary>
    public decimal? AverageRating { get; init; }

    /// <summary>Rating distribution for this evaluator.</summary>
    public RatingCountsDto RatingCounts { get; init; } = new();
}

/// <summary>
/// Observation statistics for a specific phase.
/// </summary>
public record PhaseObservationSummaryDto
{
    public Guid? PhaseId { get; init; }
    public string PhaseName { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public int ObservationCount { get; init; }
    public RatingCountsDto RatingCounts { get; init; } = new();
}

// =============================================================================
// S04: Exercise Timeline Summary DTOs (AAR metrics)
// =============================================================================

/// <summary>
/// Comprehensive timeline and duration analysis for after-action review.
/// </summary>
public record TimelineSummaryDto
{
    /// <summary>Planned exercise duration based on start/end times.</summary>
    public TimeSpan? PlannedDuration { get; init; }

    /// <summary>Actual exercise duration (total elapsed time on clock).</summary>
    public TimeSpan ActualDuration { get; init; }

    /// <summary>
    /// Variance from planned duration (positive = ran longer than planned).
    /// Null if no planned duration defined.
    /// </summary>
    public TimeSpan? DurationVariance { get; init; }

    /// <summary>When the exercise was first started (clock started).</summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>When the exercise was ended (clock stopped or exercise completed).</summary>
    public DateTime? EndedAt { get; init; }

    /// <summary>Total wall-clock time from first start to final stop.</summary>
    public TimeSpan? WallClockDuration { get; init; }

    /// <summary>Number of times the exercise was paused.</summary>
    public int PauseCount { get; init; }

    /// <summary>Total time spent paused.</summary>
    public TimeSpan TotalPauseTime { get; init; }

    /// <summary>Average pause duration.</summary>
    public TimeSpan? AveragePauseDuration { get; init; }

    /// <summary>Longest single pause duration.</summary>
    public TimeSpan? LongestPauseDuration { get; init; }

    /// <summary>Individual pause events with details.</summary>
    public List<PauseEventDto> PauseEvents { get; init; } = new();

    /// <summary>Timeline of all clock events (start, pause, stop).</summary>
    public List<ClockEventDto> ClockEvents { get; init; } = new();

    /// <summary>Phase timing analysis.</summary>
    public List<PhaseTimingDto> PhaseTimings { get; init; } = new();

    /// <summary>Inject pacing metrics.</summary>
    public InjectPacingDto InjectPacing { get; init; } = new();
}

/// <summary>
/// Details of a single pause event.
/// </summary>
public record PauseEventDto
{
    /// <summary>When the pause started.</summary>
    public DateTime PausedAt { get; init; }

    /// <summary>When the pause ended (clock resumed). Null if still paused.</summary>
    public DateTime? ResumedAt { get; init; }

    /// <summary>Duration of this pause.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Elapsed exercise time when pause started.</summary>
    public TimeSpan ElapsedAtPause { get; init; }

    /// <summary>User who initiated the pause.</summary>
    public string? PausedByName { get; init; }

    /// <summary>User who resumed the clock.</summary>
    public string? ResumedByName { get; init; }

    /// <summary>Reason for the pause (if provided).</summary>
    public string? Notes { get; init; }
}

/// <summary>
/// A single clock event in the timeline.
/// </summary>
public record ClockEventDto
{
    /// <summary>Type of event (Started, Paused, Stopped).</summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>When the event occurred.</summary>
    public DateTime OccurredAt { get; init; }

    /// <summary>Elapsed exercise time when event occurred.</summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>User who triggered the event.</summary>
    public string? UserName { get; init; }

    /// <summary>Notes associated with the event.</summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Timing analysis for a specific phase.
/// </summary>
public record PhaseTimingDto
{
    public Guid? PhaseId { get; init; }
    public string PhaseName { get; init; } = string.Empty;
    public int Sequence { get; init; }

    /// <summary>When the first inject in this phase was fired.</summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>When the last inject in this phase was fired.</summary>
    public DateTime? EndedAt { get; init; }

    /// <summary>Time spent in this phase (last - first inject fire time).</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Number of injects fired in this phase.</summary>
    public int InjectsFired { get; init; }

    /// <summary>Elapsed exercise time at first inject fire.</summary>
    public TimeSpan? ElapsedAtStart { get; init; }

    /// <summary>Elapsed exercise time at last inject fire.</summary>
    public TimeSpan? ElapsedAtEnd { get; init; }
}

/// <summary>
/// Inject pacing analysis for the exercise.
/// </summary>
public record InjectPacingDto
{
    /// <summary>Total number of injects fired.</summary>
    public int TotalFired { get; init; }

    /// <summary>Average time between inject fires.</summary>
    public TimeSpan? AverageTimeBetweenInjects { get; init; }

    /// <summary>Shortest gap between consecutive inject fires.</summary>
    public TimeSpan? ShortestGap { get; init; }

    /// <summary>Longest gap between consecutive inject fires.</summary>
    public TimeSpan? LongestGap { get; init; }

    /// <summary>Average inject firing rate (injects per hour).</summary>
    public decimal? InjectsPerHour { get; init; }

    /// <summary>Busiest period analysis.</summary>
    public BusiestPeriodDto? BusiestPeriod { get; init; }
}

/// <summary>
/// Information about the busiest period of inject activity.
/// </summary>
public record BusiestPeriodDto
{
    /// <summary>Start of the busiest 15-minute window.</summary>
    public DateTime StartedAt { get; init; }

    /// <summary>End of the busiest period.</summary>
    public DateTime EndedAt { get; init; }

    /// <summary>Number of injects fired in this period.</summary>
    public int InjectCount { get; init; }
}

// =============================================================================
// S07: Controller Activity Metrics DTOs (AAR metrics)
// =============================================================================

/// <summary>
/// Comprehensive controller activity metrics for after-action review.
/// Shows workload distribution and performance by controller.
/// </summary>
public record ControllerActivitySummaryDto
{
    /// <summary>Total number of controllers who fired injects.</summary>
    public int TotalControllers { get; init; }

    /// <summary>Total injects fired across all controllers.</summary>
    public int TotalInjectsFired { get; init; }

    /// <summary>Total injects skipped across all controllers.</summary>
    public int TotalInjectsSkipped { get; init; }

    /// <summary>Overall on-time rate across all controllers.</summary>
    public decimal? OverallOnTimeRate { get; init; }

    /// <summary>Detailed activity for each controller.</summary>
    public List<ControllerActivityDto> Controllers { get; init; } = new();
}

/// <summary>
/// Detailed activity metrics for a single controller.
/// </summary>
public record ControllerActivityDto
{
    /// <summary>Controller's user ID (Guid as string for API consistency).</summary>
    public string? ControllerId { get; init; }

    /// <summary>Controller's display name.</summary>
    public string ControllerName { get; init; } = string.Empty;

    /// <summary>Number of injects this controller fired.</summary>
    public int InjectsFired { get; init; }

    /// <summary>Number of injects this controller skipped.</summary>
    public int InjectsSkipped { get; init; }

    /// <summary>Percentage of total fired injects handled by this controller.</summary>
    public decimal WorkloadPercentage { get; init; }

    /// <summary>On-time rate for this controller's fired injects.</summary>
    public decimal? OnTimeRate { get; init; }

    /// <summary>Number of on-time inject fires.</summary>
    public int OnTimeCount { get; init; }

    /// <summary>Average timing variance for this controller (positive = late).</summary>
    public TimeSpan? AverageVariance { get; init; }

    /// <summary>Phases where this controller was active.</summary>
    public List<ControllerPhaseActivityDto> PhaseActivity { get; init; } = new();

    /// <summary>First inject fired timestamp.</summary>
    public DateTime? FirstFireAt { get; init; }

    /// <summary>Last inject fired timestamp.</summary>
    public DateTime? LastFireAt { get; init; }
}

/// <summary>
/// Controller activity within a specific phase.
/// </summary>
public record ControllerPhaseActivityDto
{
    public Guid? PhaseId { get; init; }
    public string PhaseName { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public int InjectsFired { get; init; }
    public int InjectsSkipped { get; init; }
}

// =============================================================================
// S08: Evaluator Coverage Metrics DTOs (AAR metrics)
// =============================================================================

/// <summary>
/// Comprehensive evaluator coverage metrics for after-action review.
/// Shows observation distribution and coverage by evaluator.
/// </summary>
public record EvaluatorCoverageSummaryDto
{
    /// <summary>Total number of evaluators who recorded observations.</summary>
    public int TotalEvaluators { get; init; }

    /// <summary>Total observations recorded.</summary>
    public int TotalObservations { get; init; }

    /// <summary>Number of objectives covered by at least one observation.</summary>
    public int ObjectivesCovered { get; init; }

    /// <summary>Total number of objectives defined.</summary>
    public int TotalObjectives { get; init; }

    /// <summary>Overall objective coverage rate.</summary>
    public decimal? ObjectiveCoverageRate { get; init; }

    /// <summary>Number of capabilities covered by at least one observation.</summary>
    public int CapabilitiesCovered { get; init; }

    /// <summary>Total number of capabilities evaluated in this exercise.</summary>
    public int TotalCapabilities { get; init; }

    /// <summary>Evaluator consistency indicator: Low variance = High consistency.</summary>
    public EvaluatorConsistencyDto? Consistency { get; init; }

    /// <summary>Detailed coverage for each evaluator.</summary>
    public List<EvaluatorCoverageDto> Evaluators { get; init; } = new();

    /// <summary>Coverage matrix: objectives × evaluators.</summary>
    public List<ObjectiveCoverageRowDto> CoverageMatrix { get; init; } = new();

    /// <summary>Objectives with no observations (gaps).</summary>
    public List<UncoveredObjectiveDto> UncoveredObjectives { get; init; } = new();

    /// <summary>Objectives with low coverage (1-2 observations).</summary>
    public List<LowCoverageObjectiveDto> LowCoverageObjectives { get; init; } = new();
}

/// <summary>
/// Detailed coverage metrics for a single evaluator.
/// </summary>
public record EvaluatorCoverageDto
{
    /// <summary>Evaluator's user ID.</summary>
    public string? EvaluatorId { get; init; }

    /// <summary>Evaluator's display name.</summary>
    public string EvaluatorName { get; init; } = string.Empty;

    /// <summary>Total observations recorded by this evaluator.</summary>
    public int ObservationCount { get; init; }

    /// <summary>Number of distinct objectives this evaluator covered.</summary>
    public int ObjectivesCovered { get; init; }

    /// <summary>Number of distinct capabilities this evaluator covered.</summary>
    public int CapabilitiesCovered { get; init; }

    /// <summary>Average rating given by this evaluator (P=1, S=2, M=3, U=4).</summary>
    public decimal? AverageRating { get; init; }

    /// <summary>P/S/M/U rating distribution for this evaluator.</summary>
    public RatingCountsDto RatingCounts { get; init; } = new();

    /// <summary>Phases where this evaluator was active.</summary>
    public List<EvaluatorPhaseActivityDto> PhaseActivity { get; init; } = new();

    /// <summary>First observation timestamp.</summary>
    public DateTime? FirstObservationAt { get; init; }

    /// <summary>Last observation timestamp.</summary>
    public DateTime? LastObservationAt { get; init; }
}

/// <summary>
/// Evaluator activity within a specific phase.
/// </summary>
public record EvaluatorPhaseActivityDto
{
    public Guid? PhaseId { get; init; }
    public string PhaseName { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public int ObservationCount { get; init; }
}

/// <summary>
/// Coverage matrix row: one objective's coverage by all evaluators.
/// </summary>
public record ObjectiveCoverageRowDto
{
    public Guid ObjectiveId { get; init; }
    public string ObjectiveNumber { get; init; } = string.Empty;
    public string ObjectiveName { get; init; } = string.Empty;

    /// <summary>Total observations for this objective.</summary>
    public int TotalObservations { get; init; }

    /// <summary>Observations per evaluator: EvaluatorId → count.</summary>
    public Dictionary<string, int> ByEvaluator { get; init; } = new();

    /// <summary>Coverage status: Good, Low, None.</summary>
    public string CoverageStatus { get; init; } = string.Empty;
}

/// <summary>
/// Objective with low coverage (1-2 observations).
/// </summary>
public record LowCoverageObjectiveDto
{
    public Guid Id { get; init; }
    public string ObjectiveNumber { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int ObservationCount { get; init; }
}

/// <summary>
/// Evaluator consistency metrics based on rating variance.
/// High consistency means evaluators rate similarly when observing the same activities.
/// </summary>
public record EvaluatorConsistencyDto
{
    /// <summary>Overall consistency level: High, Moderate, Low, or Insufficient Data.</summary>
    public string Level { get; init; } = string.Empty;

    /// <summary>Overall average rating across all evaluators.</summary>
    public decimal OverallAverageRating { get; init; }

    /// <summary>Standard deviation of average ratings between evaluators.</summary>
    public decimal RatingStandardDeviation { get; init; }

    /// <summary>Evaluators with notably harsh ratings (avg > overall + 0.5).</summary>
    public List<EvaluatorRatingBiasDto> HarshRaters { get; init; } = new();

    /// <summary>Evaluators with notably lenient ratings (avg < overall - 0.5).</summary>
    public List<EvaluatorRatingBiasDto> LenientRaters { get; init; } = new();

    /// <summary>Description of consistency finding.</summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Indicates an evaluator's rating bias compared to overall average.
/// </summary>
public record EvaluatorRatingBiasDto
{
    public string EvaluatorName { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public decimal Deviation { get; init; }
}

// =============================================================================
// S06: Core Capability Performance DTOs (AAR metrics)
// =============================================================================

/// <summary>
/// Comprehensive capability performance metrics for after-action review.
/// Shows P/S/M/U ratings broken down by FEMA Core Capability.
/// </summary>
public record CapabilityPerformanceSummaryDto
{
    /// <summary>Total capabilities with observations.</summary>
    public int CapabilitiesEvaluated { get; init; }

    /// <summary>Target capabilities for this exercise (if any).</summary>
    public int TargetCapabilitiesCount { get; init; }

    /// <summary>Target capabilities that were actually evaluated.</summary>
    public int TargetCapabilitiesEvaluated { get; init; }

    /// <summary>Target capability coverage rate (null if no targets).</summary>
    public decimal? TargetCoverageRate { get; init; }

    /// <summary>Total observations with capability tags.</summary>
    public int TotalTaggedObservations { get; init; }

    /// <summary>Total observations (for comparison - shows tagging rate).</summary>
    public int TotalObservations { get; init; }

    /// <summary>Percentage of observations tagged with capabilities.</summary>
    public decimal TaggingRate { get; init; }

    /// <summary>Performance for each capability (sorted by rating, worst first).</summary>
    public List<CapabilityPerformanceDto> Capabilities { get; init; } = new();

    /// <summary>Target capabilities with no observations (gaps).</summary>
    public List<UnevaluatedCapabilityDto> UnevaluatedTargets { get; init; } = new();

    /// <summary>Performance grouped by mission area.</summary>
    public List<MissionAreaSummaryDto> ByMissionArea { get; init; } = new();
}

/// <summary>
/// Performance metrics for a single core capability.
/// </summary>
public record CapabilityPerformanceDto
{
    /// <summary>Capability ID.</summary>
    public Guid CapabilityId { get; init; }

    /// <summary>Capability name (e.g., "Mass Care Services").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>FEMA mission area (Prevention, Protection, Mitigation, Response, Recovery).</summary>
    public string MissionArea { get; init; } = string.Empty;

    /// <summary>Number of observations tagged with this capability.</summary>
    public int ObservationCount { get; init; }

    /// <summary>Average rating (P=1, S=2, M=3, U=4). Lower is better.</summary>
    public decimal? AverageRating { get; init; }

    /// <summary>Descriptive rating category based on average.</summary>
    public string RatingCategory { get; init; } = string.Empty;

    /// <summary>P/S/M/U rating distribution.</summary>
    public RatingCountsDto RatingCounts { get; init; } = new();

    /// <summary>Whether this is a target capability for this exercise.</summary>
    public bool IsTargetCapability { get; init; }

    /// <summary>Performance classification: Good, Satisfactory, Needs Improvement, Critical.</summary>
    public string PerformanceLevel { get; init; } = string.Empty;
}

/// <summary>
/// Target capability that was not evaluated.
/// </summary>
public record UnevaluatedCapabilityDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string MissionArea { get; init; } = string.Empty;
}

/// <summary>
/// Performance summary grouped by FEMA mission area.
/// </summary>
public record MissionAreaSummaryDto
{
    /// <summary>Mission area name.</summary>
    public string MissionArea { get; init; } = string.Empty;

    /// <summary>Number of capabilities evaluated in this area.</summary>
    public int CapabilitiesEvaluated { get; init; }

    /// <summary>Total observations in this area.</summary>
    public int ObservationCount { get; init; }

    /// <summary>Average rating across all capabilities in this area.</summary>
    public decimal? AverageRating { get; init; }

    /// <summary>Rating distribution for this mission area.</summary>
    public RatingCountsDto RatingCounts { get; init; } = new();
}
