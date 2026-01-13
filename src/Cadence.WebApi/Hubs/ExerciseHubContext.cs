using Cadence.Core.Features.ExerciseClock.Models.DTOs;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Observations.Models.DTOs;
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
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("InjectFired", inject);

        _logger.LogDebug(
            "Broadcast InjectFired for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectSkipped(Guid exerciseId, InjectDto inject)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("InjectSkipped", inject);

        _logger.LogDebug(
            "Broadcast InjectSkipped for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyInjectReset(Guid exerciseId, InjectDto inject)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("InjectReset", inject);

        _logger.LogDebug(
            "Broadcast InjectReset for inject {InjectId} to exercise {ExerciseId}",
            inject.Id, exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyClockStarted(Guid exerciseId, ClockStateDto clockState)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("ClockStarted", clockState);

        _logger.LogDebug("Broadcast ClockStarted to exercise {ExerciseId}", exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyClockPaused(Guid exerciseId, ClockStateDto clockState)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("ClockPaused", clockState);

        _logger.LogDebug("Broadcast ClockPaused to exercise {ExerciseId}", exerciseId);
    }

    /// <inheritdoc />
    public async Task NotifyClockStopped(Guid exerciseId, ClockStateDto clockState)
    {
        await _hubContext.Clients
            .Group(GetGroupName(exerciseId))
            .SendAsync("ClockStopped", clockState);

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
}
