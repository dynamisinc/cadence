namespace Cadence.Core.Features.Injects.Services;

/// <summary>
/// Service for evaluating and transitioning injects to Ready status when their
/// delivery time is reached in clock-driven exercises.
/// </summary>
public interface IInjectReadinessService
{
    /// <summary>
    /// Evaluates all active, clock-driven exercises with running clocks and transitions
    /// pending injects to Ready when their delivery time has been reached.
    /// Called periodically by background timer service.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task EvaluateAllExercisesAsync(CancellationToken ct = default);

    /// <summary>
    /// Evaluates a specific exercise for ready injects and transitions them to Ready status.
    /// Called when clock starts or resumes to immediately check for past-due injects.
    /// </summary>
    /// <param name="exerciseId">ID of the exercise to evaluate</param>
    /// <param name="ct">Cancellation token</param>
    Task EvaluateExerciseAsync(Guid exerciseId, CancellationToken ct = default);
}
