using Cadence.Core.Data;
using Cadence.Core.Features.Notifications.Models.DTOs;
using Cadence.Core.Features.Notifications.Services;
using Cadence.Core.Models.Entities;
using Cadence.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cadence.Core.Tests.Features.Notifications;

/// <summary>
/// Tests for NotificationService.
/// </summary>
public class NotificationServiceTests
{
    private readonly AppDbContext _context;
    private readonly NotificationService _service;
    private readonly string _testUserId = "test-user-id";

    public NotificationServiceTests()
    {
        _context = TestDbContextFactory.Create();
        var logger = new Mock<ILogger<NotificationService>>();
        _service = new NotificationService(_context, logger.Object);
    }

    /// <summary>
    /// Helper to create a test notification.
    /// </summary>
    private async Task<Notification> CreateNotificationAsync(
        string userId,
        bool isRead = false,
        NotificationType type = NotificationType.InjectReady)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Priority = NotificationPriority.Medium,
            Title = "Test Notification",
            Message = "Test message",
            IsRead = isRead,
            ReadAt = isRead ? DateTime.UtcNow : null
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    [Fact]
    public async Task GetNotificationsAsync_NoNotifications_ReturnsEmptyList()
    {
        var result = await _service.GetNotificationsAsync(_testUserId);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.UnreadCount);
    }

    [Fact]
    public async Task GetNotificationsAsync_WithNotifications_ReturnsCorrectData()
    {
        await CreateNotificationAsync(_testUserId, isRead: false);
        await CreateNotificationAsync(_testUserId, isRead: true);

        var result = await _service.GetNotificationsAsync(_testUserId);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.UnreadCount);
    }

    [Fact]
    public async Task GetUnreadCountAsync_WithUnreadNotifications_ReturnsCorrectCount()
    {
        await CreateNotificationAsync(_testUserId, isRead: false);
        await CreateNotificationAsync(_testUserId, isRead: false);
        await CreateNotificationAsync(_testUserId, isRead: true);

        var count = await _service.GetUnreadCountAsync(_testUserId);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task MarkAsReadAsync_ExistingUnreadNotification_MarksAsRead()
    {
        var notification = await CreateNotificationAsync(_testUserId, isRead: false);

        var success = await _service.MarkAsReadAsync(_testUserId, notification.Id);

        Assert.True(success);
        var updated = await _context.Notifications.FindAsync(notification.Id);
        Assert.True(updated!.IsRead);
        Assert.NotNull(updated.ReadAt);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WithUnreadNotifications_MarksAllAsRead()
    {
        await CreateNotificationAsync(_testUserId, isRead: false);
        await CreateNotificationAsync(_testUserId, isRead: false);

        var count = await _service.MarkAllAsReadAsync(_testUserId);

        Assert.Equal(2, count);
        var unreadCount = await _service.GetUnreadCountAsync(_testUserId);
        Assert.Equal(0, unreadCount);
    }

    [Fact]
    public async Task CreateNotificationAsync_ValidRequest_CreatesNotification()
    {
        var request = new CreateNotificationRequest
        {
            UserId = _testUserId,
            Type = NotificationType.InjectReady,
            Priority = NotificationPriority.High,
            Title = "Inject Ready",
            Message = "Inject #1 is ready to fire"
        };

        var result = await _service.CreateNotificationAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Inject Ready", result.Title);
        Assert.False(result.IsRead);
    }
}
