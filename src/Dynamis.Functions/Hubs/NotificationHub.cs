using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DynamisReferenceApp.Api.Hubs;

/// <summary>
/// SignalR hub functions for real-time notifications.
/// </summary>
public class NotificationHub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Negotiates a SignalR connection for a client.
    /// Clients call this endpoint to get connection information.
    /// </summary>
    [Function("Negotiate")]
    public SignalRConnectionInfo Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate")] HttpRequest req,
        [SignalRConnectionInfoInput(HubName = "notifications", UserId = "{headers.x-user-id}")] SignalRConnectionInfo connectionInfo)
    {
        _logger.LogInformation("SignalR negotiate requested");
        return connectionInfo;
    }

    /// <summary>
    /// Broadcasts a message to all connected clients.
    /// This is an example of how to send messages from server to clients.
    /// </summary>
    [Function("Broadcast")]
    [SignalROutput(HubName = "notifications")]
    public SignalRMessageAction Broadcast(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "broadcast")] HttpRequest req)
    {
        _logger.LogInformation("Broadcasting message to all clients");

        return new SignalRMessageAction("newMessage")
        {
            Arguments = new object[] { new { message = "Hello from server!", timestamp = DateTime.UtcNow } }
        };
    }

    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    [Function("SendToUser")]
    [SignalROutput(HubName = "notifications")]
    public SignalRMessageAction? SendToUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "notify/{userId}")] HttpRequest req,
        string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("SendToUser called without userId");
            return null;
        }

        _logger.LogInformation("Sending notification to user {UserId}", userId);

        return new SignalRMessageAction("notification")
        {
            UserId = userId,
            Arguments = new object[] { new { message = "You have a notification!", timestamp = DateTime.UtcNow } }
        };
    }
}

/// <summary>
/// Extension methods for broadcasting SignalR messages from services.
/// </summary>
public static class SignalRBroadcastExtensions
{
    /// <summary>
    /// Creates a SignalR message for note created event.
    /// </summary>
    public static SignalRMessageAction NoteCreated(Guid noteId, string userId)
    {
        return new SignalRMessageAction("noteCreated")
        {
            Arguments = new object[] { new { noteId, userId, timestamp = DateTime.UtcNow } }
        };
    }

    /// <summary>
    /// Creates a SignalR message for note updated event.
    /// </summary>
    public static SignalRMessageAction NoteUpdated(Guid noteId, string userId)
    {
        return new SignalRMessageAction("noteUpdated")
        {
            Arguments = new object[] { new { noteId, userId, timestamp = DateTime.UtcNow } }
        };
    }

    /// <summary>
    /// Creates a SignalR message for note deleted event.
    /// </summary>
    public static SignalRMessageAction NoteDeleted(Guid noteId, string userId)
    {
        return new SignalRMessageAction("noteDeleted")
        {
            Arguments = new object[] { new { noteId, userId, timestamp = DateTime.UtcNow } }
        };
    }
}
