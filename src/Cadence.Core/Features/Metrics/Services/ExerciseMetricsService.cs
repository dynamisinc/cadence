using Cadence.Core.Features.Metrics.Models.DTOs;

namespace Cadence.Core.Features.Metrics.Services;

/// <summary>
/// Facade service for exercise metrics calculations.
/// Delegates to specialized services for different metric domains.
/// </summary>
public class ExerciseMetricsService : IExerciseMetricsService
{
    private readonly IProgressMetricsService _progressService;
    private readonly IInjectMetricsService _injectService;
    private readonly IObservationMetricsService _observationService;
    private readonly ITimelineMetricsService _timelineService;

    /// <summary>
    /// Constructor with explicit service dependencies (preferred for DI).
    /// </summary>
    public ExerciseMetricsService(
        IProgressMetricsService progressService,
        IInjectMetricsService injectService,
        IObservationMetricsService observationService,
        ITimelineMetricsService timelineService)
    {
        _progressService = progressService;
        _injectService = injectService;
        _observationService = observationService;
        _timelineService = timelineService;
    }

    /// <inheritdoc />
    public Task<ExerciseProgressDto?> GetExerciseProgressAsync(Guid exerciseId)
        => _progressService.GetExerciseProgressAsync(exerciseId);

    /// <inheritdoc />
    public Task<InjectSummaryDto?> GetInjectSummaryAsync(Guid exerciseId, int onTimeToleranceMinutes = 5)
        => _injectService.GetInjectSummaryAsync(exerciseId, onTimeToleranceMinutes);

    /// <inheritdoc />
    public Task<ObservationSummaryDto?> GetObservationSummaryAsync(Guid exerciseId)
        => _observationService.GetObservationSummaryAsync(exerciseId);

    /// <inheritdoc />
    public Task<TimelineSummaryDto?> GetTimelineSummaryAsync(Guid exerciseId)
        => _timelineService.GetTimelineSummaryAsync(exerciseId);

    /// <inheritdoc />
    public Task<ControllerActivitySummaryDto?> GetControllerActivityAsync(Guid exerciseId, int onTimeToleranceMinutes = 5)
        => _injectService.GetControllerActivityAsync(exerciseId, onTimeToleranceMinutes);

    /// <inheritdoc />
    public Task<EvaluatorCoverageSummaryDto?> GetEvaluatorCoverageAsync(Guid exerciseId)
        => _observationService.GetEvaluatorCoverageAsync(exerciseId);

    /// <inheritdoc />
    public Task<CapabilityPerformanceSummaryDto?> GetCapabilityPerformanceAsync(Guid exerciseId)
        => _observationService.GetCapabilityPerformanceAsync(exerciseId);
}
