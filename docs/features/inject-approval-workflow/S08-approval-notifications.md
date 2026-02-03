# S08: Approval Notifications

**Feature:** [Inject Approval Workflow](FEATURE.md)  
**Priority:** P2  
**Points:** 5  
**Dependencies:** S04 (Approve/Reject), S05 (Batch Approval)

## User Story

**As a** Controller,  
**I want** to be notified when my injects are approved or rejected,  
**So that** I know when my work has been reviewed and can take action on feedback.

**As an** Exercise Director,  
**I want** to be notified when injects are submitted for my review,  
**So that** I can promptly approve content and keep the exercise on schedule.

## Context

The approval workflow creates a communication loop between Controllers (authors) and Directors (approvers). Notifications keep both parties informed of status changes. In-app notifications provide immediate visibility, with future email notifications for users not actively in the system.

Batch actions require consolidated notifications to avoid spamming users - if 10 injects are approved at once, the author should receive ONE notification listing all approved injects, not 10 separate notifications.

## Acceptance Criteria

### In-App Notification Bell
- [ ] **Given** I am logged in, **when** I view the app header, **then** I see a notification bell icon
- [ ] **Given** I have unread notifications, **when** I view the bell, **then** it shows a badge with unread count
- [ ] **Given** I click the bell, **when** dropdown opens, **then** I see my recent notifications
- [ ] **Given** I click a notification, **when** clicked, **then** I navigate to the relevant inject/exercise and notification is marked read

### Notification Types - For Authors (Controllers)
- [ ] **Given** my inject is approved, **when** approval is saved, **then** I receive notification: "Your inject [INJ-001] was approved by [Name]"
- [ ] **Given** my inject is rejected, **when** rejection is saved, **then** I receive notification: "Your inject [INJ-001] was rejected by [Name]" with rejection reason preview
- [ ] **Given** multiple of my injects are batch approved, **when** batch completes, **then** I receive ONE notification: "[Name] approved X of your injects" with list

### Notification Types - For Approvers (Directors/Admins)
- [ ] **Given** an inject is submitted for approval, **when** submission is saved, **then** all Directors/Admins on exercise receive notification: "[Author] submitted [INJ-001] for approval"
- [ ] **Given** all injects are now approved, **when** last approval completes, **then** Directors receive: "All injects approved - [Exercise Name] is ready to publish"

### Consolidated Batch Notifications
- [ ] **Given** a Director batch approves 5 injects from Author A and 3 from Author B, **when** batch completes, **then** Author A receives ONE notification listing their 5 injects
- [ ] **Given** same batch, **when** batch completes, **then** Author B receives ONE notification listing their 3 injects
- [ ] **Given** batch rejection with shared reason, **when** batch completes, **then** each author's notification includes the rejection reason

### Notification Management
- [ ] **Given** I view the notification dropdown, **when** I click "Mark all as read", **then** all notifications are marked read and badge clears
- [ ] **Given** I view a notification, **when** I click the X button, **then** that notification is dismissed
- [ ] **Given** dropdown is open, **when** I click "View all", **then** I navigate to full notifications page

### Notification Preferences (Future)
- [ ] **Given** notification preferences exist, **when** I view settings, **then** I can enable/disable notification types
- [ ] **Given** email notifications are enabled (future), **when** an event occurs and I'm offline, **then** I receive email after delay

### Real-Time Updates
- [ ] **Given** I have the app open, **when** a notification is created for me, **then** it appears in real-time via SignalR
- [ ] **Given** new notification arrives, **when** I don't have dropdown open, **then** badge count increments

## UI Design

### Notification Bell in Header

```
┌─────────────────────────────────────────────────────────────────┐
│  🎯 Cadence                              [🔔 3]  [👤 John Doe ▼] │
└─────────────────────────────────────────────────────────────────┘
```

### Notification Dropdown

```
┌─────────────────────────────────────────────────────────────────┐
│  Notifications                          [Mark all as read]      │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ● Jane Smith approved your inject                    2m  │   │
│  │   INJ-005: Media Inquiry                                 │   │
│  │   📝 "Good content, consider adding contact info"        │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ● Jane Smith rejected your inject                   15m  │   │
│  │   INJ-003: Shelter Capacity Report                       │   │
│  │   ❌ "Timing conflicts with evacuation phase"            │   │
│  └─────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ○ Mike Roberts submitted for approval                1h  │   │
│  │   INJ-012: Resource Staging Request                      │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  [View all notifications]                                       │
└─────────────────────────────────────────────────────────────────┘

● = unread, ○ = read
```

### Batch Approval Notification

```
┌─────────────────────────────────────────────────────────────────┐
│ ● Jane Smith approved 3 of your injects                     5m  │
│                                                                 │
│   • INJ-002: EOC Activation Notice                              │
│   • INJ-005: Media Inquiry                                      │
│   • INJ-009: Hospital Status Report                             │
│                                                                 │
│   📝 "All look good for exercise conduct"                       │
└─────────────────────────────────────────────────────────────────┘
```

### Full Notifications Page

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Notifications                                                              │
│                                                                             │
│  [All] [Approvals] [Submissions] [Exercise Updates]    [Mark all as read]   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Today                                                                      │
│  ─────────────────────────────────────────────────────────────────────────  │
│  │ ● Your inject INJ-005 was approved                              2:15 PM │
│  │   Jane Smith approved "Media Inquiry"                                   │
│  ─────────────────────────────────────────────────────────────────────────  │
│  │ ● Your inject INJ-003 was rejected                             11:30 AM │
│  │   Jane Smith: "Timing conflicts with evacuation phase"                  │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
│  Yesterday                                                                  │
│  ─────────────────────────────────────────────────────────────────────────  │
│  │ ○ INJ-012 submitted for approval                                4:45 PM │
│  │   Mike Roberts submitted "Resource Staging Request"                     │
│  ─────────────────────────────────────────────────────────────────────────  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Technical Implementation

### Backend: Notification Entity

```csharp
// File: src/Cadence.Core/Entities/ApprovalNotification.cs

/// <summary>
/// In-app notification for approval workflow events.
/// </summary>
public class ApprovalNotification : BaseEntity
{
    /// <summary>User who receives this notification.</summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    /// <summary>Exercise context for the notification.</summary>
    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    
    /// <summary>
    /// Single inject for individual notifications.
    /// Null for batch or exercise-level notifications.
    /// </summary>
    public Guid? InjectId { get; set; }
    public Inject? Inject { get; set; }
    
    /// <summary>Type of notification event.</summary>
    public NotificationType Type { get; set; }
    
    /// <summary>Pre-rendered notification title.</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Pre-rendered notification message/body.</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Additional data as JSON (e.g., list of inject IDs for batch).
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>User who triggered the notification (approver, author, etc.).</summary>
    public Guid? TriggeredById { get; set; }
    public User? TriggeredBy { get; set; }
    
    /// <summary>Whether user has seen this notification.</summary>
    public bool IsRead { get; set; } = false;
    
    /// <summary>When notification was read.</summary>
    public DateTime? ReadAt { get; set; }
    
    /// <summary>When notification was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum NotificationType
{
    /// <summary>To approvers: inject needs review.</summary>
    InjectSubmitted = 0,
    
    /// <summary>To author: your inject was approved.</summary>
    InjectApproved = 1,
    
    /// <summary>To author: your inject was rejected.</summary>
    InjectRejected = 2,
    
    /// <summary>To authors: multiple injects approved (batch).</summary>
    BatchApproved = 3,
    
    /// <summary>To authors: multiple injects rejected (batch).</summary>
    BatchRejected = 4,
    
    /// <summary>To approvers: reminder about pending items.</summary>
    ApprovalReminder = 5,
    
    /// <summary>To director: all injects approved, ready to publish.</summary>
    ExerciseReadyToPublish = 6,
    
    /// <summary>To author: approval was reverted.</summary>
    ApprovalReverted = 7
}
```

### Backend: Notification Service

```csharp
// File: src/Cadence.Core/Services/NotificationService.cs

public interface INotificationService
{
    Task NotifyInjectSubmittedAsync(Inject inject);
    Task NotifyInjectApprovedAsync(Inject inject);
    Task NotifyInjectRejectedAsync(Inject inject);
    Task NotifyBatchApprovedAsync(Guid authorId, List<Inject> injects, string? notes);
    Task NotifyBatchRejectedAsync(Guid authorId, List<Inject> injects, string reason);
    Task NotifyApprovalRevertedAsync(Inject inject);
    Task CheckAndNotifyReadyToPublishAsync(Guid exerciseId);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;
    
    /// <summary>
    /// Notifies all Directors/Admins that an inject was submitted.
    /// </summary>
    public async Task NotifyInjectSubmittedAsync(Inject inject)
    {
        // Get all Directors and Admins for this exercise
        var approvers = await GetExerciseApproversAsync(inject.Msel.ExerciseId);
        
        var author = await _context.Users.FindAsync(inject.SubmittedById);
        
        foreach (var approver in approvers)
        {
            if (approver.Id == inject.SubmittedById) continue; // Don't notify self
            
            var notification = new ApprovalNotification
            {
                Id = Guid.NewGuid(),
                UserId = approver.Id,
                ExerciseId = inject.Msel.ExerciseId,
                InjectId = inject.Id,
                Type = NotificationType.InjectSubmitted,
                Title = $"{author.DisplayName} submitted for approval",
                Message = $"{inject.InjectNumber}: {inject.Title}",
                TriggeredById = inject.SubmittedById,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.ApprovalNotifications.Add(notification);
            await SendRealTimeNotificationAsync(approver.Id, notification);
        }
        
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Notifies the author that their inject was approved.
    /// </summary>
    public async Task NotifyInjectApprovedAsync(Inject inject)
    {
        var authorId = inject.SubmittedById ?? inject.CreatedById;
        if (authorId == inject.ApprovedById) return; // Don't notify self
        
        var approver = await _context.Users.FindAsync(inject.ApprovedById);
        
        var notification = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = authorId,
            ExerciseId = inject.Msel.ExerciseId,
            InjectId = inject.Id,
            Type = NotificationType.InjectApproved,
            Title = $"{approver.DisplayName} approved your inject",
            Message = string.IsNullOrEmpty(inject.ApproverNotes)
                ? $"{inject.InjectNumber}: {inject.Title}"
                : $"{inject.InjectNumber}: {inject.Title}\n📝 \"{inject.ApproverNotes}\"",
            TriggeredById = inject.ApprovedById,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.ApprovalNotifications.Add(notification);
        await SendRealTimeNotificationAsync(authorId, notification);
        await _context.SaveChangesAsync();
        
        // Check if all injects are now approved
        await CheckAndNotifyReadyToPublishAsync(inject.Msel.ExerciseId);
    }
    
    /// <summary>
    /// Creates consolidated notification for batch approval.
    /// </summary>
    public async Task NotifyBatchApprovedAsync(
        Guid authorId, 
        List<Inject> injects, 
        string? notes)
    {
        if (!injects.Any()) return;
        
        var approver = await _context.Users.FindAsync(injects.First().ApprovedById);
        var exerciseId = injects.First().Msel.ExerciseId;
        
        var injectList = string.Join("\n", 
            injects.Select(i => $"• {i.InjectNumber}: {i.Title}"));
        
        var notification = new ApprovalNotification
        {
            Id = Guid.NewGuid(),
            UserId = authorId,
            ExerciseId = exerciseId,
            Type = NotificationType.BatchApproved,
            Title = $"{approver.DisplayName} approved {injects.Count} of your injects",
            Message = string.IsNullOrEmpty(notes)
                ? injectList
                : $"{injectList}\n📝 \"{notes}\"",
            Metadata = JsonSerializer.Serialize(injects.Select(i => i.Id)),
            TriggeredById = injects.First().ApprovedById,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.ApprovalNotifications.Add(notification);
        await SendRealTimeNotificationAsync(authorId, notification);
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Sends real-time notification via SignalR.
    /// </summary>
    private async Task SendRealTimeNotificationAsync(
        Guid userId, 
        ApprovalNotification notification)
    {
        await _hubContext.Clients
            .User(userId.ToString())
            .SendAsync("NotificationReceived", new
            {
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.CreatedAt,
                notification.ExerciseId,
                notification.InjectId
            });
    }
}
```

### Backend: API Endpoints

```csharp
// File: src/Cadence.Core/Controllers/NotificationsController.cs

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    /// <summary>
    /// Gets notifications for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int limit = 20)
    {
        var userId = GetUserId(User);
        var query = _context.ApprovalNotifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);
        
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }
        
        var notifications = await query
            .Take(limit)
            .ToListAsync();
        
        return Ok(_mapper.Map<List<NotificationDto>>(notifications));
    }
    
    /// <summary>
    /// Gets unread notification count.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var userId = GetUserId(User);
        var count = await _context.ApprovalNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
        
        return Ok(count);
    }
    
    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var notification = await _context.ApprovalNotifications.FindAsync(id);
        if (notification == null) return NotFound();
        if (notification.UserId != GetUserId(User)) return Forbid();
        
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
    
    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId(User);
        await _context.ApprovalNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow));
        
        return NoContent();
    }
}
```

### Frontend: Notification Bell Component

```tsx
// File: src/frontend/src/components/NotificationBell.tsx

export const NotificationBell: React.FC = () => {
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const { connection } = useSignalR();
  
  // Fetch initial count
  useEffect(() => {
    notificationApi.getUnreadCount().then(setUnreadCount);
  }, []);
  
  // Listen for real-time notifications
  useEffect(() => {
    if (!connection) return;
    
    connection.on('NotificationReceived', (notification: Notification) => {
      setUnreadCount(prev => prev + 1);
      setNotifications(prev => [notification, ...prev]);
    });
    
    return () => {
      connection.off('NotificationReceived');
    };
  }, [connection]);
  
  const handleOpen = async (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
    const recent = await notificationApi.getNotifications({ limit: 10 });
    setNotifications(recent);
  };
  
  const handleMarkAllRead = async () => {
    await notificationApi.markAllAsRead();
    setUnreadCount(0);
    setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
  };
  
  return (
    <>
      <IconButton onClick={handleOpen}>
        <Badge badgeContent={unreadCount} color="error">
          <NotificationsIcon />
        </Badge>
      </IconButton>
      
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={() => setAnchorEl(null)}
        PaperProps={{ sx: { width: 360, maxHeight: 480 } }}
      >
        <Box sx={{ p: 1, display: 'flex', justifyContent: 'space-between' }}>
          <Typography variant="subtitle1" fontWeight={600}>
            Notifications
          </Typography>
          {unreadCount > 0 && (
            <Button size="small" onClick={handleMarkAllRead}>
              Mark all as read
            </Button>
          )}
        </Box>
        
        <Divider />
        
        {notifications.length === 0 ? (
          <Box sx={{ p: 3, textAlign: 'center' }}>
            <Typography color="text.secondary">
              No notifications
            </Typography>
          </Box>
        ) : (
          notifications.map(notification => (
            <NotificationItem
              key={notification.id}
              notification={notification}
              onRead={() => handleNotificationRead(notification.id)}
            />
          ))
        )}
        
        <Divider />
        
        <MenuItem 
          component={Link} 
          to="/notifications"
          sx={{ justifyContent: 'center' }}
        >
          View all notifications
        </MenuItem>
      </Menu>
    </>
  );
};
```

### SignalR Hub

```csharp
// File: src/Cadence.Core/Hubs/NotificationHub.cs

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // User automatically joins their own group based on user ID
        var userId = Context.UserIdentifier;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }
}
```

## Test Cases

### Unit Tests

```csharp
[Fact]
public async Task NotifyInjectApproved_SendsToAuthor()
{
    // Arrange
    var author = await CreateUser("Controller");
    var approver = await CreateUser("Director");
    var inject = await CreateApprovedInject(author.Id, approver.Id);
    
    // Act
    await _service.NotifyInjectApprovedAsync(inject);
    
    // Assert
    var notification = await _context.ApprovalNotifications
        .FirstOrDefaultAsync(n => n.UserId == author.Id);
    
    Assert.NotNull(notification);
    Assert.Equal(NotificationType.InjectApproved, notification.Type);
    Assert.Contains(inject.InjectNumber, notification.Message);
}

[Fact]
public async Task NotifyBatchApproved_CreatesConsolidatedNotification()
{
    // Arrange
    var author = await CreateUser("Controller");
    var injects = await Create3InjectsByAuthor(author.Id);
    
    // Act
    await _service.NotifyBatchApprovedAsync(author.Id, injects, "All good!");
    
    // Assert
    var notifications = await _context.ApprovalNotifications
        .Where(n => n.UserId == author.Id)
        .ToListAsync();
    
    Assert.Single(notifications); // One consolidated, not three
    Assert.Contains("approved 3 of your injects", notifications[0].Title);
}

[Fact]
public async Task NotifyInjectSubmitted_NotifiesAllApprovers()
{
    // Arrange
    var exercise = await CreateExercise();
    var director1 = await CreateParticipant(exercise.Id, "Director");
    var director2 = await CreateParticipant(exercise.Id, "Director");
    var author = await CreateParticipant(exercise.Id, "Controller");
    var inject = await CreateSubmittedInject(author.Id);
    
    // Act
    await _service.NotifyInjectSubmittedAsync(inject);
    
    // Assert
    var notifications = await _context.ApprovalNotifications.ToListAsync();
    Assert.Equal(2, notifications.Count); // Both directors
    Assert.All(notifications, n => 
        Assert.Equal(NotificationType.InjectSubmitted, n.Type));
}
```

## Out of Scope

- Email notifications (future phase)
- Push notifications (mobile)
- Notification preferences/settings
- Daily digest emails

## Definition of Done

- [ ] ApprovalNotification entity created
- [ ] Notification service with all notification types
- [ ] Consolidated batch notifications working
- [ ] SignalR hub for real-time delivery
- [ ] API endpoints for notifications
- [ ] Notification bell component in header
- [ ] Badge shows unread count
- [ ] Dropdown shows recent notifications
- [ ] Mark as read functionality
- [ ] Mark all as read functionality
- [ ] Real-time updates via SignalR
- [ ] Navigation to inject on notification click
- [ ] Full notifications page
- [ ] Unit tests for notification service
- [ ] Frontend component tests
