using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Observations.Models.DTOs;

namespace Cadence.Core.Hubs;

/// <summary>
/// Interface for broadcasting exercise-related events via SignalR.
/// Implemented in WebApi project - this interface lives in Core to avoid
/// SignalR dependency in the Core project.
/// </summary>
public interface IExerciseHubContext
{
    /// <summary>
    /// Notify clients that an inject was fired.
    /// </summary>
    Task NotifyInjectFired(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that an inject was skipped.
    /// </summary>
    Task NotifyInjectSkipped(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that an inject was reset to pending.
    /// </summary>
    Task NotifyInjectReset(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that the exercise clock was started.
    /// </summary>
    Task NotifyClockStarted(Guid exerciseId, ClockStateDto clockState);

    /// <summary>
    /// Notify clients that the exercise clock was paused.
    /// </summary>
    Task NotifyClockPaused(Guid exerciseId, ClockStateDto clockState);

    /// <summary>
    /// Notify clients that the exercise clock was stopped.
    /// </summary>
    Task NotifyClockStopped(Guid exerciseId, ClockStateDto clockState);

    /// <summary>
    /// Notify clients that a new observation was added.
    /// </summary>
    Task NotifyObservationAdded(Guid exerciseId, ObservationDto observation);

    /// <summary>
    /// Notify clients that an observation was updated.
    /// </summary>
    Task NotifyObservationUpdated(Guid exerciseId, ObservationDto observation);

    /// <summary>
    /// Notify clients that an observation was deleted.
    /// </summary>
    Task NotifyObservationDeleted(Guid exerciseId, Guid observationId);
}
