using Cadence.Core.Features.Notifications.Services;
using Cadence.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadence.WebApi.Controllers;

/// <summary>
/// Controller for managing user notifications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Get notifications for the current user.
    /// </summary>
    /// <param name="limit">Maximum notifications to return (default: 10)</param>
    /// <param name="offset">Number of notifications to skip (default: 0)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Notifications with pagination info</returns>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var userId = User.TryGetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Clamp limit to reasonable bounds
        limit = Math.Clamp(limit, 1, 100);
        offset = Math.Max(0, offset);

        var notifications = await _notificationService.GetNotificationsAsync(userId, limit, offset, ct);
        return Ok(notifications);
    }

    /// <summary>
    /// Get unread notification count for the current user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Unread count</returns>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.GetUnreadCountAsync(userId, ct);
        return Ok(new { unreadCount = count });
    }

    /// <summary>
    /// Mark a notification as read.
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success or 404 if not found</returns>
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.MarkAsReadAsync(userId, id, ct);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Mark all notifications as read for the current user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of notifications marked as read</returns>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = User.TryGetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var count = await _notificationService.MarkAllAsReadAsync(userId, ct);
        return Ok(new { markedCount = count });
    }
}
