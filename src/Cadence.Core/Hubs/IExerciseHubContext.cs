using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.Exercises.Models.DTOs;
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
    /// Also broadcasts InjectStatusChanged for generic status listeners.
    /// </summary>
    Task NotifyInjectFired(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that an inject was skipped.
    /// Also broadcasts InjectStatusChanged for generic status listeners.
    /// </summary>
    Task NotifyInjectSkipped(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that an inject was reset to pending.
    /// Also broadcasts InjectStatusChanged for generic status listeners.
    /// </summary>
    Task NotifyInjectReset(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that an inject has transitioned to Ready status.
    /// Used in clock-driven mode when an inject's delivery time has been reached.
    /// </summary>
    Task NotifyInjectReadyToFire(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients of a generic inject status change.
    /// Used when the specific event type (Fired/Skipped/Reset) is not relevant.
    /// </summary>
    Task NotifyInjectStatusChanged(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that an inject was submitted for approval.
    /// Also broadcasts InjectStatusChanged for generic status listeners.
    /// </summary>
    Task NotifyInjectSubmitted(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that an inject was approved.
    /// Also broadcasts InjectStatusChanged for generic status listeners.
    /// </summary>
    Task NotifyInjectApproved(Guid exerciseId, InjectDto inject);

    /// <summary>
    /// Notify clients that an inject was rejected and returned to Draft.
    /// Also broadcasts InjectStatusChanged for generic status listeners.
    /// </summary>
    Task NotifyInjectRejected(Guid exerciseId, InjectDto inject);

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

    /// <summary>
    /// Notify clients that the exercise status has changed.
    /// Used for status workflow transitions (Draft → Active → Paused → Completed → Archived).
    /// </summary>
    Task NotifyExerciseStatusChanged(Guid exerciseId, ExerciseDto exercise);

    /// <summary>
    /// Notify clients that injects have been reordered.
    /// </summary>
    Task NotifyInjectsReordered(Guid exerciseId, List<Guid> injectIds);
}
