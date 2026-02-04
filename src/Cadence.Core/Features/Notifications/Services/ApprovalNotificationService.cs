using System.Text.Json;
using Cadence.Core.Constants;
using Cadence.Core.Data;
using Cadence.Core.Features.Injects.Models.DTOs;
using Cadence.Core.Features.Notifications.Models.DTOs;
using Cadence.Core.Hubs;
using Cadence.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cadence.Core.Features.Notifications.Services;

/// <summary>
/// Service for managing approval notifications (S08: Approval Notifications).
/// Creates and manages notifications for inject approval workflow events.
/// </summary>
public class ApprovalNotificationService : IApprovalNotificationService
{
    private readonly AppDbContext _context;
    private readonly ICurrentOrganizationContext _orgContext;
    private readonly IExerciseHubContext _hubContext;
    private readonly ILogger<ApprovalNotificationService> _logger;

    public ApprovalNotificationService(
        AppDbContext context,
        ICurrentOrganizationContext orgContext,
        IExerciseHubContext hubContext,
        ILogger<ApprovalNotificationService> logger)
    {
        _context = context;
        _orgContext = orgContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyInjectSubmittedAsync(Inject inject, CancellationToken cancellationToken = default)
    {
        // Load inject with related data
        await _context.Entry(inject)
            .Reference(i => i.Msel)
            .Query()
            .Include(m => m.Exercise)
            .LoadAsync(cancellationToken);

        var exercise = inject.Msel.Exercise;

        // Find all Exercise Directors on this exercise
        var directors = await _context.ExerciseParticipants
            .Where(p => p.ExerciseId == exercise.Id && p.Role == ExerciseRole.ExerciseDirector)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        if (!directors.Any())
        {
            _logger.LogWarning(
                "No Exercise Directors found for exercise {ExerciseId} when notifying inject submission",
                exercise.Id);
            return;
        }

        // Create notification for each director
        foreach (var directorId in directors)
        {
            var notification = new ApprovalNotification
            {
                Id = Guid.NewGuid(),
                UserId = directorId,
                ExerciseId = exercise.Id,
                InjectId = inject.Id,
                Type = ApprovalNotificationType.InjectSubmitted,
                Title = $"Inject #{inject.InjectNumber} submitted for approval",
                Message = $"'{inject.Title}' has been submitted for approval in exercise '{exercise.Name}'.",
                OrganizationId = exercise.OrganizationId,
                TriggeredByUserId = inject.SubmittedByUserId,
                IsRead = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            };

            _context.ApprovalNotifications.Add(notification);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created {Count} approval notification(s) for inject {InjectId} submission",
            directors.Count, inject.Id);

        // Send real-time notification via SignalR
        var injectDto = inject.ToDto();
        await _hubContext.NotifyInjectSubmitted(exercise.Id, injectDto);
    }

    public async Task NotifyInjectApprovedAsync(Inject inject, CancellationToken cancellationToken = default)
    {
        // Don't notify if approver is the author
        if (inject.ApprovedByUserId == inject.SubmittedByUserId)
        {
            _logger.LogDebug(
                "Skipping approval notification for inject {InjectId} - approver is the author",
                inject.Id);
            return;
        }

        if (inject.SubmittedByUserId == null)
        {
            _logger.LogWarning(
                "Cannot notify inject approval - SubmittedByUserId is null for inject {InjectId}",
                inject.Id);
            return;
        }

        // Load inject with related data
        await _context.Entry(inject)
            .Reference(i => i.Msel)
            .Query()
            .Include(m => m.Exercise)
            .LoadAsync(cancellationToken);

        var exercise = inject.Msel.Exercise;

        var message = $"Your inject '{inject.Title}' has been approved for exercise '{exercise.Name}'.";
        if (!string.IsNullOrWhiteSpace(inject.ApproverNotes))
        {
            message += $" Notes: {inject.ApproverNotes}";
        }

        var notification = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = inject.SubmittedByUserId,
            ExerciseId = exercise.Id,
            InjectId = inject.Id,
            Type = ApprovalNotificationType.InjectApproved,
            Title = $"Inject #{inject.InjectNumber} approved",
            Message = message,
            OrganizationId = exercise.OrganizationId,
            TriggeredByUserId = inject.ApprovedByUserId,
            IsRead = false,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.ApprovalNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created approval notification for inject {InjectId} approved by {ApprovedBy}",
            inject.Id, inject.ApprovedByUserId);

        // Send real-time notification via SignalR
        var injectDto = inject.ToDto();
        await _hubContext.NotifyInjectApproved(exercise.Id, injectDto);
    }

    public async Task NotifyInjectRejectedAsync(Inject inject, CancellationToken cancellationToken = default)
    {
        if (inject.SubmittedByUserId == null)
        {
            _logger.LogWarning(
                "Cannot notify inject rejection - SubmittedByUserId is null for inject {InjectId}",
                inject.Id);
            return;
        }

        // Load inject with related data
        await _context.Entry(inject)
            .Reference(i => i.Msel)
            .Query()
            .Include(m => m.Exercise)
            .LoadAsync(cancellationToken);

        var exercise = inject.Msel.Exercise;

        var message = $"Your inject '{inject.Title}' was rejected in exercise '{exercise.Name}'.";
        if (!string.IsNullOrWhiteSpace(inject.RejectionReason))
        {
            message += $" Reason: {inject.RejectionReason}";
        }

        var notification = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = inject.SubmittedByUserId,
            ExerciseId = exercise.Id,
            InjectId = inject.Id,
            Type = ApprovalNotificationType.InjectRejected,
            Title = $"Inject #{inject.InjectNumber} rejected",
            Message = message,
            OrganizationId = exercise.OrganizationId,
            TriggeredByUserId = inject.RejectedByUserId,
            IsRead = false,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.ApprovalNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created rejection notification for inject {InjectId} rejected by {RejectedBy}",
            inject.Id, inject.RejectedByUserId);

        // Send real-time notification via SignalR
        var injectDto = inject.ToDto();
        await _hubContext.NotifyInjectRejected(exercise.Id, injectDto);
    }

    public async Task NotifyInjectRevertedAsync(Inject inject, CancellationToken cancellationToken = default)
    {
        if (inject.SubmittedByUserId == null)
        {
            _logger.LogWarning(
                "Cannot notify inject revert - SubmittedByUserId is null for inject {InjectId}",
                inject.Id);
            return;
        }

        // Load inject with related data
        await _context.Entry(inject)
            .Reference(i => i.Msel)
            .Query()
            .Include(m => m.Exercise)
            .LoadAsync(cancellationToken);

        var exercise = inject.Msel.Exercise;

        var message = $"Your approved inject '{inject.Title}' was reverted back to submitted status in exercise '{exercise.Name}'.";
        if (!string.IsNullOrWhiteSpace(inject.RevertReason))
        {
            message += $" Reason: {inject.RevertReason}";
        }

        var notification = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = inject.SubmittedByUserId,
            ExerciseId = exercise.Id,
            InjectId = inject.Id,
            Type = ApprovalNotificationType.InjectReverted,
            Title = $"Inject #{inject.InjectNumber} approval reverted",
            Message = message,
            OrganizationId = exercise.OrganizationId,
            TriggeredByUserId = inject.RevertedByUserId,
            IsRead = false,
            CreatedBy = SystemConstants.SystemUserIdString,
            ModifiedBy = SystemConstants.SystemUserIdString
        };

        _context.ApprovalNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created revert notification for inject {InjectId} reverted by {RevertedBy}",
            inject.Id, inject.RevertedByUserId);

        // Send real-time notification via SignalR
        var injectDto = inject.ToDto();
        await _hubContext.NotifyInjectReverted(exercise.Id, injectDto);
    }

    public async Task NotifyBatchApprovedAsync(
        string approverUserId,
        List<Inject> injects,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (!injects.Any())
        {
            return;
        }

        // Load exercise data for the first inject (all injects should be in same exercise)
        var firstInject = injects.First();
        await _context.Entry(firstInject)
            .Reference(i => i.Msel)
            .Query()
            .Include(m => m.Exercise)
            .LoadAsync(cancellationToken);

        var exercise = firstInject.Msel.Exercise;

        // Group injects by author
        var injectsByAuthor = injects
            .Where(i => i.SubmittedByUserId != null && i.SubmittedByUserId != approverUserId)
            .GroupBy(i => i.SubmittedByUserId)
            .ToList();

        foreach (var authorGroup in injectsByAuthor)
        {
            var authorId = authorGroup.Key!;
            var authorInjects = authorGroup.ToList();
            var count = authorInjects.Count;

            var injectNumbers = string.Join(", ", authorInjects.Select(i => $"#{i.InjectNumber}"));
            var message = $"{count} inject{(count > 1 ? "s" : "")} ({injectNumbers}) approved in exercise '{exercise.Name}'.";
            if (!string.IsNullOrWhiteSpace(notes))
            {
                message += $" Notes: {notes}";
            }

            // Store inject IDs in metadata for batch notifications
            var metadata = JsonSerializer.Serialize(new
            {
                InjectIds = authorInjects.Select(i => i.Id).ToList(),
                InjectNumbers = authorInjects.Select(i => i.InjectNumber).ToList()
            });

            var notification = new ApprovalNotification
            {
                Id = Guid.NewGuid(),
                UserId = authorId,
                ExerciseId = exercise.Id,
                InjectId = null, // Batch notification
                Type = ApprovalNotificationType.InjectApproved,
                Title = $"{count} inject{(count > 1 ? "s" : "")} approved",
                Message = message,
                Metadata = metadata,
                OrganizationId = exercise.OrganizationId,
                TriggeredByUserId = approverUserId,
                IsRead = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            };

            _context.ApprovalNotifications.Add(notification);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created {Count} batch approval notification(s) for {InjectCount} injects",
            injectsByAuthor.Count, injects.Count);

        // Send real-time notifications via SignalR for each inject
        foreach (var inject in injects)
        {
            var injectDto = inject.ToDto();
            await _hubContext.NotifyInjectApproved(exercise.Id, injectDto);
        }
    }

    public async Task NotifyBatchRejectedAsync(
        string approverUserId,
        List<Inject> injects,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!injects.Any())
        {
            return;
        }

        // Load exercise data for the first inject (all injects should be in same exercise)
        var firstInject = injects.First();
        await _context.Entry(firstInject)
            .Reference(i => i.Msel)
            .Query()
            .Include(m => m.Exercise)
            .LoadAsync(cancellationToken);

        var exercise = firstInject.Msel.Exercise;

        // Group injects by author
        var injectsByAuthor = injects
            .Where(i => i.SubmittedByUserId != null && i.SubmittedByUserId != approverUserId)
            .GroupBy(i => i.SubmittedByUserId)
            .ToList();

        foreach (var authorGroup in injectsByAuthor)
        {
            var authorId = authorGroup.Key!;
            var authorInjects = authorGroup.ToList();
            var count = authorInjects.Count;

            var injectNumbers = string.Join(", ", authorInjects.Select(i => $"#{i.InjectNumber}"));
            var message = $"{count} inject{(count > 1 ? "s" : "")} ({injectNumbers}) rejected in exercise '{exercise.Name}'.";
            if (!string.IsNullOrWhiteSpace(reason))
            {
                message += $" Reason: {reason}";
            }

            // Store inject IDs in metadata for batch notifications
            var metadata = JsonSerializer.Serialize(new
            {
                InjectIds = authorInjects.Select(i => i.Id).ToList(),
                InjectNumbers = authorInjects.Select(i => i.InjectNumber).ToList()
            });

            var notification = new ApprovalNotification
            {
                Id = Guid.NewGuid(),
                UserId = authorId,
                ExerciseId = exercise.Id,
                InjectId = null, // Batch notification
                Type = ApprovalNotificationType.InjectRejected,
                Title = $"{count} inject{(count > 1 ? "s" : "")} rejected",
                Message = message,
                Metadata = metadata,
                OrganizationId = exercise.OrganizationId,
                TriggeredByUserId = approverUserId,
                IsRead = false,
                CreatedBy = SystemConstants.SystemUserIdString,
                ModifiedBy = SystemConstants.SystemUserIdString
            };

            _context.ApprovalNotifications.Add(notification);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created {Count} batch rejection notification(s) for {InjectCount} injects",
            injectsByAuthor.Count, injects.Count);

        // Send real-time notifications via SignalR for each inject
        foreach (var inject in injects)
        {
            var injectDto = inject.ToDto();
            await _hubContext.NotifyInjectRejected(exercise.Id, injectDto);
        }
    }

    public async Task<List<ApprovalNotificationDto>> GetNotificationsAsync(
        string userId,
        int limit = 20,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (!_orgContext.HasContext)
        {
            return new List<ApprovalNotificationDto>();
        }

        var query = _context.ApprovalNotifications
            .IgnoreQueryFilters() // Bypass DbContext filters, we do our own filtering below
            .Where(n => !n.IsDeleted) // Explicit soft delete filter
            .Include(n => n.Exercise)
            .Include(n => n.Inject)
            .Include(n => n.TriggeredByUser)
            .Where(n => n.UserId == userId)
            .AsQueryable();

        // Filter by organization
        if (_orgContext.CurrentOrganizationId.HasValue && !_orgContext.IsSysAdmin)
        {
            query = query.Where(n => n.OrganizationId == _orgContext.CurrentOrganizationId.Value);
        }

        // Filter by read status if requested
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new ApprovalNotificationDto(
                n.Id,
                n.UserId,
                n.ExerciseId,
                n.Exercise != null ? n.Exercise.Name : "",
                n.InjectId,
                n.Inject != null ? $"#{n.Inject.InjectNumber}" : null,
                n.Type.ToString(),
                n.Title,
                n.Message,
                n.Metadata,
                n.TriggeredByUserId,
                n.TriggeredByUser != null ? n.TriggeredByUser.DisplayName : null,
                n.IsRead,
                n.ReadAt,
                n.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return notifications;
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!_orgContext.HasContext)
        {
            return 0;
        }

        var query = _context.ApprovalNotifications
            .IgnoreQueryFilters() // Bypass DbContext filters, we do our own filtering below
            .Where(n => !n.IsDeleted) // Explicit soft delete filter
            .Where(n => n.UserId == userId && !n.IsRead)
            .AsQueryable();

        // Filter by organization
        if (_orgContext.CurrentOrganizationId.HasValue && !_orgContext.IsSysAdmin)
        {
            query = query.Where(n => n.OrganizationId == _orgContext.CurrentOrganizationId.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(string userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.ApprovalNotifications
            .FirstOrDefaultAsync(
                n => n.Id == notificationId && n.UserId == userId,
                cancellationToken);

        if (notification == null)
        {
            _logger.LogWarning(
                "Notification {NotificationId} not found or not owned by user {UserId}",
                notificationId, userId);
            return;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.ModifiedBy = userId;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Marked notification {NotificationId} as read for user {UserId}",
                notificationId, userId);
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!_orgContext.HasContext)
        {
            return;
        }

        var query = _context.ApprovalNotifications
            .IgnoreQueryFilters() // Bypass DbContext filters, we do our own filtering below
            .Where(n => !n.IsDeleted) // Explicit soft delete filter
            .Where(n => n.UserId == userId && !n.IsRead)
            .AsQueryable();

        // Filter by organization
        if (_orgContext.CurrentOrganizationId.HasValue && !_orgContext.IsSysAdmin)
        {
            query = query.Where(n => n.OrganizationId == _orgContext.CurrentOrganizationId.Value);
        }

        var notifications = await query.ToListAsync(cancellationToken);

        if (!notifications.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            notification.ModifiedBy = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Marked {Count} notifications as read for user {UserId}",
            notifications.Count, userId);
    }
}
