using Cadence.Core.Features.Eeg.Models.DTOs;
using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.Exercises.Models.DTOs;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Observations.Models.DTOs;
using Cadence.Core.Features.Photos.Models.DTOs;
using Cadence.Core.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Cadence.WebApi.Hubs;

/// <summary>
/// Implementation of IExerciseHubContext for broadcasting events via SignalR.
/// </summary>
public class ExerciseHubContext : IExerciseHubContext
{
    private readonly IHubContext<ExerciseHub> _hubContext;
    private readonly ILogger<ExerciseHubContext> _logger;

    public ExerciseHubContext(IHubContext<ExerciseHub> hubContext, ILogger<ExerciseHubContext> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    private string GetGroupName(Guid exerciseId) => $"exercise-{exerciseId}";

    /// <inheritdoc />
    public async Task NotifyInjectFired(Guid exerciseId, InjectDto inject)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("InjectFired", inject),
            group.SendAsync("InjectStatusChanged", inject)
        );

        _logger.LogDebug(
            "Broadcast InjectFired for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectSkipped(Guid exerciseId, InjectDto inject)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("InjectSkipped", inject),
            group.SendAsync("InjectStatusChanged", inject)
        );

        _logger.LogDebug(
            "Broadcast InjectSkipped for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectReset(Guid exerciseId, InjectDto inject)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("InjectReset", inject),
            group.SendAsync("InjectStatusChanged", inject)
        );

        _logger.LogDebug(
            "Broadcast InjectReset for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectReadyToFire(Guid exerciseId, InjectDto inject)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("InjectReadyToFire", inject),
            group.SendAsync("InjectStatusChanged", inject)
        );

        _logger.LogDebug(
            "Broadcast InjectReadyToFire for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectStatusChanged(Guid exerciseId, InjectDto inject)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("InjectStatusChanged", inject);

        _logger.LogDebug(
            "Broadcast InjectStatusChanged for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectSubmitted(Guid exerciseId, InjectDto inject)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("InjectSubmitted", inject),
            group.SendAsync("InjectStatusChanged", inject)
        );

        _logger.LogDebug(
            "Broadcast InjectSubmitted for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectApproved(Guid exerciseId, InjectDto inject)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("InjectApproved", inject),
            group.SendAsync("InjectStatusChanged", inject)
        );

        _logger.LogDebug(
            "Broadcast InjectApproved for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectRejected(Guid exerciseId, InjectDto inject)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("InjectRejected", inject),
            group.SendAsync("InjectStatusChanged", inject)
        );

        _logger.LogDebug(
            "Broadcast InjectRejected for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectReverted(Guid exerciseId, InjectDto inject)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("InjectReverted", inject),
            group.SendAsync("InjectStatusChanged", inject)
        );

        _logger.LogDebug(
            "Broadcast InjectReverted for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyClockStarted(Guid exerciseId, ClockStateDto clockState)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("ClockStarted", clockState),
            group.SendAsync("ClockChanged", clockState)
        );

        _logger.LogDebug("Broadcast ClockStarted to exercise {ExerciseId}", exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyClockPaused(Guid exerciseId, ClockStateDto clockState)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events
        await Task.WhenAll(
            group.SendAsync("ClockPaused", clockState),
            group.SendAsync("ClockChanged", clockState)
        );

        _logger.LogDebug("Broadcast ClockPaused to exercise {ExerciseId}", exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyClockStopped(Guid exerciseId, ClockStateDto clockState)
    {
        var group = _hubContext.Clients.Group(GetGroupName(exerciseId));

        // Send both specific and generic events (ClockReset for reset operations)
        await Task.WhenAll(
            group.SendAsync("ClockStopped", clockState),
            group.SendAsync("ClockReset", clockState),
            group.SendAsync("ClockChanged", clockState)
        );

        _logger.LogDebug("Broadcast ClockStopped to exercise {ExerciseId}", exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyObservationAdded(Guid exerciseId, ObservationDto observation)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("ObservationAdded", observation);

        _logger.LogDebug(
            "Broadcast ObservationAdded for observation {ObservationId} to exercise {ExerciseId}",
            observation.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyObservationUpdated(Guid exerciseId, ObservationDto observation)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("ObservationUpdated", observation);

        _logger.LogDebug(
            "Broadcast ObservationUpdated for observation {ObservationId} to exercise {ExerciseId}",
            observation.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyObservationDeleted(Guid exerciseId, Guid observationId)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("ObservationDeleted", observationId);

        _logger.LogDebug(
            "Broadcast ObservationDeleted for observation {ObservationId} to exercise {ExerciseId}",
            observationId, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyExerciseStatusChanged(Guid exerciseId, ExerciseDto exercise)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("ExerciseStatusChanged", exercise);

        _logger.LogDebug(
            "Broadcast ExerciseStatusChanged (status: {Status}) to exercise {ExerciseId}",
            exercise.Status, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectsReordered(Guid exerciseId, List<Guid> injectIds)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("InjectsReordered", injectIds);

        _logger.LogDebug(
            "Broadcast InjectsReordered ({Count} injects) to exercise {ExerciseId}",
            injectIds.Count, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyEegEntryCreated(Guid exerciseId, EegEntryDto entry)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("EegEntryCreated", entry);

        _logger.LogDebug(
            "Broadcast EegEntryCreated for entry {EntryId} to exercise {ExerciseId}",
            entry.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyEegEntryUpdated(Guid exerciseId, EegEntryDto entry)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("EegEntryUpdated", entry);

        _logger.LogDebug(
            "Broadcast EegEntryUpdated for entry {EntryId} to exercise {ExerciseId}",
            entry.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyEegEntryDeleted(Guid exerciseId, Guid entryId)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("EegEntryDeleted", entryId);

        _logger.LogDebug(
            "Broadcast EegEntryDeleted for entry {EntryId} to exercise {ExerciseId}",
            entryId, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyPhotoAdded(Guid exerciseId, PhotoDto photo)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("PhotoAdded", photo);

        _logger.LogDebug(
            "Broadcast PhotoAdded for photo {PhotoId} to exercise {ExerciseId}",
            photo.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyPhotoUpdated(Guid exerciseId, PhotoDto photo)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("PhotoUpdated", photo);

        _logger.LogDebug(
            "Broadcast PhotoUpdated for photo {PhotoId} to exercise {ExerciseId}",
            photo.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyPhotoDeleted(Guid exerciseId, Guid photoId)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("PhotoDeleted", photoId);

        _logger.LogDebug(
            "Broadcast PhotoDeleted for photo {PhotoId} to exercise {ExerciseId}",
            photoId, exerciseId);
    }
}
