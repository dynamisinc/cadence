using Microsoft.AspNetCore.SignalR;

namespace Cadence.WebApi.Hubs;

/// <summary>
/// SignalR hub for exercise-related real-time communications.
/// Clients join exercise-specific groups to receive updates.
/// Also supports user-specific groups for notifications.
/// </summary>
public class ExerciseHub : Hub
{
    private readonly ILogger<ExerciseHub> _logger;

    public ExerciseHub(ILogger<ExerciseHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join an exercise group to receive updates for that exercise.
    /// </summary>
    public async Task JoinExercise(string exerciseId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"exercise-{exerciseId}");
        _logger.LogInformation(
            "Client {ConnectionId} joined exercise group {ExerciseId}",
            Context.ConnectionId, exerciseId);
    }

    /// <summary>
    /// Leave an exercise group.
    /// </summary>
    public async Task LeaveExercise(string exerciseId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"exercise-{exerciseId}");
        _logger.LogInformation(
            "Client {ConnectionId} left exercise group {ExerciseId}",
            Context.ConnectionId, exerciseId);
    }

    /// <summary>
    /// Join a user-specific group for notifications.
    /// Called automatically when user authenticates.
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        _logger.LogInformation(
            "Client {ConnectionId} joined user group {UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    /// Leave a user-specific group.
    /// </summary>
    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        _logger.LogInformation(
            "Client {ConnectionId} left user group {UserId}",
            Context.ConnectionId, userId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client disconnected: {ConnectionId}. Exception: {Exception}",
            Context.ConnectionId, exception?.Message ?? "None");
        await base.OnDisconnectedAsync(exception);
    }
}
